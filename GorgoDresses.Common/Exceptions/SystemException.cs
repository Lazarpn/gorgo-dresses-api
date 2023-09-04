using GorgoDresses.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GorgoDresses.Common.Exceptions;

public abstract class SystemException : Exception
{
    public List<ExceptionDetail> Details { get; set; } = new List<ExceptionDetail>();

    public SystemException(ErrorCode errorCode, object responseParams = null) : base(string.Empty)
    {
        var exceptionDetail = new ExceptionDetail
        {
            ErrorCode = errorCode
        };

        if (responseParams != null)
        {
            if (responseParams is string)
            {
                exceptionDetail.Params = new { details = responseParams };
            }
            else
            {
                exceptionDetail.Params = responseParams;
            }
        }
        Details.Add(exceptionDetail);
    }

    public SystemException(List<ExceptionDetail> details) : base(string.Empty)
    {
        foreach (var detail in details)
        {
            if (detail.Params != null && detail.Params is string)
            {
                detail.Params = new { details = detail.Params };
            }
        }
        Details = details;
    }
}

public class ExceptionDetail
{
    public ErrorCode ErrorCode { get; set; }
    public object Params { get; set; }
}
