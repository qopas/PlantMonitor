using FluentValidation;
using PlantMonitor.Application.Features.SensorData.Commands;

namespace PlantMonitor.Application.Validators;

public class RecordSensorDataCommandValidator : AbstractValidator<RecordSensorDataCommand>
{
    public RecordSensorDataCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("Device ID is required");

        RuleFor(x => x.SoilMoisture)
            .InclusiveBetween(0, 100)
            .WithMessage("Soil moisture must be between 0% and 100%");

        RuleFor(x => x.WaterLevel)
            .InclusiveBetween(0, 100)
            .WithMessage("Water level must be between 0% and 100%");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(-50, 100)
            .WithMessage("Temperature must be between -50Â°C and 100Â°C")
            .When(x => x.Temperature.HasValue);

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("Timestamp is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Timestamp cannot be more than 5 minutes in the future");
    }
}
