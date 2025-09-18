using System.Linq;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AISAM.API.Swagger
{
    public class ODataQueryOptionsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasEnableQuery = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<EnableQueryAttribute>()
                .Any()
                || context.MethodInfo.DeclaringType != null && context.MethodInfo.DeclaringType
                    .GetCustomAttributes(true)
                    .OfType<EnableQueryAttribute>()
                    .Any();

            if (!hasEnableQuery)
            {
                return;
            }

            operation.Parameters ??= new System.Collections.Generic.List<OpenApiParameter>();

            void AddQueryParam(string name, string description)
            {
                if (operation.Parameters.Any(p => p.Name == name && p.In == ParameterLocation.Query))
                {
                    return;
                }

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = name,
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = description,
                    Schema = new OpenApiSchema { Type = "string" }
                });
            }

            AddQueryParam("$filter", "Filter the results using OData filter syntax");
            AddQueryParam("$select", "Select specific fields to return");
            AddQueryParam("$orderby", "Order the results, e.g. field asc, field2 desc");
            AddQueryParam("$expand", "Expand related entities");
            AddQueryParam("$top", "Limit the number of results returned");
            AddQueryParam("$skip", "Skip the first N results");
            AddQueryParam("$count", "Include count of items. true or false");
            AddQueryParam("$search", "Full-text search if supported");
        }
    }
}


