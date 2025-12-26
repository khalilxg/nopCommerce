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
        var routeData = httpContext.GetRouteData() ?? new RouteData();
        var urlHelperFactory = EngineContext.Current.Resolve<IUrlHelperFactory>();

        var endpoint = httpContext.GetEndpoint();
        var actionDescriptor = endpoint?.Metadata.GetMetadata<ActionDescriptor>();

        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);
        if (actionContext == null)
            return null;

        return urlHelperFactory.GetUrlHelper(actionContext);
    }
}