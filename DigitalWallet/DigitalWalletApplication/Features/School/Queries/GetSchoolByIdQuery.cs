using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.School.Queries
{
    public record GetSchoolByIdQuery(Guid SchoolId) : IRequest<AppResponse<SchoolResponseDto>>;
    public class GetSchoolByIdQueryHandler(ISchoolService schoolService)
        : IRequestHandler<GetSchoolByIdQuery, AppResponse<SchoolResponseDto>>
    {
        public async Task<AppResponse<SchoolResponseDto>> Handle(GetSchoolByIdQuery request, CancellationToken cancellationToken)
        {
            var response = await schoolService.GetSchoolByIdAsync(request.SchoolId);
            return response;
        }
    }
}
