using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Mvc.Routing;

/// <summary>
/// Represents url helper extension methods
/// </summary>
public static class UrlHelperExtensions
{
    public static IUrlHelper GetUrlHelper()
    {
        var httpContext = EngineContext.Current.Resolve<IHttpContextAccessor>().HttpContext;

        if (httpContext == null)
            return null;

        var routeData = httpContext.GetRouteData();
        var endpoint = httpContext.GetEndpoint();
        var actionDescriptor = endpoint?.Metadata.GetMetadata<ActionDescriptor>();

        if (actionDescriptor == null)
            return null;

        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);
        var urlHelperFactory = EngineContext.Current.Resolve<IUrlHelperFactory>();

        return urlHelperFactory.GetUrlHelper(actionContext);
    }
}