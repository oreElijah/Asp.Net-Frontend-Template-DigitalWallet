using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.School.Commands
{
    public record AddSchoolCommand(SchoolRequestDto SchoolRequestDto) : IRequest<AppResponse<SchoolResponseDto>>;

    public class AddSchoolCommandHandler(ISchoolService schoolService) 
        : IRequestHandler<AddSchoolCommand, AppResponse<SchoolResponseDto>>
    {
        public async Task<AppResponse<SchoolResponseDto>> Handle(AddSchoolCommand request, CancellationToken cancellationToken)
        {
            var response = await schoolService.AddSchoolToPlatform(request.SchoolRequestDto);
            return response;
        }
    }
}
