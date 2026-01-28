using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Nop.Web.Framework.ClientsideFluentValidation;

/// <summary>
/// Represents view options configuration to use clientside fluent validation
/// </summary>
public partial class ClientsideFluentValidationViewOptionsSetup : IConfigureOptions<MvcViewOptions>
{
    private readonly Action<ClientModelValidatorProvider> _action;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientsideFluentValidationViewOptionsSetup(Action<ClientModelValidatorProvider> action, IHttpContextAccessor httpContextAccessor)
    {
        _action = action;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Invoked to configure an option instance
    /// </summary>
    /// <param name="options">The options instance to configure</param>
    public void Configure(MvcViewOptions options)
    {
        var provider = new ClientModelValidatorProvider(_httpContextAccessor);
        _action?.Invoke(provider);
        options.ClientModelValidatorProviders.Add(provider);
    }
}