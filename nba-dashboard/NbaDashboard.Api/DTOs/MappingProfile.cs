using AutoMapper;
using NbaDashboard.Core.Entities;

namespace NbaDashboard.Api.DTOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Team, TeamDto>();

        CreateMap<Game, GameDto>()
            .ForMember(d => d.Date, opt => opt.MapFrom(s => s.Date.ToString("O")));

        CreateMap<PlayerGameStats, PlayerGameStatsDto>()
            .ForMember(d => d.PlayerName,
                opt => opt.MapFrom(s => s.Player.FirstName + " " + s.Player.LastName))
            .ForMember(d => d.Position,
                opt => opt.MapFrom(s => s.Player.Position))
            .ForMember(d => d.JerseyNumber,
                opt => opt.MapFrom(s => s.Player.JerseyNumber));

        CreateMap<PlayerSeasonStats, PlayerSeasonAvgDto>()
            .ForMember(d => d.PlayerName,
                opt => opt.MapFrom(s => s.Player.FirstName + " " + s.Player.LastName))
            .ForMember(d => d.Position,
                opt => opt.MapFrom(s => s.Player.Position))
            .ForMember(d => d.JerseyNumber,
                opt => opt.MapFrom(s => s.Player.JerseyNumber));
    }
}
