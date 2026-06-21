using DigitalWalletApi.Extensions;
using DigitalWalletApplication;
using DigitalWalletCore;
using DigitalWalletInfrastructure;
using DigitalWalletApi.Filter;
using Microsoft.AspNetCore.Mvc;
using Amazon.S3;

namespace DigitalWalletApi
{
    public static class EntryPoint
    {
        public static IServiceCollection AddAppDI(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationDI(configuration)
                .AddInfrastructureDI(configuration)
                .AddCoreDI(configuration);

            services.AddControllers();

            services.AddEndpointsApiExplorer();

            services.AddSwaggerDocumentation();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5173")
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });
            services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            //services.AddOpenApi();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            services.AddSingleton<IAmazonS3>(sp =>
            {
                return new AmazonS3Client(
                configuration["Backblaze:KeyId"],
                configuration["Backblaze:ApplicationKey"],
                new AmazonS3Config
                {
                    ServiceURL = configuration["Backblaze:Endpoint"],
                    ForcePathStyle = true
                });
            });

            services.AddScoped<LogActionFilter>();

            return services;
        }
    }
}
