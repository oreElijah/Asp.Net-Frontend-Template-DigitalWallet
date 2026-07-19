using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Merchant;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.Transaction.Commands
{
    public record ScanToChargeCommand(BarcodeScanDto BarcodeScanDto, decimal Amount, string UserId, string Pin) : IRequest<AppResponse<TransactionDto>>;

    public class ScanToChargeCommandHandler(ITransactionService transactionService)
        : IRequestHandler<ScanToChargeCommand, AppResponse<TransactionDto>>
    {
        public async Task<AppResponse<TransactionDto>> Handle(ScanToChargeCommand request, CancellationToken cancellationToken)
        {
            var response = await transactionService.ScanToChargeWallet(request.BarcodeScanDto, request.Amount, request.UserId, request.Pin);
            return response;
        }
    }
}
