using DigitalWalletCore.Entities;
using DigitalWalletCore.Interfaces;
using DigitalWalletCore.Options;
using DigitalWalletInfrastructure.Authentication;
using DigitalWalletInfrastructure.Data;
using DigitalWalletInfrastructure.Repositories;
using DigitalWalletInfrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
//using Microsoft.AspNetCore.Authentication.G
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

namespace DigitalWalletInfrastructure
{
    public static class EntryPoint
    {
        public static IServiceCollection AddInfrastructureDI(this IServiceCollection services, IConfiguration configuration)
        {
            //services.Add          
            services.AddDbContext<ApplicationDbContext>((provider, options) =>
            {
                var connectionString = provider
                    .GetRequiredService<IOptionsSnapshot<ConnectionStringOptions>>()
                    .Value
                    .DefaultConnection;

                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            });

            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
            services.AddHttpClient();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                options.DefaultChallengeScheme =
                options.DefaultForbidScheme =
                options.DefaultSignInScheme =
                options.DefaultScheme =
                options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(options =>
            {
                var clientId = configuration["Authentication:Google:ClientId"] ?? "MissingClientId"; ;

                if (clientId == null)
                {
                    throw new ArgumentNullException("Google ClientId is not configured.");
                }

                var clientSecret = configuration["Authentication:Google:ClientSecret"];

                if (clientSecret == null)
                {
                    throw new ArgumentNullException("Google ClientSecret is not configured.");
                }

                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(configuration["JWT:SigningKey"] ?? throw new InvalidOperationException("JWT:SigningKey is not configured."))
                    )
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Request.Method == "OPTIONS")
                        {
                            context.NoResult();
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (string.IsNullOrWhiteSpace(jti))
                        {
                            context.Fail("Token is missing jti.");
                            return;
                        }

                        // var cache = context.HttpContext.RequestServices.GetService<IDistributedCache>();

                        // if (cache is null)
                        // {
                        //     // Redis isn't configured. Skip token blacklist validation.
                        //     return;
                        // }

                        // var revoked = await cache.GetStringAsync(jti);

                        // if (!string.IsNullOrWhiteSpace(revoked))
                        // {
                        //     context.Fail("Token has been revoked.");
                        // }
                        
                        // if (!string.IsNullOrWhiteSpace(revoked))
                        // {
                        //     context.Fail("Token has been revoked.");
                        // }
                    }
                };
            });

            // var redisConnection = configuration["Redis:Configuration"];

            // if (!string.IsNullOrWhiteSpace(redisConnection))
            // {
            //     services.AddStackExchangeRedisCache(options =>
            //     {
            //         options.Configuration = redisConnection;
            //         options.InstanceName = configuration["Redis:InstanceName"];
            //     });
            // }

            services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(
                    configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddHangfireServer();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddHttpClient<IPaymentService, PaymentService>();
            services.AddScoped<ISchoolService, SchoolService>();
            services.AddScoped<ISchoolRepository, SchoolRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<IFileStorageService, BackBlazeStorageService>();
            services.AddScoped<IQRCodeService, QRCodeService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IAuditService, AuditService>();
            return services;
        }
    }
}
