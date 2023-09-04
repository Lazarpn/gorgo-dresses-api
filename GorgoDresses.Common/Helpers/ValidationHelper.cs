using GorgoDresses.Common.Enums;
using GorgoDresses.Common.Exceptions;
using GorgoDresses.Common.Extensions;
using GorgoDresses.Common.Validation;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GorgoDresses.Common.Helpers;

public static class ValidationHelper
{
    private static readonly string[] AllowedColors = { "#f8c9a6", "#f5e27c", "#d4f8a6", "#aff4df", "#bdc7fc", "#deb9fc", "#f3b8cd" };

    public static void MustExist<T>(T entity, string customEntityName = null) where T : class
    {
        string entityName = typeof(T).Name.ToSentenceCase();
        if (entity == null)
        {
            throw new NotFoundException(ErrorCode.EntityDoesNotExist, new { entityName = customEntityName ?? entityName });
        }
    }

    public static void MustExist<T>(bool entityExists, string customEntityName = null)
    {
        string entityName = typeof(T).Name.ToSentenceCase();

        if (!entityExists)
        {
            throw new NotFoundException(ErrorCode.EntityDoesNotExist, new { entityName = customEntityName ?? entityName });
        }
    }

    public static void MustNotExist<T>(T entity, string customEntityName = null) where T : class
    {
        string entityName = typeof(T).Name.ToSentenceCase();
        if (entity != null)
        {
            throw new ValidationException(ErrorCode.EntityAlreadyExists, new { entityName = customEntityName ?? entityName });
        }
    }

    public static void MustNotExist<T>(bool entityExists, string customEntityName = null)
    {
        string entityName = typeof(T).Name.ToSentenceCase();

        if (entityExists)
        {
            throw new NotFoundException(ErrorCode.EntityAlreadyExists, new { entityName = customEntityName ?? entityName });
        }
    }

    public static void ThrowModelValidationException(string property, string errorMessage)
    {
        throw new ValidationException(new List<ExceptionDetail>
        {
            new()
            {
                ErrorCode = ErrorCode.RequestInvalid,
                Params = new ValidationResult { Property = property.ToCamelCase(), Errors = new List<string> { errorMessage } }
            }
        });
    }

    public static void CheckIdentityResult(IdentityResult identityResult, ErrorOverrideModel errorOverride = null)
    {
        if (identityResult.Succeeded)
        {
            return;
        }

        var errorDetails = GetIdentityErrors(identityResult);

        if (errorOverride != null && errorDetails == errorOverride.TextToOverride)
        {
            errorDetails = errorOverride.OverrideWith;
        }

        throw new ValidationException(ErrorCode.IdentityError, errorDetails);
    }

    public static bool IsEmailValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Normalize the domain
            email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));

            // Examines the domain part of the email and normalizes it.
            static string DomainMapper(Match match)
            {
                // Use IdnMapping class to convert Unicode domain names.
                var idn = new IdnMapping();

                // Pull out and process domain name (throws ArgumentException on invalid)
                var domainName = idn.GetAscii(match.Groups[2].Value);

                return match.Groups[1].Value + domainName;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(email,
                @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static bool IsValidColor(string color)
    {
        return !string.IsNullOrWhiteSpace(color) && AllowedColors.Any(c => c == color.ToLowerInvariant());
    }

    private static string GetIdentityErrors(IdentityResult identityResult)
    {
        //return string.Join(", ", identityResult.Errors.Select(it => it.Description));
        //TODO: pogledati ovo
        return string.Join(", ", identityResult.Errors);
    }
}

public class ErrorOverrideModel
{
    public string TextToOverride { get; set; }
    public string OverrideWith { get; set; }
}
