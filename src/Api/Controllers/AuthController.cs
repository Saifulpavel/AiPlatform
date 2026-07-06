using System.Security.Claims;
using Asp.Versioning;
using AiPlatform.Api.Common;
using AiPlatform.Api.Contracts;
using AiPlatform.Application.Features.Auth.Login;
using AiPlatform.Application.Features.Auth.Logout;
using AiPlatform.Application.Features.Auth.Refresh;
using AiPlatform.Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var result = await mediator.Send(new RefreshCommand(refreshToken), cancellationToken);
        if (result.IsFailure)
        {
            ClearRefreshCookie();   // invalid/reused → cookie মুছে দাও
            return ToProblem(result.Error);
        }

        var auth = result.Value;
        SetRefreshCookie(auth.RefreshToken, auth.RefreshTokenExpiresAtUtc);
        return Ok(new { accessToken = auth.AccessToken, expiresAtUtc = auth.AccessTokenExpiresAtUtc });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
            await mediator.Send(new LogoutCommand(refreshToken), cancellationToken);

        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            fullName = User.FindFirstValue("fullName"),
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
        });
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

    // delete করতে হলে একই attribute (Path/Secure/SameSite) দিয়ে করতে হয়
    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
    }
}
