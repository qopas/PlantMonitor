using AutoMapper;
using PlantMonitor.Application.DTOs;
using PlantMonitor.Domain.Entities;

namespace PlantMonitor.Application;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Device, DeviceDto>();
        CreateMap<Plant, PlantDto>();
        CreateMap<SensorData, SensorDataDto>();
        CreateMap<WateringEvent, WateringEventDto>();
        CreateMap<DeviceAlert, DeviceAlertDto>();
        CreateMap<User, UserDto>();
    }
}
