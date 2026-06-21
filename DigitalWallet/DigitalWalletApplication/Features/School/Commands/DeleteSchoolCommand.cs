using DigitalWalletCore.Common;
using DigitalWalletCore.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletApplication.Features.School.Commands
{
    public record DeleteSchoolCommand(Guid SchoolId) : IRequest<AppResponse<bool>>;

    public class DeleteSchoolCommandHandler(ISchoolService schoolService)
        : IRequestHandler<DeleteSchoolCommand, AppResponse<bool>>
    {
        public async Task<AppResponse<bool>> Handle(DeleteSchoolCommand request, CancellationToken cancellationToken)
        {
            var response = await schoolService.DeleteSchoolAsync(request.SchoolId);
            return response;
        }
    }
}
