using Amazon.Runtime.Internal.Util;
using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Exceptions;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using DigitalWalletInfrastructure.Mapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Repositories
{
    public class SchoolRepository : ISchoolRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SchoolRepository> _logger;
        public SchoolRepository(ApplicationDbContext context, ILogger<SchoolRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AppResponse<SchoolResponseDto>> AddSchoolToPlatform(SchoolRequestDto schoolRequestDto)
        {
            var school = schoolRequestDto.ToSchool();

            if(school == null)
            {
                _logger.LogError("Failed to map SchoolRequestDto to School entity.");
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to add school."
                };
            }

            var schoolExists = await _context.School.AnyAsync(s => s.Code == schoolRequestDto.Code || s.Name == schoolRequestDto.Name);
            if (schoolExists)
            {
                _logger.LogError("Couldn't add School because a School with the name or code already exists.");
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to add school."
                };
            }

            await _context.School.AddAsync(school);
            await _context.SaveChangesAsync();

            _logger.LogInformation("School {schoolName} added successfully with Code: {schoolCode}", school.Name, school.Code);
            return new AppResponse<SchoolResponseDto>
            {
                Data = school.ToSchoolResponseDto(),
                Succeeded = true,
                Message = "School added successfully"
            };
        }

        public async Task<AppResponse<bool>> DeleteSchoolAsync(Guid schoolId)
        {
            var school = await SchoolExists(schoolId);

            if(school == null)
            {
                _logger.LogError("School with {schoolId} does not exist", schoolId);
                return new AppResponse<bool>
                {
                    Succeeded = false,
                    Message = "School could not be deleted"
                };
            }

            _context.Remove(school);
            await _context.SaveChangesAsync();

            _logger.LogInformation("School with {schoolId} has been deleted", schoolId);
            return new AppResponse<bool>
            {
                Data = true,
                Succeeded = true,
                Message = "School has been deleted"
            };
        }

        public async Task<AppResponse<List<SchoolResponseDto>>> GetAllSchoolsAsync()
        {
            var schools = await _context.School
                .Include(s => s.Users)
                .ToListAsync();

            if(schools == null)
            {
                _logger.LogWarning("Schools returns null");
                return new AppResponse<List<SchoolResponseDto>>
                {
                    Message = "No School was found",
                    Succeeded = false
                };
            }

            _logger.LogInformation("All registered schools have been returned");
            return new AppResponse<List<SchoolResponseDto>>
            {
                Data = schools.Select(s => s.ToSchoolResponseDto()).ToList(),
                Succeeded = true,
                Message = "Schools retrieved successfully"
            }; 
        }

        public async Task<AppResponse<SchoolResponseDto>> GetSchoolByCodeAsync(string schoolCode)
        {
            var school = await _context.School
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Code == schoolCode);
            if (school == null)
            {
                _logger.LogWarning("No school found with code {SchoolCode}", schoolCode);
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to find school."
                };
            }
            else
            {
                _logger.LogInformation("School found with code {SchoolCode}: {Name}", schoolCode, school.Name);
                return new AppResponse<SchoolResponseDto>
                {
                    Data = school.ToSchoolResponseDto(),
                    Succeeded = true,
                    Message = "School retrieved successfully"
                };
            }
        }

        public async Task<AppResponse<SchoolResponseDto>> GetSchoolByIdAsync(Guid schoolId)
        {
            var school = await _context.School
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Id == schoolId);
            if (school == null)
            {
                _logger.LogWarning("No school found with Id {SchoolId}", schoolId);
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to find school."
                };
            }
            else
            {
                _logger.LogInformation("School found with code {SchoolCode}: {Name}", schoolId, school.Name);
                return new AppResponse<SchoolResponseDto>
                {
                    Data = school.ToSchoolResponseDto(),
                    Succeeded = true,
                    Message = "School retrieved successfully"
                };
            }
        }

        public async Task<AppResponse<SchoolUserResponseDto>> GetSchoolUsersAsync(string schoolCode)
        {
            var school = await _context.School
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Code == schoolCode);

            if (school == null)
            {
                _logger.LogWarning("No school found with code {SchoolCode}", schoolCode);
                return new AppResponse<SchoolUserResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to find school."
                };
            }

            var data = school.ToSchoolUserResponseDto();

            return new AppResponse<SchoolUserResponseDto>
            {
                Succeeded = true,
                Data = data,
                Message = "School users returned succesfully"
            };
        }

        public async Task<School> SchoolExists(Guid schoolId)
        {
            var school = await _context.School
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Id == schoolId);
            if (school == null)
            {
                _logger.LogWarning($"School with ID {schoolId} does not exist.");
                throw new NotFoundException("School with ID {schoolId} not found.");
            }

            _logger.LogInformation($"School with ID {schoolId} exists.");
            return school;
        }

        public async Task<AppResponse<SchoolResponseDto>> UpdateSchoolAsync(Guid schoolId, SchoolUpdateDto schoolUpdateDto)
        {
            var school = await SchoolExists(schoolId);
            if (school == null)
            {
                _logger.LogWarning($"School with ID {schoolId} does not exist.");
                throw new NotFoundException("School with ID {schoolId} not found.");
            }

            var schoolExists = await _context.School.AnyAsync(s => s.Name == schoolUpdateDto.Name);
            if (schoolExists)
            {
                _logger.LogError("Couldn't Update School because a School with the name already exists.");
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to update school."
                };
            }

            school.Name = schoolUpdateDto.Name;
            await _context.SaveChangesAsync();

            _logger.LogInformation("School with Id {SchoolId} has been updated successfully", schoolId);
            return new AppResponse<SchoolResponseDto>
            {
                Data = school.ToSchoolResponseDto(),
                Succeeded = true,
                Message = "School Updated Successfully"
            };
        }
    }
}
