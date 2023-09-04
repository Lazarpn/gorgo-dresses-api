using GorgoDresses.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GorgoDresses.Common.Exceptions;

public class ValidationException : SystemException
{
    public ValidationException(ErrorCode errorCode, object responseParams = null) : base(errorCode, responseParams) { }
    public ValidationException(List<ExceptionDetail> details) : base(details) { }
}

