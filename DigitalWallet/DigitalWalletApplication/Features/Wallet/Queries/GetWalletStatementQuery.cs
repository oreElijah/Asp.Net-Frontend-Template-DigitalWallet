using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Interfaces;
using MediatR;

namespace DigitalWalletApplication.Features.Wallet.Queries
{
    public record GetWalletStatementQuery(string userId, WalletStatementRequestDto requestDto) : IRequest<WalletStatementResponseDto>;
    public class GetWalletStatementQueryHandler(IWalletService walletService) : IRequestHandler<GetWalletStatementQuery, WalletStatementResponseDto>
    {
        public async Task<WalletStatementResponseDto> Handle(GetWalletStatementQuery request, CancellationToken cancellationToken)
        {
            var response = await walletService.GetWalletStatementAsync(request.userId, request.requestDto);
            return response.Data;
        }
    }
}