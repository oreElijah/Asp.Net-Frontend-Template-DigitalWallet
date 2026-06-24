using System;
using System.Collections.Generic;
using System.Text;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Enums;

namespace DigitalWalletInfrastructure.Mapper
{
    public static class TransactionMapper
    {
        public static TransactionDto ToTransactionResponseDto(this Transaction transaction)
        {
            return new TransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Reference = transaction.Reference,
                ReceiverWalletName = transaction.ReceiverWallet?.User.FirstName + " " + transaction.ReceiverWallet?.User.LastName,
                SenderWalletName = transaction.SenderWallet?.User.FirstName + " " + transaction.SenderWallet?.User.LastName,
                Type = transaction.Type,
                Status = transaction.Status,
                SenderWalletNumber = transaction.SenderWalletNumber,
                ReceiverWalletNumber = transaction.ReceiverWalletNumber,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt                
            };
        }

        public static TransactionDto ToTransactionResponseDto2(this Transaction transaction, Wallet receiverWallet, Wallet senderWallet)
        {
            return new TransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Reference = transaction.Reference,
                ReceiverWalletName = receiverWallet?.User.FirstName + " " + transaction.ReceiverWallet?.User.LastName,
                SenderWalletName = senderWallet?.User.FirstName + " " + transaction.SenderWallet?.User.LastName,
                Type = transaction.Type,
                Status = transaction.Status,
                SenderWalletNumber = transaction.SenderWalletNumber,
                ReceiverWalletNumber = transaction.ReceiverWalletNumber,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            };
        }

        public static Transaction ToTransaction(this TransactionDto transactionDto, Guid senderWalletId, Guid receiverWalletId)
        {
            return new Transaction
            {
                Id = transactionDto.Id,
                Amount = transactionDto.Amount,
                Reference = transactionDto.Reference,
                ReceiverWalletNumber = transactionDto.ReceiverWalletNumber,
                SenderWalletNumber = transactionDto.SenderWalletNumber,
                Type = transactionDto.Type,
                Status = transactionDto.Status,
                Description = transactionDto.Description,
                SenderWalletId = senderWalletId,
                ReceiverWalletId = receiverWalletId,
                CreatedAt = transactionDto.CreatedAt
            };
        }

        public static List<TransactionDto> ToTransactionResponseDtoList(this List<Transaction> transactions)
        {
            var transactionDtos = new List<TransactionDto>();
            foreach (var transaction in transactions)
            {
                transactionDtos.Add(transaction.ToTransactionResponseDto());
            }
            return transactionDtos;
        }

        public static List<Transaction> ToTransactionList(this List<TransactionDto> transactionDtos, Guid senderWalletId, Guid receiverWalletId)
        {
            var transactions = new List<Transaction>();
            foreach (var transactionDto in transactionDtos)
            {
                transactions.Add(transactionDto.ToTransaction(senderWalletId, receiverWalletId));
            }
            return transactions;
        }

        public static Transaction ToTransactionFromDeposit(this DepositDto depositDto, string WalletNumber, TransactionStatus status, string reference, Guid receiverWalletId)
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = depositDto.Amount,
                Reference = reference,
                ReceiverWalletNumber = WalletNumber,
                Type = TransactionType.Deposit,
                Status = status,
                ReceiverWalletId = receiverWalletId,
                SenderWalletId = null,
                SenderWalletNumber = "EXTERNAL",
                Description = $"Deposit to wallet {WalletNumber}",
                CreatedAt = DateTime.UtcNow,
            };
        }

        public static Transaction ToTransactionFromWithdrawal(this WithdrawDto withdrawDto, string WalletNumber, TransactionStatus status, string reference, Guid senderWalletId)
        {
            return new Transaction
            {
                Amount = withdrawDto.Amount,
                Reference = reference,

                Type = TransactionType.Withdrawal,
                Status = status,

                SenderWalletId = senderWalletId,
                SenderWalletNumber = WalletNumber,
                ReceiverWalletId = null,
                ReceiverWalletNumber = "EXTERNAL",

                Description = "Withdrawal to bank account",

                CreatedAt = DateTime.UtcNow
            };
        }

        public static Transaction ToTransactionFromTransfer(this TransferDto transferDto, string WalletNumber, TransactionStatus status, Guid senderWalletId, Guid receiverWalletId)
        {
            return new Transaction
            {
                Amount = transferDto.Amount,
                Reference = $"TRF-{Guid.NewGuid():N}",

                Type = TransactionType.Transfer,
                Status = status,

                SenderWalletId = senderWalletId,
                SenderWalletNumber = WalletNumber,
                ReceiverWalletId = receiverWalletId,
                ReceiverWalletNumber = transferDto.ReceiverWalletNumber,
                
                Description = transferDto.Description,

                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
