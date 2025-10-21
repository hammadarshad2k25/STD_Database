using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace STD_Database.Helper
{
    public class ConfigurationSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _Provider;
        public ConfigurationSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _Provider = provider;
        }
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _Provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Version = description.ApiVersion.ToString(),
                    Title = $"STD_Database {description.ApiVersion}",
                    Description = $"API Version {description.ApiVersion} - STD_Database with JWT + Redis"
                });
            }
        }
    }
}
