using DigitalWalletCore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Entity;

namespace DigitalWalletCore
{
    public static class EntryPoint
    {
        public static IServiceCollection AddCoreDI(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ConnectionStringOptions>(configuration.GetSection(ConnectionStringOptions.SectionName));
            services.Configure<PaystackOptions>(configuration.GetSection(PaystackOptions.SectionName));

            return services;
        }
    }
}
