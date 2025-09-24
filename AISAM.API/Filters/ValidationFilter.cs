using System.Net;
using AISAM.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AISAM.API.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Check model validation after model binding
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var response = GenericResponse<object>.CreateError(
                    "Validation failed", 
                    HttpStatusCode.BadRequest, 
                    "VALIDATION_ERROR");
                
                response.Error.ValidationErrors = new Dictionary<string, List<string>>
                {
                    { "ValidationErrors", errors }
                };

                context.Result = new BadRequestObjectResult(response);
                return;
            }

            await next();
        }
    }
}
