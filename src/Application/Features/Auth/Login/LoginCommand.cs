using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
