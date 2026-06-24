using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using DigitalWalletCore.Dtos.Wallet;    
using DigitalWalletCore.Interfaces;

using DigitalWalletCore.Common;

namespace DigitalWalletApplication.Features.Wallet.Queries
{
    public record GetWalletByUserIdQuery(string UserId) : IRequest<AppResponse<WalletDto>>;

    public class GetWalletByUserIdQueryHandler(IWalletService walletService)
        : IRequestHandler<GetWalletByUserIdQuery, AppResponse<WalletDto>>
    {
        public async Task<AppResponse<WalletDto>> Handle(GetWalletByUserIdQuery request, CancellationToken cancellationToken)
        {
            var response = await walletService.GetWalletByUserId(request.UserId);
            return response;
        }
    }
}