using DigitalWalletCore.Common;
using DigitalWalletCore.Dtos.School;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.School.Queries
{
    public record GetSchoolUsersQuery(string SchoolCode) : IRequest<AppResponse<SchoolUserResponseDto>>;

    public class GetSchoolUsersQueryHandler(ISchoolService schoolService)
        : IRequestHandler<GetSchoolUsersQuery, AppResponse<SchoolUserResponseDto>>
    {
        public async Task<AppResponse<SchoolUserResponseDto>> Handle(GetSchoolUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await schoolService.GetSchoolUsersAsync(request.SchoolCode);
            return users;
        }
    }
}
