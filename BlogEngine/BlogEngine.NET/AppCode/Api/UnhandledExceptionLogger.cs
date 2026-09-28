using System;
using Microsoft.AspNetCore.Mvc.Filters;
using BlogEngine.Core;

namespace BlogEngine.NET.AppCode.Api
{
    /// <summary>
    /// Global exception filter that logs unhandled exceptions.
    /// Replaces the Web API 2 ExceptionLogger (IExceptionLogger from System.Web.Http.ExceptionHandling).
    /// </summary>
    public class UnhandledExceptionLogger : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is UnauthorizedAccessException) { return; }

            var request = context.HttpContext.Request;
            Utils.Log("{0} {1}: {2}", request.Method, request.Path + request.QueryString, context.Exception.Message);
        }
    }
}
