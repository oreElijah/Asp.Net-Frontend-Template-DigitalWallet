using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Analytics;
using DigitalWalletCore.Enums;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigitalWalletInfrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AppResponse<StudentDashboardDto>> GetStudentDashboardAsync(string userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.Wallet.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => new { w.Id, w.Balance })
            .SingleOrDefaultAsync(cancellationToken);
        if (wallet is null) return new AppResponse<StudentDashboardDto>("Wallet not found.");

        var now = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var weekStart = today.AddDays(-((7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7));
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var successful = _context.Transaction.AsNoTracking().Where(t => t.Status == TransactionStatus.Successful);
        var sent = successful.Where(t => t.SenderWalletId == wallet.Id);
        var received = successful.Where(t => t.ReceiverWalletId == wallet.Id);
        var chartStart = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var dailyChartStart = today.AddDays(-29);

        var dto = new StudentDashboardDto
        {
            CurrentBalance = wallet.Balance,
            TotalCredits = await received.SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            TotalDebits = await sent.SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            TodaySpending = await sent.Where(t => t.CreatedAt >= today).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            WeeklySpending = await sent.Where(t => t.CreatedAt >= weekStart).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            MonthlySpending = await sent.Where(t => t.CreatedAt >= monthStart).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            TransactionCount = await successful.Where(t => t.SenderWalletId == wallet.Id || t.ReceiverWalletId == wallet.Id).CountAsync(cancellationToken),
            AverageTransactionValue = await sent.AverageAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            MonthlySpendingChart = await sent.Where(t => t.CreatedAt >= chartStart)
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new TimeSeriesPointDto { Period = new DateTime(g.Key.Year, g.Key.Month, 1), Amount = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderBy(x => x.Period).ToListAsync(cancellationToken),
            DailySpending = await sent.Where(t => t.CreatedAt >= dailyChartStart).GroupBy(t => t.CreatedAt.Date)
                .Select(g => new TimeSeriesPointDto { Period = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderBy(x => x.Period).ToListAsync(cancellationToken),
            TransactionTypes = await successful.Where(t => t.SenderWalletId == wallet.Id || t.ReceiverWalletId == wallet.Id)
                .GroupBy(t => t.Type)
                .Select(g => new TransactionTypeBreakdownDto { Type = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() })
                .ToListAsync(cancellationToken),
            TopMerchants = await sent.Where(t => t.ReceiverWallet!.User.Merchant != null)
                .GroupBy(t => new { t.ReceiverWallet!.User.Merchant!.Id, t.ReceiverWallet.User.Merchant.BusinessName })
                .Select(g => new TopMerchantDto { MerchantId = g.Key.Id, BusinessName = g.Key.BusinessName, Amount = g.Sum(x => x.Amount), TransactionCount = g.Count() })
                .OrderByDescending(x => x.Amount).Take(5).ToListAsync(cancellationToken)
        };
        return new AppResponse<StudentDashboardDto>(dto, "Student dashboard retrieved successfully.");
    }

    public async Task<AppResponse<MerchantDashboardDto>> GetMerchantDashboardAsync(string userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.Wallet.AsNoTracking().Where(w => w.UserId == userId).Select(w => new { w.Id }).SingleOrDefaultAsync(cancellationToken);
        if (wallet is null) return new AppResponse<MerchantDashboardDto>("Wallet not found.");
        var now = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var weekStart = today.AddDays(-((7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7));
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlyChartStart = monthStart.AddMonths(-5);
        var successful = _context.Transaction.AsNoTracking().Where(t => t.Status == TransactionStatus.Successful);
        var revenue = successful.Where(t => t.ReceiverWalletId == wallet.Id && t.Type != TransactionType.Deposit);
        var withdrawals = _context.Transaction.AsNoTracking().Where(t => t.SenderWalletId == wallet.Id && t.Type == TransactionType.Withdrawal);

        var dto = new MerchantDashboardDto
        {
            RevenueToday = await revenue.Where(t => t.CreatedAt >= today).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            RevenueWeek = await revenue.Where(t => t.CreatedAt >= weekStart).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            RevenueMonth = await revenue.Where(t => t.CreatedAt >= monthStart).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            TotalRevenue = await revenue.SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            CustomerCount = await revenue.Where(t => t.SenderWalletId != null).Select(t => t.SenderWalletId).Distinct().CountAsync(cancellationToken),
            RevenueTransactionCount = await revenue.CountAsync(cancellationToken),
            AverageTransactionValue = await revenue.AverageAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            WithdrawalStatistics = new WithdrawalStatisticsDto
            {
                TotalAmount = await withdrawals.Where(t => t.Status == TransactionStatus.Successful).SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
                TotalCount = await withdrawals.Where(t => t.Status == TransactionStatus.Successful).CountAsync(cancellationToken),
                PendingCount = await withdrawals.Where(t => t.Status == TransactionStatus.Pending).CountAsync(cancellationToken)
            },
            DailyRevenue = await revenue.Where(t => t.CreatedAt >= today.AddDays(-6)).GroupBy(t => t.CreatedAt.Date)
                .Select(g => new TimeSeriesPointDto { Period = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderBy(x => x.Period).ToListAsync(cancellationToken),
            MonthlyRevenue = await revenue.Where(t => t.CreatedAt >= monthlyChartStart).GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new TimeSeriesPointDto { Period = new DateTime(g.Key.Year, g.Key.Month, 1), Amount = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderBy(x => x.Period).ToListAsync(cancellationToken),
            RevenueByTransactionType = await revenue.GroupBy(t => t.Type)
                .Select(g => new TransactionTypeBreakdownDto { Type = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() }).ToListAsync(cancellationToken),
            TopCustomers = await revenue.Where(t => t.SenderWallet != null).GroupBy(t => new { t.SenderWallet!.User.FirstName, t.SenderWallet.User.LastName })
                .Select(g => new TopCustomerDto { CustomerName = g.Key.FirstName + " " + g.Key.LastName, Amount = g.Sum(x => x.Amount), TransactionCount = g.Count() })
                .OrderByDescending(x => x.Amount).Take(5).ToListAsync(cancellationToken)
        };
        return new AppResponse<MerchantDashboardDto>(dto, "Merchant dashboard retrieved successfully.");
    }

    public async Task<AppResponse<SchoolDashboardDto>> GetSchoolDashboardAsync(string userId, CancellationToken cancellationToken = default)
    {
        var schoolCode = await _context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.SchoolCode).SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(schoolCode)) return new AppResponse<SchoolDashboardDto>("School assignment not found.");
        var now = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var schoolWallets = _context.Wallet.AsNoTracking().Where(w => w.User.SchoolCode == schoolCode);
        var transactions = _context.Transaction.AsNoTracking().Where(t => t.Status == TransactionStatus.Successful && t.ReceiverWallet!.User.SchoolCode == schoolCode);
        
        var dto = new SchoolDashboardDto
        {
            StudentCount = await (
                from user in _context.Users.AsNoTracking()
                join userRole in _context.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where user.SchoolCode == schoolCode && role.Name == "Student"
                select user.Id).CountAsync(cancellationToken),
            MerchantCount = await _context.Merchant.AsNoTracking().CountAsync(m => m.User.SchoolCode == schoolCode, cancellationToken),
            PendingMerchantApprovals = await _context.Merchant.AsNoTracking().CountAsync(m => m.User.SchoolCode == schoolCode && !m.IsApproved, cancellationToken),
            WalletBalance = await schoolWallets.SumAsync(w => (decimal?)w.Balance, cancellationToken) ?? 0,
            TransactionVolume = await transactions.SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            SuccessfulTransactionCount = await transactions.CountAsync(cancellationToken),
            LockedWalletCount = await schoolWallets.CountAsync(w => w.IsLocked, cancellationToken),
            DailyTransactionVolume = await transactions.Where(t => t.CreatedAt >= today.AddDays(-6)).GroupBy(t => t.CreatedAt.Date)
                .Select(g => new TimeSeriesPointDto { Period = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() }).OrderBy(x => x.Period).ToListAsync(cancellationToken),
            MonthlyTransactionVolume = await transactions.Where(t => t.CreatedAt >= monthStart.AddMonths(-5)).GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new TimeSeriesPointDto { Period = new DateTime(g.Key.Year, g.Key.Month, 1), Amount = g.Sum(x => x.Amount), Count = g.Count() }).OrderBy(x => x.Period).ToListAsync(cancellationToken),
            TransactionTypes = await transactions.GroupBy(t => t.Type)
                .Select(g => new TransactionTypeBreakdownDto { Type = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() }).ToListAsync(cancellationToken)
        };
        return new AppResponse<SchoolDashboardDto>(dto, "School dashboard retrieved successfully.");
    }

    public async Task<AppResponse<SystemDashboardDto>> GetSystemDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var chartStart = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var allTransactions = _context.Transaction.AsNoTracking();
        var successfulTransactions = allTransactions.Where(t => t.Status == TransactionStatus.Successful);
        var totalTransactionCount = await allTransactions.CountAsync(cancellationToken);
        var successfulTransactionCount = await successfulTransactions.CountAsync(cancellationToken);
        var dto = new SystemDashboardDto
        {
            TotalUsers = await _context.Users.AsNoTracking().CountAsync(cancellationToken),
            TotalSchools = await _context.School.AsNoTracking().CountAsync(cancellationToken),
            TotalStudents = await (from userRole in _context.UserRoles.AsNoTracking() join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id where role.Name == "Student" select userRole.UserId).CountAsync(cancellationToken),
            TotalMerchants = await _context.Merchant.AsNoTracking().CountAsync(cancellationToken),
            ApprovedMerchantCount = await _context.Merchant.AsNoTracking().CountAsync(m => m.IsApproved, cancellationToken),
            PlatformVolume = await successfulTransactions.SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0,
            WalletBalance = await _context.Wallet.AsNoTracking().SumAsync(w => (decimal?)w.Balance, cancellationToken) ?? 0,
            SuccessfulTransactionCount = successfulTransactionCount,
            FailedTransactionCount = await allTransactions.CountAsync(t => t.Status == TransactionStatus.Failed, cancellationToken),
            TransactionSuccessRate = totalTransactionCount == 0 ? 0 : Math.Round((decimal)successfulTransactionCount / totalTransactionCount * 100, 2),
            MonthlyUserGrowth = await _context.Users.AsNoTracking().Where(u => u.CreatedAt >= chartStart).GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new TimeSeriesPointDto { Period = new DateTime(g.Key.Year, g.Key.Month, 1), Count = g.Count() }).OrderBy(x => x.Period).ToListAsync(cancellationToken)
            ,MonthlyTransactionVolume = await successfulTransactions.Where(t => t.CreatedAt >= chartStart).GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new TimeSeriesPointDto { Period = new DateTime(g.Key.Year, g.Key.Month, 1), Amount = g.Sum(x => x.Amount), Count = g.Count() }).OrderBy(x => x.Period).ToListAsync(cancellationToken),
            DailyTransactionVolume = await successfulTransactions.Where(t => t.CreatedAt >= today.AddDays(-29)).GroupBy(t => t.CreatedAt.Date)
                .Select(g => new TimeSeriesPointDto { Period = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() }).OrderBy(x => x.Period).ToListAsync(cancellationToken),
            TransactionTypes = await successfulTransactions.GroupBy(t => t.Type)
                .Select(g => new TransactionTypeBreakdownDto { Type = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() }).ToListAsync(cancellationToken)
        };
        return new AppResponse<SystemDashboardDto>(dto, "System dashboard retrieved successfully.");
    }
}
