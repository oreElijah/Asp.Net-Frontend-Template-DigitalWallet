namespace DigitalWalletCore.Interfaces;

public interface ITransactionReceiptService
{
    Task<byte[]?> GenerateAsync(Guid transactionId, string userId, CancellationToken cancellationToken = default);
}
