using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.School.Commands
{
    public record UpdateSchoolCommand(Guid SchoolId, SchoolUpdateDto SchoolUpdateDto) : IRequest<AppResponse<SchoolResponseDto>>;

    public class UpdateSchoolCommandHandler(ISchoolService schoolService) 
        : IRequestHandler<UpdateSchoolCommand, AppResponse<SchoolResponseDto>>
    {
        public async Task<AppResponse<SchoolResponseDto>> Handle(UpdateSchoolCommand request, CancellationToken cancellationToken)
        {
            var response = await schoolService.UpdateSchoolAsync(request.SchoolId, request.SchoolUpdateDto);
            return response;
        }
    }
}
