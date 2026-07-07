using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Chat;

// Provider null হলে config-এর default ব্যবহার হয়।
public record ChatCommand(string Message, string? Provider) : IRequest<Result<ChatResponse>>;
