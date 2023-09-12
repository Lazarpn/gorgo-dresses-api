using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using GorgoDresses.Common.Enums;
using GorgoDresses.Common.Exceptions;
using GorgoDresses.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Core;

public class FileManager
{
    private readonly BlobServiceClient blobServiceClient;

    private readonly Dictionary<string, SKEncodedImageFormat> skiaImageExtensions = new()
    {
        [".jpg"] = SKEncodedImageFormat.Jpeg,
        [".jpeg"] = SKEncodedImageFormat.Jpeg,
        [".png"] = SKEncodedImageFormat.Png,
        [".gif"] = SKEncodedImageFormat.Gif,
        [".bmp"] = SKEncodedImageFormat.Bmp
    };

    private readonly Type[] publicContainerTypes = { typeof(User) };

    private readonly int THUMBNAIL_DIMENSION;
    private readonly int MAX_FILE_SIZE_IN_MB;
    private readonly int MINUTES_BLOB_STORAGE_URL_EXPIRES_IN;

    public FileManager(IConfiguration configuration)
    {
        THUMBNAIL_DIMENSION = int.Parse(configuration["thumbnailDimension"]);
        MAX_FILE_SIZE_IN_MB = int.Parse(configuration["maxFileSizeInMb"]);
        MINUTES_BLOB_STORAGE_URL_EXPIRES_IN = Convert.ToInt32(configuration["minutesBlobStorageUrlExpiresIn"]);

        var storageConnectionString = configuration.GetConnectionString("AzureWebJobsStorage");
        blobServiceClient = new BlobServiceClient(storageConnectionString);
    }

    public async Task<string> GetFileStorageUrl<T>(string fileName, bool isThumb = false, bool isDownload = false, string originalFileName = null)
    {
        if (isThumb)
        {
            if (!IsImage(fileName))
            {
                return null;
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb{extension}";
        }

        string containerName = typeof(T).Name.ToLower();

        BlobContainerClient containerClient = await GetBlobContainerClient<T>();
        var blobClient = containerClient.GetBlobClient(fileName);

        Uri fileUri = null;

        if (publicContainerTypes.Any(t => t == typeof(T)))
        {
            if (isDownload)
            {
                var blobSasBuilder = new BlobSasBuilder
                {
                    StartsOn = DateTime.UtcNow.AddMinutes(-1),
                    ExpiresOn = DateTime.UtcNow.AddDays(7),
                    BlobContainerName = containerName,
                    BlobName = fileName
                };

                blobSasBuilder.ContentDisposition = new ContentDisposition
                {
                    FileName = originalFileName,
                    DispositionType = "attachment"
                }.ToString();

                blobSasBuilder.SetPermissions(BlobSasPermissions.Read);

                fileUri = blobClient.GenerateSasUri(blobSasBuilder);
            }
            else
            {
                fileUri = blobClient.Uri;
            }
        }
        else
        {
            var blobSasBuilder = new BlobSasBuilder
            {
                StartsOn = DateTime.UtcNow.AddMinutes(-1),
                ExpiresOn = DateTime.UtcNow.AddMinutes(MINUTES_BLOB_STORAGE_URL_EXPIRES_IN),
                BlobContainerName = containerName,
                BlobName = fileName
            };

            if (isDownload)
            {
                blobSasBuilder.ContentDisposition = new ContentDisposition
                {
                    FileName = originalFileName,
                    DispositionType = "attachment"
                }.ToString();
            }
            else
            {
                new FileExtensionContentTypeProvider().TryGetContentType(fileName, out
                string contentType);
                blobSasBuilder.ContentType = contentType;
            }

            blobSasBuilder.SetPermissions(BlobSasPermissions.Read);

            fileUri = blobClient.GenerateSasUri(blobSasBuilder);
        }

        return fileUri.AbsoluteUri;
    }

    public async Task<string> ProcessFileStorageUpload<T>
        (IFormFile file,
        string previousFileName = null,
        int? maxFileSizeOverride = null,
        double? forceAspectRatio = null,
        int? thumbnailDimension = null)
    {
        //TODO: sto ovo nije u validateuploadedfile i kako formatirati vise parametara
        if (file.Length == 0)
        {
            throw new ValidationException(ErrorCode.EmptyFile);
        }

        var maxFileSizeInMb = maxFileSizeOverride ?? MAX_FILE_SIZE_IN_MB;
        ValidateUploadedFile(file, maxFileSizeInMb, forceAspectRatio.HasValue, forceAspectRatio);

        BlobContainerClient containerClient = await GetBlobContainerClient<T>();

        new FileExtensionContentTypeProvider().TryGetContentType(file.FileName, out string contentType);

        var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var blobClient = containerClient.GetBlobClient(newFileName);
        var uploadHeaders = new BlobHttpHeaders() { ContentType = contentType };

        using var fileStream = file.OpenReadStream();
        await blobClient.UploadAsync(fileStream, uploadHeaders);

        if (IsImage(file.FileName))
        {
            fileStream.Position = 0;
            await GenerateThumb(
                newFileName,
                fileStream,
                containerClient,
                uploadHeaders,
                forceAspectRatio ?? 1,
                thumbnailDimension ?? THUMBNAIL_DIMENSION);
        }

        if (previousFileName != null)
        {
            await DeleteFileIfExists<T>(previousFileName, containerClient);
        }

        return newFileName;
    }

    public async Task DeleteFileIfExists<T>(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        BlobContainerClient containerClient = await GetBlobContainerClient<T>();

        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync();

        if (IsImage(fileName) && !fileName.Contains("_thumb"))
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var thumbFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb{extension}";
            await DeleteFileIfExists<T>(thumbFileName, containerClient);
        }
    }

