using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.Transaction.Queries
{
    public record GetTransactionsByWalletIdQuery(Guid WalletId, string UserId) : IRequest<AppResponse<List<TransactionDto>>>;

    public class GetTransactionsByWalletIdQueryHandler(ITransactionService transactionService)
        : IRequestHandler<GetTransactionsByWalletIdQuery, AppResponse<List<TransactionDto>>>
    {
        public async Task<AppResponse<List<TransactionDto>>> Handle(GetTransactionsByWalletIdQuery request, CancellationToken cancellationToken)
        {
            var response = await transactionService.GetTransactionsByWalletIdAsync(request.WalletId, request.UserId);
            return response;
        }
    }
}
