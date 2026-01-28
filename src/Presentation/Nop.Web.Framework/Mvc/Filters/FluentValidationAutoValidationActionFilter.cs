using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Framework.Mvc.Filters;

/// <summary>
/// Represents a filter that validates models using FluentValidation before executing an action
/// </summary>
public class AutoValidationActionFilter : IAsyncActionFilter
{
    /// <summary>
    /// Called asynchronously before the action, after model binding is complete.
    /// </summary>
    /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext" />.</param>
    /// <param name="next">
    /// The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate" />. Invoked to execute the next action filter or the action itself.
    /// </param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that on completion indicates the filter has executed.</returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is ControllerBase)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            var serviceProvider = context.HttpContext.RequestServices;
            
            foreach (var parameter in controllerActionDescriptor.Parameters)
            {
                if (!context.ActionArguments.TryGetValue(parameter.Name, out var subject))
                    continue;

                var parameterType = subject?.GetType();

                if (subject == null || parameterType is not { IsClass: true, IsEnum: false, IsValueType: false, IsPrimitive: false } ||
                    serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(parameterType)) is not IValidator validator)
                {
                    continue;
                }

                IValidationContext validationContext;

                if (parameter is IParameterInfoParameterDescriptor infoParameterDescriptor &&
                    infoParameterDescriptor.ParameterInfo.CustomAttributes.Any(ca =>
                        ca.AttributeType == typeof(ValidateAttribute)))
                {
                    validationContext = ValidationContext<object>.CreateWithOptions(subject, options => options.IncludeRuleSets(NopValidationDefaults.ValidationRuleSet));
                }
                else
                {
                    validationContext = new ValidationContext<object>(subject);
                }

                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                
                if (validationResult.IsValid)
                    continue;

                foreach (var error in validationResult.Errors)
                    context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        await next();
    }
}