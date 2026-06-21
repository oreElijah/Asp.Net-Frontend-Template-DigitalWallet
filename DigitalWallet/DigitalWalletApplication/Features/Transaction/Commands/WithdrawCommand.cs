using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.Transaction.Commands
{
    public record WithdrawCommand(WithdrawDto WithdrawDto, string UserId) : IRequest<AppResponse<TransactionDto>>;

    public class WithdrawCommandHandler(ITransactionService transactionService)
        : IRequestHandler<WithdrawCommand, AppResponse<TransactionDto>>
    {
        public async Task<AppResponse<TransactionDto>> Handle(WithdrawCommand request, CancellationToken cancellationToken)
        {
            var response = await transactionService.WithdrawAsync(request.WithdrawDto, request.UserId);
            return response;
        }
    }
}
