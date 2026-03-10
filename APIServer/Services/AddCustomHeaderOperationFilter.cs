using APIServer.AuthAttributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace APIServer.Services
{
    public class AddCustomHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            // Always add X-Auth-Security header to all endpoints
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Auth-Security",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });

            // Check if the endpoint requires company context
            var requiresCompanyContext = CheckIfRequiresCompanyContext(context);

            // Add X-Company-Id header only if company context is required
            if (requiresCompanyContext)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-Company-Id",
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "uuid"
                    },
                    Description = "The company ID (GUID) for company-scoped operations"
                });
            }
        }

        private bool CheckIfRequiresCompanyContext(OperationFilterContext context)
        {
            // Check method-level attributes first
            var methodAttributes = context.MethodInfo.GetCustomAttributes(true);
            foreach (var attr in methodAttributes)
            {
                if (attr is TypeFilterAttribute typeFilter && 
                    typeFilter.ImplementationType == typeof(ServiceProviderFilterFactory))
                {
                    return GetRequireCompanyContextFromArgs(typeFilter.Arguments);
                }
            }

            // Check controller-level attributes if not found on method
            var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true);
            if (controllerAttributes != null)
            {
                foreach (var attr in controllerAttributes)
                {
                    if (attr is TypeFilterAttribute typeFilter && 
                        typeFilter.ImplementationType == typeof(ServiceProviderFilterFactory))
                    {
                        return GetRequireCompanyContextFromArgs(typeFilter.Arguments);
                    }
                }
            }

            return false;
        }

        private bool GetRequireCompanyContextFromArgs(object[]? arguments)
        {
            if (arguments == null || arguments.Length < 3)
            {
                // Default value when not specified is true
                return true;
            }

            // Third argument (index 2) is requireCompanyContext
            if (arguments[2] is bool requireCompanyContext)
            {
                return requireCompanyContext;
            }

            return true;
        }
    }
}
