using DigitalWalletApi.Extensions;
using DigitalWalletApi.Filter;
using DigitalWalletApplication.Features.Transaction.Commands;
using DigitalWalletApplication.Features.Transaction.Queries;
using DigitalWalletCore.Dtos.Merchant;
using DigitalWalletCore.Dtos.Payment;
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
        private readonly IPaymentService _paymentService;
        private readonly ISender _sender;

        public TransactionController(ILogger<TransactionController> logger, ISender sender, IWalletService walletService, IPaymentService paymentService)
        {
            _logger = logger;
            _walletService = walletService;
            _paymentService = paymentService;
            _sender = sender;
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("history")]
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
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto request)
        {
            var userId = User.GetUserId();

            var command = new TransferCommand(request, userId);
            var result = await _sender.Send(command);

            return Ok(result);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [ApiVersion("1.0")]
        [Authorize]
        [HttpGet("verify/Deposit/{reference}")]
        public async Task<IActionResult> VerifyDeposit(string reference)
        {
            _logger.LogInformation("Reference received by controller: {Reference}", reference);
            var result = await _paymentService.VerifyDepositAsync(reference);

            return Ok(result);
        }


        [ServiceFilter(typeof(LogActionFilter))]
        [ApiVersion("1.0")]
        [Authorize]
        [HttpGet("verify/Withdrawal/{reference}")]
        public async Task<IActionResult> VerifyWithdrawal(string reference)
        {
            var result = await _paymentService.VerifyWithdrawalAsync(reference);

            return Ok(result);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [ApiVersion("1.0")]
        [HttpPost("webhook/Deposit")]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            await _paymentService.HandleWebhookForDepositAsync(body);

            return Ok();
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [ApiVersion("1.0")]
        [HttpPost("webhook/Withdrawal")]
        public async Task<IActionResult> Webhook2()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            await _paymentService.HandleWebhookForWithdrawalAsync(body);

            return Ok();
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("scan_to_charge")]
        [Authorize(Roles = "Merchant")]
        public async Task<IActionResult> ScanToCharge([FromForm] BarcodeScanDto request, [FromForm] decimal amount, [FromForm] string pin)
        {
            var userId = User.GetUserId();

            var command = new ScanToChargeCommand(request, amount, userId, pin);
            var result = await _sender.Send(command);

            return Ok(result);
        }
    }
}
