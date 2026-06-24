using DigitalWalletApi.Extensions;
using DigitalWalletApi.Filter;
using DigitalWalletApplication.Features.Wallet.Commands;
using DigitalWalletApplication.Features.Wallet.Queries;
using DigitalWalletCore.Dtos.Wallet;
using DigitalWalletCore.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWalletApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]/")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly ILogger<WalletController> _logger;
        private readonly IWalletService _walletService;
        private readonly ISender _sender;

        public WalletController(ILogger<WalletController> logger, IWalletService walletService, ISender sender)
        {
            _logger = logger;
            _walletService = walletService;
            _sender = sender;
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("Wallet")]
        [Authorize]
        public async Task<IActionResult> GetWallet()
        {
            _logger.LogInformation("GetWallet called by user {UserId}", User.GetUserId());
            var userId = User.GetUserId();

            _logger.LogInformation("Fetching wallet for user {UserId}", userId);
            var query = new GetWalletByUserIdQuery(userId);

            _logger.LogInformation("Sending query to get wallet for user {UserId}", userId);
            var result = await _sender.Send(query);

            _logger.LogInformation("Returning wallet for user {UserId}", userId);
            return Ok(result);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("Search/WalletNumber")]
        [Authorize]
        public async Task<IActionResult> GetWalletByWalletNumber([FromBody] string walletNumber)
        {
            _logger.LogInformation("GetWalletByWalletNumber called with wallet number {WalletNumber}", walletNumber);
            var query = new GetWalletByWalletNumberQuery(walletNumber, User.GetUserId());
            _logger.LogInformation("Sending query to get wallet by wallet number {WalletNumber}", walletNumber);
            var result = await _sender.Send(query);
            if (result == null)
            {
                _logger.LogWarning("No wallet found for wallet number {WalletNumber}", walletNumber);
                return NotFound();
            }
            _logger.LogInformation("Returning wallet for wallet number {WalletNumber}", walletNumber);
            return Ok(result);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("Transactions")]
        [Authorize]
        public async Task<IActionResult> GetWalletTransactions()
        {
            _logger.LogInformation("GetWalletTransactions called by user {UserId}", User.GetUserId());
            var userId = User.GetUserId();
            var wallet = await _walletService.GetWalletByUserId(userId);
            var WalletId = wallet.Data.Id;
            _logger.LogInformation("Fetching wallet transactions for user {UserId}", userId);
            var query = new GetWalletTransactionsQuery(WalletId, userId);
            _logger.LogInformation("Sending query to get wallet transactions for user {UserId}", userId);
            var result = await _sender.Send(query);
            _logger.LogInformation("Returning wallet transactions for user {UserId}", userId);
            return Ok(result);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("lockOrUnlock")]
        [Authorize(Roles = "Admin, SchoolAdmin")]
        public async Task<IActionResult> LockOrUnlockWallet([FromBody] WalletNumberRequest request)
        {
            _logger.LogInformation("LockOrUnlockWallet called by user {UserId} for wallet {WalletId}", User.GetUserId(), request.WalletNumber);
            var userId = User.GetUserId();
            var command = new LockOrUnlockWalletCommand(request.WalletNumber);
            _logger.LogInformation("Sending command to lock/unlock wallet {WalletId} for user {UserId}", request.WalletNumber, userId);
            var result = await _sender.Send(command);
            _logger.LogInformation("Returning result for lock/unlock wallet {WalletId} for user {UserId}", request.WalletNumber, userId);
            return Ok(result);
        }
    }
}
