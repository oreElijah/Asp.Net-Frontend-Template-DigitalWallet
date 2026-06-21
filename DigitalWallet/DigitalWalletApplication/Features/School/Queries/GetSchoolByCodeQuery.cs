using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.School.Queries
{
    public record GetSchoolByCodeQuery(string SchoolCode) : IRequest<AppResponse<SchoolResponseDto>>;
    
    public class GetSchoolByCodeQueryHandler(ISchoolService schoolService) 
        : IRequestHandler<GetSchoolByCodeQuery, AppResponse<SchoolResponseDto>>
    {
        public async Task<AppResponse<SchoolResponseDto>> Handle(GetSchoolByCodeQuery request, CancellationToken cancellationToken)
        {
            var response = await schoolService.GetSchoolByCodeAsync(request.SchoolCode);
            return response;
        }
    }
}
