using DigitalWalletApi.Extensions;
using DigitalWalletApi.Filter;
using DigitalWalletApplication.Features.School.Commands;
using DigitalWalletApplication.Features.School.Queries;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Entities;
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
    public class SchoolController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SchoolController> _logger;
        private readonly UserManager<AppUser> _userManager;

        public SchoolController(IMediator mediator, ILogger<SchoolController> logger, UserManager<AppUser> userManager)
        {
            _mediator = mediator;
            _logger = logger;
            _userManager = userManager;
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPost("Add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddSchoolToPlatform([FromBody] SchoolRequestDto schoolRequestDto)
        {
            _logger.LogInformation("Received request to add school with Name: {schoolName} and Code: {schoolCode}", schoolRequestDto.Name, schoolRequestDto.Code);
            var command = new AddSchoolCommand(schoolRequestDto);
            var response = await _mediator.Send(command);
            _logger.LogInformation("Completed request to add school with Name: {schoolName} and Code: {schoolCode}. Response: {response}", schoolRequestDto.Name, schoolRequestDto.Code, response);
            return CreatedAtAction(nameof(GetSchoolById), new { schoolId = response.Data.Id }, response);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpDelete("Delete/{schoolId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveSchoolFromPlatform([FromRoute] Guid schoolId)
        {
            _logger.LogInformation("Received request to delete school with ID: {schoolId}", schoolId);
            var command = new DeleteSchoolCommand(schoolId);
            var response = await _mediator.Send(command);
            _logger.LogInformation("Completed request to delete school with ID: {schoolId}. Response: {response}", schoolId, response);
            return Ok(response);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpPut("Update/{schoolId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSchool([FromRoute] Guid schoolId, [FromBody] SchoolUpdateDto schoolUpdateDto)
        {
            _logger.LogInformation("Received request to update school with ID: {schoolId}", schoolId);
            var command = new UpdateSchoolCommand(schoolId, schoolUpdateDto);
            var response = await _mediator.Send(command);
            _logger.LogInformation("Completed request to update school with ID: {schoolId}. Response: {response}", schoolId, response);
            return Ok(response);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllSchools()
        {
            _logger.LogInformation("Received request to get all schools.");
            var query = new GetAllSchoolsQuery();
            var response = await _mediator.Send(query);
            _logger.LogInformation("Completed request to get all schools. Response: {response}", response);
            return Ok(response);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("Code/{code}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSchoolByCode([FromRoute] string code)
        {
            _logger.LogInformation("Received request to get school with Code: {schoolCode}", code);
            var query = new GetSchoolByCodeQuery(code);
            var response = await _mediator.Send(query);
            _logger.LogInformation("Completed request to get school with Code: {schoolCode}. Response: {response}", code, response);
            return Ok(response);
        }


        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("ID/{schoolId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSchoolById([FromRoute] Guid schoolId)
        {
            _logger.LogInformation("Received request to get school with ID: {schoolId}", schoolId);
            var query = new GetSchoolByIdQuery(schoolId);
            var response = await _mediator.Send(query);
            _logger.LogInformation("Completed request to get school with ID: {schoolId}. Response: {response}", schoolId, response);
            return Ok(response);
        }

        [ServiceFilter(typeof(LogActionFilter))]
        [HttpGet("Users")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> GetSchoolUsers()
        {
            _logger.LogInformation("Received request to get school users for user with ID: {UserId}", User.GetUserId());
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", User.GetUserId());
                return NotFound("User not found");
            }

            var query = new GetSchoolUsersQuery(user.SchoolCode);
            var schoolUsers = await _mediator.Send(query);

            _logger.LogInformation("Successfully retrieved school users for user with ID: {UserId}", User.GetUserId());
            return Ok(schoolUsers);
        }
    }
}