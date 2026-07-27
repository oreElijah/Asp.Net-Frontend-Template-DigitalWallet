using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Analytics;

namespace DigitalWalletCore.Interfaces;

public interface IAnalyticsService
{
    Task<AppResponse<StudentDashboardDto>> GetStudentDashboardAsync(string userId, CancellationToken cancellationToken = default);
    Task<AppResponse<MerchantDashboardDto>> GetMerchantDashboardAsync(string userId, CancellationToken cancellationToken = default);
    Task<AppResponse<SchoolDashboardDto>> GetSchoolDashboardAsync(string userId, CancellationToken cancellationToken = default);
    Task<AppResponse<SystemDashboardDto>> GetSystemDashboardAsync(CancellationToken cancellationToken = default);
}
