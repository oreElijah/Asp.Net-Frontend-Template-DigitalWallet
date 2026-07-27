using DigitalWalletCore.Enums;

namespace DigitalWalletCore.Dtos.Analytics;

public class TimeSeriesPointDto
{
    public DateTime Period { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class TransactionTypeBreakdownDto
{
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class TopMerchantDto
{
    public Guid MerchantId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
}

public class TopCustomerDto
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
}

public class StudentDashboardDto
{
    public decimal CurrentBalance { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TodaySpending { get; set; }
    public decimal WeeklySpending { get; set; }
    public decimal MonthlySpending { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public List<TimeSeriesPointDto> MonthlySpendingChart { get; set; } = [];
    public List<TimeSeriesPointDto> DailySpending { get; set; } = [];
    public List<TransactionTypeBreakdownDto> TransactionTypes { get; set; } = [];
    public List<TopMerchantDto> TopMerchants { get; set; } = [];
}

public class WithdrawalStatisticsDto
{
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
}

public class MerchantDashboardDto
{
    public decimal RevenueToday { get; set; }
    public decimal RevenueWeek { get; set; }
    public decimal RevenueMonth { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CustomerCount { get; set; }
    public int RevenueTransactionCount { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public WithdrawalStatisticsDto WithdrawalStatistics { get; set; } = new();
    public List<TimeSeriesPointDto> DailyRevenue { get; set; } = [];
    public List<TimeSeriesPointDto> MonthlyRevenue { get; set; } = [];
    public List<TransactionTypeBreakdownDto> RevenueByTransactionType { get; set; } = [];
    public List<TopCustomerDto> TopCustomers { get; set; } = [];
}

public class SchoolDashboardDto
{
    public int StudentCount { get; set; }
    public int MerchantCount { get; set; }
    public int PendingMerchantApprovals { get; set; }
    public decimal WalletBalance { get; set; }
    public decimal TransactionVolume { get; set; }
    public int SuccessfulTransactionCount { get; set; }
    public int LockedWalletCount { get; set; }
    public List<TimeSeriesPointDto> DailyTransactionVolume { get; set; } = [];
    public List<TimeSeriesPointDto> MonthlyTransactionVolume { get; set; } = [];
    public List<TransactionTypeBreakdownDto> TransactionTypes { get; set; } = [];
}

public class SystemDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalSchools { get; set; }
    public int TotalStudents { get; set; }
    public int TotalMerchants { get; set; }
    public int ApprovedMerchantCount { get; set; }
    public decimal PlatformVolume { get; set; }
    public decimal WalletBalance { get; set; }
    public int SuccessfulTransactionCount { get; set; }
    public int FailedTransactionCount { get; set; }
    public decimal TransactionSuccessRate { get; set; }
    public List<TimeSeriesPointDto> MonthlyUserGrowth { get; set; } = [];
    public List<TimeSeriesPointDto> MonthlyTransactionVolume { get; set; } = [];
    public List<TimeSeriesPointDto> DailyTransactionVolume { get; set; } = [];
    public List<TransactionTypeBreakdownDto> TransactionTypes { get; set; } = [];
}
