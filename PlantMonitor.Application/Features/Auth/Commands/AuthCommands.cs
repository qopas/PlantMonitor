using MediatR;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;

namespace PlantMonitor.Application.Features.Auth.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string TimeZone = "UTC"
) : IRequest<ApiResponse<UserDto>>;

public record LoginUserCommand(string Email, string Password) : IRequest<ApiResponse<string>>;

public record GenerateDeviceTokenCommand(string DeviceId, string? TokenName = null) : IRequest<ApiResponse<string>>;

public record ValidateDeviceTokenCommand(string Token, string DeviceId) : IRequest<ApiResponse<bool>>;
