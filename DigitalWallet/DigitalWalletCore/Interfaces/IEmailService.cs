using DigitalWalletCore.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletCore.Interfaces
{
    public interface IEmailService
    {
        public Task SendVerifyUserEmail(string userName, string email, string verify_token, string walletNumber);
        public Task SendForgotPasswordEmail(string userName, string email, string resetToken);
        public Task VerifyEmail(string email, string token);
        public Task ResetPasswordEmail(string email, string token, string newPassword);
        public Task SendCustomerDepositSuccessfulEmail(string userName, string email, Guid Id, decimal amount, string reference, string walletNumber, TransactionStatus status);
        public Task SendCustomerWithdrawalSuccessfulEmail(string userName, string email, Guid Id, decimal amount, string reference, string accountName, string accountNumber, string bankName, string walletNumber, TransactionStatus status);
        public Task SendMerchantApprovalEmail(string schoolAdminEmail, string schoolAdminName, string merchantBuisnessName, string merchantEmail, DateTime registrationDate);
        public Task SendReceiverTransferSuccessfulEmail(string receiverName, string receiverEmail, Guid transactionId, decimal amount, string reference, string Description, string senderName, TransactionStatus status);
        public Task SendSenderTransferSuccessfulEmail(string senderName, string senderEmail, Guid transactionId, decimal amount, string reference, string Description, string receiverName,  TransactionStatus status);
        public Task SendMerchantApprovalResponseEmail(bool isApproved, string merchantBuisnessName, string merchantEmail, DateTime DecisionDate);       
    }
}
