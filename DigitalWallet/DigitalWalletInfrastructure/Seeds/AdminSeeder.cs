using DigitalWalletCore.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Seeds
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(
            UserManager<AppUser> userManager,
            IConfiguration configuration)
        {
            var adminEmail = configuration["Admin:Email"];

            var adminPassword = configuration["Admin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail)
                || string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var existingAdmin =
                await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin != null)
            {
                return;
            }

            var admin = new AppUser
            {
                FirstName = "System",
                LastName = "Admin",
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                SchoolCode = "ADMIN",
                Wallet = new Wallet
                {
                    WalletNumber = "ADMIN001",
                    Balance = 0.00m
                }
            };

            var result = await userManager.CreateAsync(
                admin,
                adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
