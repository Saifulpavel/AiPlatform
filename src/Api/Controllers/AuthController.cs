using Asp.Versioning;
using AiPlatform.Api.Common;
using AiPlatform.Api.Contracts;
using AiPlatform.Application.Features.Auth.Login;
using AiPlatform.Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiPlatform.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(IMediator mediator, IWebHostEnvironment environment) : ApiControllerBase
{
    private const string RefreshCookieName = "refreshToken";

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RegisterCommand(request.Email, request.Password, request.FullName), cancellationToken);

        return result.IsSuccess
            ? Created(string.Empty, new { userId = result.Value })
            : ToProblem(result.Error);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error);

        var auth = result.Value;
        SetRefreshCookie(auth.RefreshToken, auth.RefreshTokenExpiresAtUtc);

        // access token শুধু body-তে (frontend memory-তে রাখবে, localStorage নয়)
        return Ok(new { accessToken = auth.AccessToken, expiresAtUtc = auth.AccessTokenExpiresAtUtc });
    }

    // refresh token: httpOnly + SameSite=Strict → JS পড়তে পারে না (XSS-safe)
    private void SetRefreshCookie(string token, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),   // dev http-এ Secure=false, নইলে cookie drop হয়
            SameSite = SameSiteMode.Strict,
            Expires = expiresAtUtc,
            Path = "/api/v1/auth"
        });
    }
}
