using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.School.Queries
{
    public record GetAllSchoolsQuery() : IRequest<AppResponse<List<SchoolResponseDto>>>;
    public class GetAllSchoolsQueryHandler(ISchoolService schoolService)
        : IRequestHandler<GetAllSchoolsQuery, AppResponse<List<SchoolResponseDto>>>
    {
        public async Task<AppResponse<List<SchoolResponseDto>>> Handle(GetAllSchoolsQuery request, CancellationToken cancellationToken)
        {
            var response = await schoolService.GetAllSchoolsAsync();
            return response;
        }
    }
}
