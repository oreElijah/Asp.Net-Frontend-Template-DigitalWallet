using DigitalWalletCore.Entities;
using DigitalWalletInfrastructure.Seeds;
using DigitalWalletInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Extensions
{
    public static class DatabaseSeederExtensions
    {
        public static async Task SeedDatabaseAsync(
        this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();

            // Apply all pending migrations
            await context.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            var configuration = services.GetRequiredService<IConfiguration>();

            await RoleSeeder.SeedAsync(roleManager);

            await AdminSeeder.SeedAsync(
                userManager,
                configuration);
        }
    }
}
