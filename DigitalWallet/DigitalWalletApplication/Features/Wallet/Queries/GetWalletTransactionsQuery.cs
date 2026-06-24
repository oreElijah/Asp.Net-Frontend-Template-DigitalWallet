using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.Wallet.Queries
{
    public record GetWalletTransactionsQuery(Guid WalletId, string UserId) : IRequest<AppResponse<WalletResponseDto>>;
    public class GetWalletTransactionsQueryHandler(IWalletService walletService)
        : IRequestHandler<GetWalletTransactionsQuery, AppResponse<WalletResponseDto>>

    {
        public async Task<AppResponse<WalletResponseDto>> Handle(GetWalletTransactionsQuery request, CancellationToken cancellationToken)
        {
            var response = await walletService.GetWalletDetailsById(request.WalletId, request.UserId);
            return response;
        }
    }
}
