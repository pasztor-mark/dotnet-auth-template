
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;

namespace auth_template.Utilities;

public class ResultFactory : IFluentValidationAutoValidationResultFactory
{
    public Task<IActionResult?> CreateActionResult(ActionExecutingContext context, ValidationProblemDetails validationProblemDetails,
        IDictionary<IValidationContext, ValidationResult> validationResults)
    {
        var errors = validationProblemDetails?.Errors ?? new Dictionary<string, string[]>();

        var response = new
        {
            data = (object)null,
            message = "Please check your inputted information",
            statusCode = 400,
            errors = errors
        };

        return Task.FromResult<IActionResult?>(new BadRequestObjectResult(response));
    }

  
}