using FluentValidation;
using PlantMonitor.Application.Features.Plants.Commands;

namespace PlantMonitor.Application.Validators;

public class CreatePlantCommandValidator : AbstractValidator<CreatePlantCommand>
{
    public CreatePlantCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .GreaterThan(0)
            .WithMessage("Valid device ID is required");

        RuleFor(x => x.PlantName)
            .NotEmpty()
            .WithMessage("Plant name is required")
            .MaximumLength(100)
            .WithMessage("Plant name cannot exceed 100 characters");

        RuleFor(x => x.PlantType)
            .NotEmpty()
            .WithMessage("Plant type is required")
            .MaximumLength(100)
            .WithMessage("Plant type cannot exceed 100 characters");

        RuleFor(x => x.MoistureThresholdLow)
            .InclusiveBetween(10, 50)
            .WithMessage("Low moisture threshold must be between 10% and 50%");

        RuleFor(x => x.MoistureThresholdHigh)
            .InclusiveBetween(60, 90)
            .WithMessage("High moisture threshold must be between 60% and 90%");

        RuleFor(x => x.WateringDuration)
            .InclusiveBetween(5, 60)
            .WithMessage("Watering duration must be between 5 and 60 seconds");

        RuleFor(x => x.AgeWeeks)
            .GreaterThan(0)
            .WithMessage("Age must be greater than 0")
            .When(x => x.AgeWeeks.HasValue);
    }
}
