using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Application.Common.Models;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Application.Features.Auth.Commands;
using PlantMonitor.Domain.Entities;

namespace PlantMonitor.Application.Features.Auth.Handlers;

public class RegisterUserHandler(
    UserManager<User> userManager,
    IMapper mapper,
    ILogger<RegisterUserHandler> logger)
    : IRequestHandler<RegisterUserCommand, ApiResponse<UserDto>>
{
    public async Task<ApiResponse<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return ApiResponse<UserDto>.ErrorResult("User with this email already exists");
            }

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                TimeZone = request.TimeZone,
                EmailConfirmed = true 
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ApiResponse<UserDto>.ErrorResult(errors);
            }

            logger.LogInformation("User {Email} registered successfully", request.Email);

            var userDto = mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.SuccessResult(userDto, "User registered successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering user {Email}", request.Email);
            return ApiResponse<UserDto>.ErrorResult("Internal server error");
        }
    }
}

public class GenerateDeviceTokenHandler(
    IDeviceTokenService tokenService,
    IApplicationDbContext context,
    ILogger<GenerateDeviceTokenHandler> logger)
    : IRequestHandler<GenerateDeviceTokenCommand, ApiResponse<string>>
{
    public async Task<ApiResponse<string>> Handle(GenerateDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<string>.ErrorResult("Device not found");
            }

            var token = await tokenService.GenerateTokenAsync(device.Id, request.TokenName);

            logger.LogInformation("Token generated for device {DeviceId}", request.DeviceId);

            return ApiResponse<string>.SuccessResult(token, "Token generated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating token for device {DeviceId}", request.DeviceId);
            return ApiResponse<string>.ErrorResult("Internal server error");
        }
    }
}

public class ValidateDeviceTokenHandler : IRequestHandler<ValidateDeviceTokenCommand, ApiResponse<bool>>
{
    private readonly IDeviceTokenService _tokenService;
    private readonly IApplicationDbContext _context;

    public ValidateDeviceTokenHandler(
        IDeviceTokenService tokenService,
        IApplicationDbContext context)
    {
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(ValidateDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<bool>.SuccessResult(false);
            }

            var isValid = await _tokenService.ValidateTokenAsync(request.Token, device.Id);
            return ApiResponse<bool>.SuccessResult(isValid);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.SuccessResult(false);
        }
    }
}
