using FluentValidation;
using PlantMonitor.Application.Features.Devices.Commands;

namespace PlantMonitor.Application.Validators;

public class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("Device ID is required")
            .Matches(@"^PM-[A-F0-9]+$")
            .WithMessage("Device ID must be in format PM-XXXXXX");

        RuleFor(x => x.UserEmail)
            .NotEmpty()
            .WithMessage("User email is required")
            .EmailAddress()
            .WithMessage("Valid email address is required");

        RuleFor(x => x.DeviceName)
            .MaximumLength(100)
            .WithMessage("Device name cannot exceed 100 characters");
    }
}

public class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Valid device ID is required");

        RuleFor(x => x.DeviceName)
            .MaximumLength(100)
            .WithMessage("Device name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.DeviceName));

        RuleFor(x => x.Location)
            .MaximumLength(200)
            .WithMessage("Location cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Location));
    }
}
