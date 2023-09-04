using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Common.Enums;
public enum ErrorCode
{
    RequestInvalid,
    Unauthorized,
    RequirementsNotMet,
    EntityDoesNotExist,
    EntityAlreadyExists,
    IdentityError,
    InternalServerError,
    InvalidCredentials,
    EmptyFile,
    MaxFileSizeReached,
    CanOnlyUploadPhotos,
    WrongAspectRatio,
    MissingAspectRatio,
    EmailNotSent,
    InvalidGoogleAccount,
    AlreadyConfirmedEmail,
    InvalidConfirmationCode,
    ConfirmationCodeExpired
}

