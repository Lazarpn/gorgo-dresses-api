using GorgoDresses.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GorgoDresses.Common.Exceptions;

public class AuthenticationException : SystemException
{
    public AuthenticationException(ErrorCode errorCode, object responseParams = null) : base(errorCode, responseParams) { }
    public AuthenticationException(List<ExceptionDetail> details) : base(details) { }
}
