using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BlogEngine.NET.AppCode.Api
{
    public class UnauthorizedAccessExceptionFilterAttribute : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is UnauthorizedAccessException)
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.Unauthorized);
                context.ExceptionHandled = true;
            }
        }
    }
}
