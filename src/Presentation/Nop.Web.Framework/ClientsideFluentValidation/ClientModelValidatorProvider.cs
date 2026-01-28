using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Nop.Web.Framework.ClientsideFluentValidation.Validators;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Framework.ClientsideFluentValidation;

/// <summary>
/// Used to generate clientside metadata from FluentValidation's rules.
/// </summary>
public class ClientModelValidatorProvider : IClientModelValidatorProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    #region Ctor

    public ClientModelValidatorProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates set of <see cref="T:Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidator" />s by updating
    /// <see cref="P:Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientValidatorItem.Validator" /> in <see cref="P:Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientValidatorProviderContext.Results" />.
    /// </summary>
    /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext" /> associated with this call.</param>
    public void CreateValidators(ClientValidatorProviderContext context)
    {
        var descriptor = getCachedDescriptor();

        if (descriptor == null)
            return;

        var propertyName = context.ModelMetadata.PropertyName;

        var validatorsWithRules = descriptor.GetRulesForMember(propertyName)
            .Where(rule => !rule.HasCondition && !rule.HasAsyncCondition)
            .Select(rule => new { rule, components = rule.Components })
            .Where(t => t.components.Any())
            .SelectMany(t => t.components, (t, component) => new { t, component })
            .Where(t => !t.component.HasCondition && !t.component.HasAsyncCondition)
            .Select(t => new { t, modelValidatorForProperty = getModelValidator(t.t.rule, t.component) })
            .Where(t => t.modelValidatorForProperty != null)
            .Select(t => t.modelValidatorForProperty)
            .ToList();

        if (validatorsWithRules.Any())
        {
            foreach (var propVal in validatorsWithRules)
                context.Results.Add(new ClientValidatorItem { Validator = propVal, IsReusable = false });
        }
        else
        {
            //add one ClientValidatorItem with IsReusable = false to prevent MVC cache the list of validators
            context.Results.Add(new ClientValidatorItem { IsReusable = false });
        }

        handleNonNullableValueTypeRequiredRule();

        return;

        static IClientModelValidator getModelValidator(IValidationRule validationRule, IRuleComponent ruleComponent)
        {
            if (validationRule?.RuleSets?.Any(rs => rs.Equals(NopValidationDefaults.ValidationRuleSet)) ?? false)
                return null;

            var type = ruleComponent.Validator.GetType();

            Dictionary<Type, IClientModelValidator> clientValidatorFactories = new() {
                { typeof(INotNullValidator), new RequiredClientValidator(validationRule, ruleComponent) },
                { typeof(INotEmptyValidator), new RequiredClientValidator(validationRule, ruleComponent) },
                { typeof(IEmailValidator), new EmailClientValidator(validationRule, ruleComponent) },
                { typeof(IRegularExpressionValidator), new RegexClientValidator(validationRule, ruleComponent) },
                { typeof(IMaximumLengthValidator), new MaxLengthClientValidator(validationRule, ruleComponent) },
                { typeof(IMinimumLengthValidator), new MinLengthClientValidator(validationRule, ruleComponent) },
                { typeof(IExactLengthValidator), new LengthClientValidator(validationRule, ruleComponent)},
                { typeof(ILengthValidator), new LengthClientValidator(validationRule, ruleComponent)},
                { typeof(IInclusiveBetweenValidator), new RangeClientValidator(validationRule, ruleComponent) },
                { typeof(IGreaterThanOrEqualValidator), new RangeMinClientValidator(validationRule, ruleComponent) },
                { typeof(ILessThanOrEqualValidator), new RangeMaxClientValidator(validationRule, ruleComponent) },
                { typeof(IEqualValidator), new EqualToClientValidator(validationRule, ruleComponent) },
                { typeof(ICreditCardValidator), new CreditCardClientValidator(validationRule, ruleComponent) },
            };

            var factory = clientValidatorFactories
                .FirstOrDefault(x => x.Key.IsAssignableFrom(type))
                .Value;

            return factory;
        }

        void handleNonNullableValueTypeRequiredRule()
        {
            if (!context.ModelMetadata.ModelType.IsValueType || Nullable.GetUnderlyingType(context.ModelMetadata.ModelType) != null)
                return;

            var fvHasRequiredRule = context.Results.Any(x => x.Validator is RequiredClientValidator);

            if (!fvHasRequiredRule)
                return;

            var dataAnnotationsRequiredRule = context.Results.FirstOrDefault(x => x.Validator is RequiredAttributeAdapter);
            context.Results.Remove(dataAnnotationsRequiredRule);
        }

        IValidatorDescriptor getCachedDescriptor()
        {
            ArgumentNullException.ThrowIfNull(_httpContextAccessor?.HttpContext);

            var modelType = context.ModelMetadata.ContainerType;

            if (modelType == null)
                return null;

            var cache = getCache();

            if (cache.TryGetValue(modelType, out var cachedDescriptor))
                return cachedDescriptor;

            var validator = _httpContextAccessor.HttpContext.RequestServices.GetService(typeof(IValidator<>).MakeGenericType(modelType)) as IValidator;

            cachedDescriptor = validator?.CreateDescriptor();
            cache[modelType] = cachedDescriptor;

            return cachedDescriptor;

            Dictionary<Type, IValidatorDescriptor> getCache()
            {
                const string cacheKey = "_Nop_Client_Validation_Cache_";

                Dictionary<Type, IValidatorDescriptor> validatorDescriptors = null;

                if (_httpContextAccessor.HttpContext.Items.TryGetValue(cacheKey, out var item))
                    validatorDescriptors = item as Dictionary<Type, IValidatorDescriptor>;

                if (validatorDescriptors != null)
                    return validatorDescriptors;

                validatorDescriptors = new Dictionary<Type, IValidatorDescriptor>();
                _httpContextAccessor.HttpContext.Items[cacheKey] = validatorDescriptors;

                return validatorDescriptors;
            }
        }
    }

    #endregion
}