    public bool IsImage(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return skiaImageExtensions.Any(e => e.Key == extension);
    }

    private async Task<BlobContainerClient> GetBlobContainerClient<T>()
    {
        string containerName = typeof(T).Name.ToLower();
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var currentAccessPolicy = await containerClient.GetAccessPolicyAsync();

        if (publicContainerTypes.Any(t => t == typeof(T)) && currentAccessPolicy.Value.BlobPublicAccess != PublicAccessType.Blob)
        {
            await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);
        }

        return containerClient;
    }

    private async Task DeleteFileIfExists<T>(string fileName, BlobContainerClient containerClient)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync();

        if (IsImage(fileName) && !fileName.Contains("_thumb"))
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var thumbFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb{extension}";
            await DeleteFileIfExists<T>(thumbFileName, containerClient);
        }
    }

    private async Task GenerateThumb
        (string fileName,
        Stream fileStream,
        BlobContainerClient containerClient,
        BlobHttpHeaders uploadHeaders,
        double aspectRatio,
        int thumbnailDimension)
    {
        var extension = skiaImageExtensions.First(e => e.Key == Path.GetExtension(fileName).ToLowerInvariant());
        var thumbFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb{extension.Key}";
        var blobClient = containerClient.GetBlobClient(thumbFileName);

        using SKBitmap sourceBitmap = SKBitmap.Decode(fileStream);
        using SKBitmap croppedAndResizedBitmap = CropAndResize(sourceBitmap, thumbnailDimension, aspectRatio);

        var imageFormat = skiaImageExtensions.First(e => e.Key == Path.GetExtension(fileName).ToLowerInvariant()).Value;
        using Stream thumbStream = croppedAndResizedBitmap.Encode(imageFormat, 80).AsStream();

        await blobClient.UploadAsync(thumbStream, uploadHeaders);
    }

    private void ValidateUploadedFile(IFormFile file, int maxSizeInMb, bool validateAspectRatio, double? forceAspectRatio)
    {
        var invalidFileSize = (double)file.Length / (1024 * 1024) > maxSizeInMb;

        if (invalidFileSize)
        {
            throw new ValidationException(ErrorCode.MaxFileSizeReached, new { maxSizeInMb });
        }

        if (!validateAspectRatio)
        {
            return;
        }

        if (!IsImage(file.FileName))
        {
            throw new ValidationException(ErrorCode.CanOnlyUploadPhotos);
        }

        using SKBitmap sourceBitmap = SKBitmap.Decode(file.OpenReadStream());

        if (Math.Abs(sourceBitmap.Width - sourceBitmap.Height *
        forceAspectRatio.Value) > forceAspectRatio.Value)
        {
            throw new ValidationException(ErrorCode.WrongAspectRatio, new
            {
                correctRatio = GetAspectRatioString(forceAspectRatio.Value)
            });
        }
    }

    private SKBitmap CropAndResize(SKBitmap bitmap, int dimension, double aspectRatioToCutTo)
    {
        double currentAspectRatio = (double)bitmap.Width / bitmap.Height;

        if (aspectRatioToCutTo != currentAspectRatio)
        {
            ImageEdge edgeToCut = aspectRatioToCutTo > currentAspectRatio ?
            ImageEdge.Y : ImageEdge.X;

            int cropWidth = Convert.ToInt32(edgeToCut == ImageEdge.X ?
            bitmap.Height * aspectRatioToCutTo : bitmap.Width);
            int cropHeight = Convert.ToInt32(edgeToCut == ImageEdge.Y ?
            bitmap.Width / aspectRatioToCutTo : bitmap.Height);

            var delta = edgeToCut == ImageEdge.X ? (bitmap.Width - cropWidth) / 2
            : (bitmap.Height - cropHeight) / 2;
            var deltaX = edgeToCut == ImageEdge.X ? delta : 0;
            var deltaY = edgeToCut == ImageEdge.Y ? delta : 0;

            bitmap = CropImage(bitmap, SKRect.Create(deltaX, deltaY, cropWidth,
            cropHeight));
        }

        var thumbWidth = aspectRatioToCutTo > 1 ? dimension : dimension / aspectRatioToCutTo;
        var thumbHeight = aspectRatioToCutTo > 1 ? dimension / aspectRatioToCutTo : dimension;

        var resizedImage = ResizeImage(bitmap, Convert.ToInt32(thumbWidth),
        Convert.ToInt32(thumbHeight));
        return resizedImage;
    }

    private SKBitmap CropImage(SKBitmap bitmap, SKRect cropArea)
    {
        var croppedBitmap = new SKBitmap(Convert.ToInt32(cropArea.Width),
        Convert.ToInt32(cropArea.Height));
        var destination = new SKRect(0, 0, cropArea.Width, cropArea.Height);

        using var canvas = new SKCanvas(croppedBitmap);
        canvas.DrawBitmap(bitmap, cropArea, destination);

        return croppedBitmap;
    }

    private SKBitmap ResizeImage(SKBitmap sourceBitmap, int width, int height)
    {
        var imageInfo = new SKImageInfo(width, height);
        return sourceBitmap.Resize(imageInfo, SKFilterQuality.High);
    }

    private string GetAspectRatioString(double aspectRatio)
    {
        return aspectRatio switch
        {
            1 => "1:1",
            (double)15 / 4 => "15:4",
            (double)16 / 9 => "16:9",
            _ => throw new ValidationException(ErrorCode.MissingAspectRatio, new
            {
                aspectRatio
            }),
        };
    }
}

public enum ImageEdge
{
    X = 1, Y
}
