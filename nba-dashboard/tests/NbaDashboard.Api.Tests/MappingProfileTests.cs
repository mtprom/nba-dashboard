using AutoMapper;
using NbaDashboard.Api.DTOs;

namespace NbaDashboard.Api.Tests;

public class MappingProfileTests
{
    [Fact]
    public void AutoMapper_ConfigurationIsValid()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MappingProfile>());

        config.AssertConfigurationIsValid();
    }
}
