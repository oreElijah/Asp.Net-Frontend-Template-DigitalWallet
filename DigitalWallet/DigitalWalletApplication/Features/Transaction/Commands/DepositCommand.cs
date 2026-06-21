using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.Transaction.Commands
{
    public record DepositCommand(DepositDto DepositDto, string UserId) : IRequest<AppResponse<DepositResponseDto>>;

    public class DepositCommandHandler(ITransactionService transactionService)
        : IRequestHandler<DepositCommand, AppResponse<DepositResponseDto>>
    {
        public async Task<AppResponse<DepositResponseDto>> Handle(DepositCommand request, CancellationToken cancellationToken)
        {
            var response = await transactionService.DepositAsync(request.DepositDto, request.UserId);
            return response;
        } 
    }
}
