using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Payment;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DigitalWalletCore.Interfaces
{
    public interface IPaymentService
    {
        public Task<AppResponse<InitializePaymentResponseDto>> InitializeDepositAsync(Guid TransferId, string walletNumber);
        public Task<AppResponse<InitializePaymentResponseDto>> InitializeWithdrawalAsync(Guid TransferId, string WalletNumber);
        public Task<AppResponse<string>> VerifyDepositAsync(string reference);
        public Task<AppResponse<string>> VerifyWithdrawalAsync(string reference);
        public Task HandleWebhookForDepositAsync(string body);
        public Task HandleWebhookForWithdrawalAsync(string body);
        public Task<string> ResolveAccountAsync(string accountNumber, string bankCode);
        public Task<string> CreateTransferRecipientAsync(string accountName, string accountNumber, string bankCode);
        public Task<string> GetBanksAsync();
        public Task<string?> GetBankNameByCodeAsync(string bankCode);
    }
}
