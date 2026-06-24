using DigitalWalletCore.Common;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.Wallet.Commands
{
    public record LockOrUnlockWalletCommand(string WalletNumber) : IRequest<AppResponse<bool>>;

    public class LockOrUnlockWalletCommandHandler(IWalletService walletService)
        : IRequestHandler<LockOrUnlockWalletCommand, AppResponse<bool>>
    {
        public async Task<AppResponse<bool>> Handle(LockOrUnlockWalletCommand request, CancellationToken cancellationToken)
        {
            var response = await walletService.LockOrUnlockWalletAsync(request.WalletNumber);
            return response;
        }
    }
}
