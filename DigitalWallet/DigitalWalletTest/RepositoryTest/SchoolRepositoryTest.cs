using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Entities;
using DigitalWalletInfrastructure.Data;
using DigitalWalletInfrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletTest.RepositoryTest
{
    public class SchoolRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private Mock<ILogger<SchoolRepository>> _loggerMock;

        public SchoolRepositoryTest()
        {
            _context = GetDbContext().GetAwaiter().GetResult();
            _loggerMock = new Mock<ILogger<SchoolRepository>>();
        }

        private async Task<ApplicationDbContext> GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();

            if (await context.School.CountAsync() == 0)
            {
                context.School.Add(
                    new School
                    {
                        Name = "Test School",
                        Code = "Test123",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        Users = new List<AppUser> { }
                    }
                );
                await context.SaveChangesAsync();
            }

            return context;
        }

        [Fact]
        public async Task SchoolRepository_AddSchoolToPlatform_ReturnsAppResponseWithMessageFailedToAddSchoolAlreadyExists()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolRequestDto = new SchoolRequestDto
            {
                Name = "Test School",
                Code = "Test123"
            };

            // Act
            var result = await schoolRepository.AddSchoolToPlatform(schoolRequestDto);

            //Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Should().BeOfType<AppResponse<SchoolResponseDto>>();
            result.Message.Should().Be("Failed to add school, school with the same name or code already exists.");
        }

        [Fact]
        public async Task SchoolRepository_AddSchoolToPlatform_ReturnsAppResponseWithMessageSchoolAddedSuccessfully()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolRequestDto = new SchoolRequestDto
            {
                Name = "Test School2",
                Code = "Test124"
            };

            // Act
            var result = await schoolRepository.AddSchoolToPlatform(schoolRequestDto);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Should().BeOfType<AppResponse<SchoolResponseDto>>();
            result.Message.Should().Be("School added successfully");
        }

        [Fact]
        public async Task SchoolRepository_DeleteSchoolAsync_ReturnsAppResponseWithMessageSchoolCouldNotBeDeleted()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolId = Guid.NewGuid();

            // Act
            Func<Task> result = async () = await schoolRepository.DeleteSchoolAsync(schoolId);

            // Assert
            await result.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"School with ID {schoolId} not found.");
        }

        [Fact]
        public async Task SchoolRepository_DeleteSchoolAsync_ReturnsAppResponseWithMessageSchoolDeletedSuccessfully()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var school = await _context.School.FirstOrDefaultAsync();
            var schoolId = school.Id;

            // Act
            var result = await schoolRepository.DeleteSchoolAsync(schoolId);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Should().BeOfType<AppResponse<bool>>();
            result.Message.Should().Be("School deleted successfully.");
        }

        [Fact]
        public async Task SchoolRepository_GetAllSchoolsAsync_ReturnsAppResponseWithMessageSchoolRetrievedSuccessfully()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);

            //Act
            var result = await schoolRepository.GetAllSchoolsAsync();

            //Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Should().BeOfType<AppResponse<List<SchoolResponseDto>>>();
            result.Message.Should().Be("Schools retrieved successfully");
        }

        [Fact]
        public async Task SchoolRepository_GetSchoolByCodeAsync_ReturnsAppRespnseWithMessageFailedToFindSchool()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolCode = "Test124";

            //Act
            var result = await schoolRepository.GetSchoolByCodeAsync(schoolCode);

            //Arrange
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Should().BeOfType<AppResponse<SchoolResponseDto>>();
            result.Message.Should().Be("Failed to find school.");
        }

        [Fact]
        public async Task SchoolRepository_GetSchoolByCodeAsync_ReturnsAppRespnseWithMessageSchoolRetrievedSuccessfully()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolCode = "Test123";

            //Act
            var result = await schoolRepository.GetSchoolByCodeAsync(schoolCode);

            //Arrange
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Should().BeOfType<AppResponse<SchoolResponseDto>>();
            result.Message.Should().Be("School retrieved successfully");
        }

        [Fact]
        public async Task SchoolRepository_GetSchoolByIdAsync_ReturnsAppRespnseWithMessageFailedToFindSchool()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var school = await _context.School.FirstOrDefaultAsync();
            var schoolId = Guid.NewGuid();

            //Act
            var result = await schoolRepository.GetSchoolByIdAsync(schoolId);

            //Arrange
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Should().BeOfType<AppResponse<SchoolResponseDto>>();
            result.Message.Should().Be("Failed to find school.");
        }

        [Fact]
        public async Task SchoolRepository_GetSchoolByIdAsync_ReturnsAppRespnseWithMessageSchoolRetrievedSuccessfully()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var school = await _context.School.FirstOrDefaultAsync();
            var schoolId = school.Id;

            //Act
            var result = await schoolRepository.GetSchoolByIdAsync(schoolId);

            //Arrange
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Should().BeOfType<AppResponse<SchoolResponseDto>>();
            result.Message.Should().Be("School retrieved successfully");
        }

        [Fact]
        public async Task SchoolRepository_GetSchoolUsersAsync_ReturnsAppRespnseWithMessageFailedToFindSchool()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolCode = "Test124";

            //Act
            var result = await schoolRepository.GetSchoolUsersAsync(schoolCode);

            //Arrange
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Should().BeOfType<AppResponse<SchoolUserResponseDto>>();
            result.Message.Should().Be("Failed to find school.");
        }

        [Fact]
        public async Task SchoolRepository_GetSchoolUsersAsync_ReturnsAppRespnseWithMessageSchoolUsersReturnedSuccessfully()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolCode = "Test123";

            //Act
            var result = await schoolRepository.GetSchoolUsersAsync(schoolCode);

            //Arrange
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Should().BeOfType<AppResponse<SchoolUserResponseDto>>();
            result.Message.Should().Be("School users returned succesfully");
        }

        [Fact]
        public async Task SchoolRepository_UpdateSchoolAsync_ReturnsAppResponseWithMessageFailedToFindSchool()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolRequestDto = new SchoolUpdateDto
            {
                Name = "Test School2"
            };
            var schoolId = Guid.NewGuid();

            // Act
            var result = await schoolRepository.UpdateSchoolAsync(schoolId, schoolRequestDto);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Should().BeOfType<AppResponse<SchoolResponseDto>>();
            result.Message.Should().Be("Failed to find school.");
        }

        [Fact]
        public async Task SchoolRepository_UpdateSchoolAsync_ReturnsAppResponseWithMessageFailedToUpdateSchool()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolRequestDto = new SchoolUpdateDto
            {
                Name = "Test School"
            };
            var school = await _context.School.FirstOrDefaultAsync();
            var schoolId = school.Id;

            // Act
            Func<Task> result = async () = await schoolRepository.UpdateSchoolAsync(schoolId, schoolRequestDto);

            // Assert
            await result.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"School with ID {schoolId} not found.");
        }

        [Fact]
        public async Task SchoolRepository_UpdateSchoolAsync_ReturnsAppResponseWithMessageSchoolUpdatedSuccessfully()
        {
            //Arrange
            var schoolRepository = new SchoolRepository(_context, _loggerMock.Object);
            var schoolRequestDto = new SchoolUpdateDto
            {
                Name = "Test School2"
            };
            var school = await _context.School.FirstOrDefaultAsync();
            var schoolId = school.Id;

            // Act
            var result = await schoolRepository.UpdateSchoolAsync(schoolId, schoolRequestDto);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Should().BeOfType<AppResponse<SchoolResponseDto>>();
            result.Message.Should().Be("School Updated Successfully");
        }
    }
}
