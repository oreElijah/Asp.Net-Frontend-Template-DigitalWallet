using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Interfaces
{
    public interface ISchoolRepository
    {
        public Task<AppResponse<SchoolResponseDto>> GetSchoolByCodeAsync(string schoolCode);

        public Task<AppResponse<List<SchoolResponseDto>>> GetAllSchoolsAsync();

        public Task<AppResponse<SchoolResponseDto>> GetSchoolByIdAsync(Guid schoolId);

        public Task<AppResponse<SchoolResponseDto>> AddSchoolToPlatform(SchoolRequestDto schoolRequestDto);

        public Task<AppResponse<SchoolResponseDto>> UpdateSchoolAsync(Guid schoolId, SchoolUpdateDto schoolUpdateDto);

        public Task<AppResponse<SchoolUserResponseDto>> GetSchoolUsersAsync(string schoolCode);

        public Task<AppResponse<bool>> DeleteSchoolAsync(Guid schoolId);

        public Task<School> SchoolExists(Guid schoolId);
    }
}
