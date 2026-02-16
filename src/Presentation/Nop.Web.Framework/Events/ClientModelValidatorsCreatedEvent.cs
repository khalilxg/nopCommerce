using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Nop.Web.Framework.Events;

/// <summary>
/// Represents an event that occurs after dictionary of client side model validators is created
/// </summary>
public partial class ClientModelValidatorsCreatedEvent
{
    #region Fields

    protected readonly Dictionary<Type, IClientModelValidator> _clientValidatorFactories;
    
    #endregion

    #region Ctor

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="clientValidatorFactories">Dictionary of client side model validators</param>
    public ClientModelValidatorsCreatedEvent(Dictionary<Type, IClientModelValidator> clientValidatorFactories)
    {
        _clientValidatorFactories = clientValidatorFactories;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Adds a client model validator for the specified type if one does not already exist
    /// </summary>
    /// <remarks>
    /// This method enables the registration of custom client-side validation logic for model
    /// binding. If a validator for the specified type is already registered, the method overwrite it
    /// </remarks>
    /// <param name="type">The type for which the client model validator is to be registered</param>
    /// <param name="clientModelValidator">The client model validator instance to associate with the specified type.</param>
    /// <returns>True if the client model validator was successfully added; otherwise, false.</returns>
    public virtual bool AddClientModelValidator(Type type, IClientModelValidator clientModelValidator)
    {
        if (!_clientValidatorFactories.ContainsKey(type))
            return _clientValidatorFactories.TryAdd(type, clientModelValidator);

        _clientValidatorFactories[type] = clientModelValidator;

        return true;
    }

    #endregion
}