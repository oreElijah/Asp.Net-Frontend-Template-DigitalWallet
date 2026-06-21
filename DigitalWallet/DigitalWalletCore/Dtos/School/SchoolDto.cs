using DigitalWalletCore.Dtos.User;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Dtos.School
{
    public class SchoolDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public List<AppUserDto> Users { get; set; } = new List<AppUserDto>();
    }
}
