using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.Wallet.Queries
{
    public record GetWalletByWalletNumberQuery(string WalletNumber, string UserId) : IRequest<AppResponse<WalletSearchDto>>;
    public class GetWalletByWalletNumberHandler(IWalletService walletService) : IRequestHandler<GetWalletByWalletNumberQuery, AppResponse<WalletSearchDto>>
    {
        public async Task<AppResponse<WalletSearchDto>> Handle(GetWalletByWalletNumberQuery request, CancellationToken cancellationToken)
        {
            var response = await walletService.GetWalletByWalletNumber(request.WalletNumber, request.UserId);
            return response;
        }
    }
}
