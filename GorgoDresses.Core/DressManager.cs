using AutoMapper;
using GorgoDresses.Common.Helpers;
using GorgoDresses.Common.Models.Dress;
using GorgoDresses.Data;
using GorgoDresses.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Core;
public class DressManager
{
    private readonly GorgoDressesDbContext db;
    private readonly IMapper mapper;
    private readonly FileManager fileManager;

    public DressManager(GorgoDressesDbContext db, IMapper mapper, FileManager fileManager)
    {
        this.db = db;
        this.mapper = mapper;
        this.fileManager = fileManager;
    }

    public async Task<List<DressBasicInfoModel>> GetDressesBasicInfo()
    {
        var dresses = await db.Dresses.ToListAsync();
        foreach (var dress in dresses)
        {
            dress.FileUrl = await fileManager.GetFileStorageUrl<Dress>(dress.FileName);
        }
        var dressList =  mapper.Map<List<DressBasicInfoModel>>(dresses);
        var sortedDresses = dressList.OrderByDescending(dress => dress.Date).ToList();
        return sortedDresses;
    }

    public async Task<DressAdminBasicInfoModel> CreateDress(Guid userId, DressCreateModel model)
    {
        var dress = new Dress
        {
            Name = model.Name,
            Date = model.Date,
            Type = model.Type,
            RentingPrice = model.RentingPrice,
            SellingPrice = model.SellingPrice,
        };

        await db.AddAsync(dress);

        if (model.File == null)
        {
            return null;
        };

        var user = await db.Users.FindAsync(userId);
        ValidationHelper.MustExist(user);

        dress.FileName = await fileManager.ProcessFileStorageUpload<Dress>(model.File, dress.FileName);
        dress.FileOriginalName = model.File.FileName;
        dress.FileUrl = await fileManager.GetFileStorageUrl<Dress>(dress.FileName);
        dress.ThumbUrl = await fileManager.GetFileStorageUrl<Dress>(dress.FileName, isThumb: true);
        await db.SaveChangesAsync();

        var dressModel = new DressAdminBasicInfoModel
        {
            Id = dress.Id,
            Name = model.Name,
            Date = model.Date,
            Type = model.Type,
            ThumbUrl = dress.ThumbUrl
        };

        return dressModel;
    }

    public async Task<List<DressAdminBasicInfoModel>> GetAdminDressesBasicInfo()
    {
        var dresses = await db.Dresses.ToListAsync();
        foreach (var dress in dresses)
        {
            dress.ThumbUrl = await fileManager.GetFileStorageUrl<Dress>(dress.FileName, isThumb: true);
        }
        var dressList = mapper.Map<List<DressAdminBasicInfoModel>>(dresses);

        return dressList;
    }

    public async Task<DressAdminModel> GetDressAdmin(Guid id)
    {
        var dress = await db.Dresses.FirstOrDefaultAsync(d => d.Id == id);
        dress.FileUrl = await fileManager.GetFileStorageUrl<Dress>(dress.FileName);
        var dressModel = mapper.Map<DressAdminModel>(dress);
        return dressModel;
    }

    public async Task DeleteDress(Guid id)
    {
        var dress = await db.Dresses.FirstOrDefaultAsync(d => d.Id == id);
        ValidationHelper.MustExist(dress);
        db.Dresses.Remove(dress);
        await db.SaveChangesAsync();
    }

    public async Task<List<DressTypeModel>> GetDressTypes()
    {
        var dresses = await db.Dresses.ToListAsync();
        var types = new List<string>();
        dresses.ForEach(dress =>
        {
            types.Add(dress.Type);
        });
        var uniqueTypes = types.Distinct().ToList();
        var uniqueDressTypes = new List<DressTypeModel>();
        uniqueTypes.ForEach(type =>
        {
            uniqueDressTypes.Add(new DressTypeModel { Type = type });
        });

        return uniqueDressTypes;
    }
}
