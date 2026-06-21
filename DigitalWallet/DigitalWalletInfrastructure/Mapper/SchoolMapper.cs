using System;
using System.Collections.Generic;
using System.Text;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Entities;

namespace DigitalWalletInfrastructure.Mapper
{
    public static class SchoolMapper
    {
        public static SchoolResponseDto ToSchoolResponseDto(this School school)
        {
            return new SchoolResponseDto
            {
                Id = school.Id,
                Name = school.Name,
                Code = school.Code,
                IsActive = school.IsActive,
                Users = school.Users.Where(user => user.SchoolCode == school.Code).Select(user => user.ToAppUserDto()).ToList(),
                CreatedAt = school.CreatedAt
            };
        }

        public static School ToSchool(this SchoolRequestDto schoolRequestDto)
        {
            return new School
            {
                Name = schoolRequestDto.Name,
                Code = schoolRequestDto.Code,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static SchoolUserResponseDto ToSchoolUserResponseDto(this School school)
        {
            return new SchoolUserResponseDto
            {
                SchoolCode = school.Code,
                SchoolName = school.Name,
                Users = school.Users.Where(user => user.SchoolCode == school.Code).Select(user => user.ToAppUserDto()).ToList()
            };
        }
    }
}
