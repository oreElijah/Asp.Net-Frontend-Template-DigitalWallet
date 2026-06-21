using DigitalWalletApi.Extensions;
using DigitalWalletApi.Filter;
using DigitalWalletApplication.Features.Transaction.Commands;
using DigitalWalletApplication.Features.Transaction.Queries;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWalletApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]/")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ILogger<TransactionController> _logger;
        private readonly IWalletService _walletService;
        private readonly ISender _sender;

        public TransactionController(ILogger<TransactionController> logger, ISender sender, IWalletService walletService)
        {
            _logger = logger;
            _walletService = walletService;
            _sender = sender;
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllTransactionsForUser()
        {
            _logger.LogInformation("User {UserId} is requesting all transactions", User.Identity?.Name);
            var userId = User.GetUserId();
            var wallet = await _walletService.GetWalletByUserId(userId);
            var WalletId = wallet.Data.Id;

            _logger.LogInformation("Getting all transactions for user {UserId}", User.Identity?.Name);
            var query = new GetTransactionsByWalletIdQuery(WalletId, userId);
            var result = await _sender.Send(query);

            _logger.LogInformation("Retrieved {Count} transactions for user {UserId}", result.Data.Count, User.Identity?.Name);
            return Ok(result);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("Deposit")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Deposit([FromBody] DepositDto request)
        {
            var userId = User.GetUserId();

            var command = new DepositCommand(request, userId);
            var result = await _sender.Send(command);

            return Ok(result);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("Withdrawal")]
        [Authorize(Roles = "Merchant")]
        public async Task<IActionResult> Withdrawal([FromBody] WithdrawDto request)
        {
            var userId = User.GetUserId();

            var command = new WithdrawCommand(request, userId);
            var result = await _sender.Send(command);

            return Ok(result);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("Transfer")]
        [Authorize(Roles = "Student, Merchant")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto request)
        {
            var userId = User.GetUserId();

            var command = new TransferCommand(request, userId);
            var result = await _sender.Send(command);

            return Ok(result);
        }

    }
}
