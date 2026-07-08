using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Auth.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IIdentityService identityService,
    IEmailSender emailSender)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var token = await identityService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);

        // user থাকলে email; না থাকলেও একই response — enumeration রোধ
        if (token is not null)
        {
            await emailSender.SendAsync(
                request.Email,
                "Reset your password",
                $"Use this token to reset your password: {token}",
                cancellationToken);
        }

        return Result.Success();
    }
}
