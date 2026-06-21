using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.Transaction.Commands
{
    public record TransferCommand(TransferDto TransferDto, string UserId) : IRequest<AppResponse<TransactionDto>>;

    public class TransferCommandHandler(ITransactionService transactionService)
        : IRequestHandler<TransferCommand, AppResponse<TransactionDto>>
    {
        public async Task<AppResponse<TransactionDto>> Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            var response = await transactionService.TransferAsync(request.TransferDto, request.UserId);
            return response;
        }
    }
}
