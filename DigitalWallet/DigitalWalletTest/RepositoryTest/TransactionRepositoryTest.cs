using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.Payment;
using DigitalWalletCore.Dtos.Transaction;
using DigitalWalletCore.Entities;
using DigitalWalletCore.Enums;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using DigitalWalletInfrastructure.Repositories;
using FluentAssertions;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletTest.RepositoryTest
{
    public class TransactionRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private Mock<IPaymentService> _paymentServiceMock;
        private Mock<IQRCodeService> _qrCodeServiceMock;
        private Mock<IWalletService> _walletServiceMock;
        private Mock<ILogger<TransactionRepository>> _loggerMock;
        private Mock<IAuditService> _auditServiceMock;
        private readonly PasswordHasher<Wallet> _pinHasher = new();
        private TransactionRepository _repository;

        public TransactionRepositoryTest()
        {
            _context = GetDbContext().GetAwaiter().GetResult();
            _paymentServiceMock = new Mock<IPaymentService>();
            _qrCodeServiceMock = new Mock<IQRCodeService>();
            _walletServiceMock = new Mock<IWalletService>();
            _loggerMock = new Mock<ILogger<TransactionRepository>>();
            _auditServiceMock = new Mock<IAuditService>();
            _pinHasher = new PasswordHasher<Wallet>();
            _repository = new TransactionRepository(
                _context,
                _paymentServiceMock.Object,
                _walletServiceMock.Object,
                _loggerMock.Object,
                _qrCodeServiceMock.Object,
                _auditServiceMock.Object
            );
        }

        private async Task<ApplicationDbContext> GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();

            if (await context.Transaction.CountAsync() == 0)
            {
                await context.Transaction.AddRangeAsync(
                    new Transaction
                    {
                        Reference = "TXN001",
                        Amount = 100.00m,
                        Description = "Test Transaction 1",
                        Type = TransactionType.Transfer,
                        Status = TransactionStatus.Successful,
                        SenderWalletNumber = "WALLET001",
                        SenderBalanceBefore = 500.00m,
                        SenderBalanceAfter = 400.00m,
                        ReceiverWalletNumber = "WALLET002",
                        ReceiverBalanceBefore = 200.00m,
                        ReceiverBalanceAfter = 300.00m,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Transaction
                    {
                        Reference = "TXN002",
                        Amount = 50.00m,
                        Description = "Test Transaction 2",
                        Type = TransactionType.Deposit,
                        Status = TransactionStatus.Pending,
                        SenderWalletNumber = "WALLET003",
                        SenderBalanceBefore = 300.00m,
                        SenderBalanceAfter = 250.00m,
                        SenderWallet = new Wallet
                        {
                            Id = Guid.NewGuid(),
                            UserId = "user5678",
                            WalletNumber = "WALLET003",
                            Balance = 250.00m,
                            IsLocked = false,
                            User = new AppUser
                            {
                                Id = "user5699",
                                UserName = "Senderuser",
                                Email = "test2@example.com",
                                FirstName = "Sender",
                                LastName = "User",
                                SchoolCode = "SCH001",
                                CreatedAt = DateTime.UtcNow
                            }
                        },
                        ReceiverWallet = new Wallet
                        {
                            Id = Guid.NewGuid(),
                            UserId = "user5679",
                            WalletNumber = "WALLET004",
                            Balance = 150.00m,
                            IsLocked = false,
                            User = new AppUser
                            {
                                Id = "user5679",
                                UserName = "receiveruser",
                                Email = "test@example.com",
                                FirstName = "Receiver",
                                LastName = "User",
                                SchoolCode = "SCH001",
                                CreatedAt = DateTime.UtcNow
                            }
                        },
                        ReceiverWalletNumber = "WALLET004",
                        ReceiverBalanceBefore = 100.00m,
                        ReceiverBalanceAfter = 150.00m,
                        CreatedAt = DateTime.UtcNow
                    }
                );
                await context.SaveChangesAsync();
            }

            return context;
        }

        private Wallet CreateWallet(decimal balance = 1000)
        {
            return new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = "user1234",
                WalletNumber = "STU0001",
                Balance = balance,
                IsLocked = false,
                User = new AppUser
                {
                    Id = "user569",
                    UserName = "user",
                    Email = "test@example.com",
                    FirstName = "User",
                    LastName = "User",
                    SchoolCode = "SCH001",
                    CreatedAt = DateTime.UtcNow
                }
            };
        }

        [Fact]
        public async Task TransactionRepository_DepositAsync_ShouldReturnInvalidAmountResponse()

        {
            // Arrange
            var dto = new DepositDto
            {
                Amount = 0
            };

            // Act
            var result = await _repository.DepositAsync(dto, "user123");

            // Assert   
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Amount must be positive, have at most two decimal places, and not exceed 1,000,000.");
        }

        [Fact]
        public async Task TransactionRepository_DepositAsync_ShouldReturnAppResponseWithMessageWalletNotFound()
        {
            // Arrange
            var depositDto = new DepositDto
            {
                Amount = 100.00m
            };

            var userId = "user123";

            // Act
            var result = await _repository.DepositAsync(depositDto, userId);

            //Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Wallet not found.");
        }

        [Fact]
        public async Task TransactionRepository_DepositAsync_ShouldReturnAppResponseWithMessageWalletIsLocked()
        {
            // Arrange
            var wallet = CreateWallet();
            wallet.IsLocked = true;
            await _context.Wallet.AddAsync(wallet);
            await _context.SaveChangesAsync();
            var depositDto = new DepositDto
            {
                Amount = 100.00m
            };
            var userId = wallet.UserId;
            // Act
            var result = await _repository.DepositAsync(depositDto, userId);
            //Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Be("Wallet is locked.");
        }

        [Fact]
        public async Task TransactionRepository_DepositAsync_ShouldReturnAppResponseWithMessageDepositFailed()

        {
            //Arrange
            var wallet = CreateWallet();

            _context.Wallet.Add(wallet);

            await _context.SaveChangesAsync();

            _paymentServiceMock
                .Setup(x => x.InitializeDepositAsync(It.IsAny<Guid>(), wallet.WalletNumber))
                .ReturnsAsync(new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = false,
                    Message = "Payment gateway failed"
                });

            var dto = new DepositDto
            {
                Amount = 500.00m
            };

            //Act
            _repository = new TransactionRepository(
                _context,
                _paymentServiceMock.Object,
                _walletServiceMock.Object,
                _loggerMock.Object,
                _qrCodeServiceMock.Object,
                _auditServiceMock.Object
            );

            var result = await _repository.DepositAsync(dto, wallet.UserId);

            //Assert
            result.Succeeded.Should().BeFalse();

            result.Message.Should().Be("Payment gateway failed");
        }

        [Fact]
        public async Task TransactionRepository_DepositAsync_ShouldReturnAppResponseWithMessageDepositSuccessful()
        {
            var wallet = CreateWallet();

            _context.Wallet.Add(wallet);

            await _context.SaveChangesAsync();                     
                        
            _paymentServiceMock
                .Setup(x => x.InitializeDepositAsync(It.IsAny<Guid>(), wallet.WalletNumber))
                .ReturnsAsync(new AppResponse<InitializePaymentResponseDto>
                {
                    Succeeded = true,
                    Data = new InitializePaymentResponseDto
                    { 
                        AuthorizationUrl = "https://paymentgateway.com/authorize",
                        Reference = "PAYMENT_REF_12345"
                    },
                    Message = "Payment initiated successfully."
                });

            var dto = new DepositDto
            {
                Amount = 500.00m
            };


            //Act
            var result = await _repository.DepositAsync(dto, wallet.UserId);

            //Assert
            result.Succeeded.Should().BeTrue();
            result.Message.Should().Be("Deposit successful.");
            result.Data.Should().NotBeNull();
            result.Data.PaymentReference.Should().Be("PAYMENT_REF_12345");
            result.Data.PaymentUrl.Should().Be("https://paymentgateway.com/authorize");
        }
    }
}
