using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Services
{
    public class SchoolService : ISchoolService
    {
        private readonly ISchoolRepository _schoolRepository;
        private readonly ILogger<SchoolService> _logger;

        public SchoolService(ISchoolRepository schoolRepository, ILogger<SchoolService> logger)
        {
            _schoolRepository = schoolRepository;
            _logger = logger;
        }

        public async Task<AppResponse<SchoolResponseDto>> AddSchoolToPlatform(SchoolRequestDto schoolRequestDto)
        {
            var response = await _schoolRepository.AddSchoolToPlatform(schoolRequestDto);

            if (response == null)
            {
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to add school to platform.",
                    Errors = new List<string> { "An error occurred while adding the school." }
                };
            }

            return response;
        }

        public async Task<AppResponse<bool>> DeleteSchoolAsync(Guid schoolId)
        {
            var response = await _schoolRepository.DeleteSchoolAsync(schoolId);
            return response;
        }

        public async Task<AppResponse<List<SchoolResponseDto>>> GetAllSchoolsAsync()
        {
            var response = await _schoolRepository.GetAllSchoolsAsync();

            if(response == null)
            {
                return new AppResponse<List<SchoolResponseDto>>
                {
                    Succeeded = false,
                    Message = "Failed to retrieve schools.",
                    Errors = new List<string> { "An error occurred while retrieving the schools." }
                };
            }

            return response;
        }

        public async Task<AppResponse<SchoolResponseDto>> GetSchoolByCodeAsync(string schoolCode)
        {
            var response = await _schoolRepository.GetSchoolByCodeAsync(schoolCode);

            if (response == null)
            {
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to retrieve school by code.",
                    Errors = new List<string> { "An error occurred while retrieving the school." }
                };
            }
            return response;
        }

        public async Task<AppResponse<SchoolResponseDto>> GetSchoolByIdAsync(Guid schoolId)
        {
            var response = await _schoolRepository.GetSchoolByIdAsync(schoolId);

            if (response == null)
            {
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to retrieve school by ID.",
                    Errors = new List<string> { "An error occurred while retrieving the school." }
                };
            }
            return response;
        }

        public async Task<AppResponse<SchoolUserResponseDto>> GetSchoolUsersAsync(string schoolCode)
        {
            var response = await _schoolRepository.GetSchoolUsersAsync(schoolCode);

            if (response == null)
            {
                return new AppResponse<SchoolUserResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to retrieve school users.",
                    Errors = new List<string> { "An error occurred while retrieving the school users." }
                };
            }

            return response;
        }

        public async Task<AppResponse<SchoolResponseDto>> UpdateSchoolAsync(Guid schoolId, SchoolUpdateDto schoolUpdateDto)
        {
            var response = await _schoolRepository.UpdateSchoolAsync(schoolId, schoolUpdateDto);

            if (response == null)
            {
                return new AppResponse<SchoolResponseDto>
                {
                    Succeeded = false,
                    Message = "Failed to update school.",
                    Errors = new List<string> { "An error occurred while updating the school." }
                };
            }

            return response;
        }
    }
}
