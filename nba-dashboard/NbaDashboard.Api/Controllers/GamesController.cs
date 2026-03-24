using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NbaDashboard.Api.DTOs;
using NbaDashboard.Infrastructure.Data;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace NbaDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private readonly NbaStatsClient _nba;
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GamesController> _logger;
    private const string CacheKey = "scoreboard_today";

    public GamesController(NbaStatsClient nba, AppDbContext db, IMemoryCache cache, ILogger<GamesController> logger)
    {
        _nba = nba;
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<List<UpcomingGameDto>>> GetUpcoming(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKey, out List<UpcomingGameDto>? cached))
            return Ok(cached);

        try
        {
            var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            var todayEt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, eastern);
            var dateStr = todayEt.ToString("MM/dd/yyyy");

            var resp = await _nba.GetAsync<ScoreboardV2Response>("scoreboardv2",
                new Dictionary<string, string>
                {
                    ["GameDate"] = dateStr,
                    ["LeagueID"] = "00",
                    ["DayOffset"] = "0",
                }, ct);

            if (resp == null)
                return Ok(new List<UpcomingGameDto>());

            var result = await ParseScoreboard(resp, ct);

            _cache.Set(CacheKey, result, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch scoreboard from NBA API");
            return Ok(new List<UpcomingGameDto>());
        }
    }

    private async Task<List<UpcomingGameDto>> ParseScoreboard(ScoreboardV2Response resp, CancellationToken ct)
    {
        var gameHeader = resp.ResultSets.FirstOrDefault(rs => rs.Name == "GameHeader");
        var lineScore = resp.ResultSets.FirstOrDefault(rs => rs.Name == "LineScore");

        if (gameHeader == null)
            return [];

        var gh = gameHeader.Headers;
        int gameIdIdx = gh.IndexOf("GAME_ID");
        int statusIdx = gh.IndexOf("GAME_STATUS_TEXT");
        int homeTeamIdx = gh.IndexOf("HOME_TEAM_ID");
        int visitorTeamIdx = gh.IndexOf("VISITOR_TEAM_ID");
        int arenaIdx = gh.IndexOf("ARENA_NAME");
        int dateIdx = gh.IndexOf("GAME_DATE_EST");

        // Build lookups from LineScore: scores and team info (fallback when team not in DB)
        var scores = new Dictionary<(string gameId, int teamId), int>();
        var lineScoreTeams = new Dictionary<int, TeamDto>();
        if (lineScore != null)
        {
            var lh = lineScore.Headers;
            int lsGameIdIdx = lh.IndexOf("GAME_ID");
            int lsTeamIdIdx = lh.IndexOf("TEAM_ID");
            int lsPtsIdx = lh.IndexOf("PTS");
            int lsAbbrIdx = lh.IndexOf("TEAM_ABBREVIATION");
            int lsCityIdx = lh.IndexOf("TEAM_CITY_NAME");
            int lsNameIdx = lh.IndexOf("TEAM_NICKNAME");

            foreach (var row in lineScore.RowSet)
            {
                var gid = row[lsGameIdIdx].GetString() ?? "";
                var tid = row[lsTeamIdIdx].GetInt32();
                var pts = lsPtsIdx >= 0 && row[lsPtsIdx].ValueKind != JsonValueKind.Null
                    ? row[lsPtsIdx].GetInt32() : 0;
                scores[(gid, tid)] = pts;

                if (!lineScoreTeams.ContainsKey(tid))
                {
                    var abbr = lsAbbrIdx >= 0 ? (row[lsAbbrIdx].GetString() ?? "") : "";
                    var city = lsCityIdx >= 0 ? (row[lsCityIdx].GetString() ?? "") : "";
                    var name = lsNameIdx >= 0 ? (row[lsNameIdx].GetString() ?? "") : "";
                    lineScoreTeams[tid] = new TeamDto
                    {
                        Id = tid,
                        Abbreviation = abbr,
                        City = city,
                        Name = name,
                        FullName = $"{city} {name}".Trim(),
                    };
                }
            }
        }

        // Pre-load all team IDs we need
        var allTeamIds = gameHeader.RowSet
            .SelectMany(row => new[] { row[homeTeamIdx].GetInt32(), row[visitorTeamIdx].GetInt32() })
            .Distinct()
            .ToList();

        var teams = await _db.Teams
            .Where(t => allTeamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        var result = new List<UpcomingGameDto>();
        var seen = new HashSet<string>();
        foreach (var row in gameHeader.RowSet)
        {
            var gameId = row[gameIdIdx].GetString() ?? "";
            if (!seen.Add(gameId)) continue;
            var homeTeamId = row[homeTeamIdx].GetInt32();
            var visitorTeamId = row[visitorTeamIdx].GetInt32();
            var statusText = row[statusIdx].GetString() ?? "";
            var arena = arenaIdx >= 0 ? (row[arenaIdx].GetString() ?? "") : "";
            var dateRaw = dateIdx >= 0 ? (row[dateIdx].GetString() ?? "") : "";

            // Use DB team data, falling back to LineScore data when team not in DB
            TeamDto homeTeamDto;
            TeamDto visitorTeamDto;

            if (teams.TryGetValue(homeTeamId, out var homeTeam))
            {
                homeTeamDto = new TeamDto
                {
                    Id = homeTeam.Id, Name = homeTeam.Name, FullName = homeTeam.FullName,
                    Abbreviation = homeTeam.Abbreviation, City = homeTeam.City,
                    Conference = homeTeam.Conference, Division = homeTeam.Division,
                };
            }
            else if (lineScoreTeams.TryGetValue(homeTeamId, out var lsHome))
            {
                homeTeamDto = lsHome;
            }
            else continue;

            if (teams.TryGetValue(visitorTeamId, out var visitorTeam))
            {
                visitorTeamDto = new TeamDto
                {
                    Id = visitorTeam.Id, Name = visitorTeam.Name, FullName = visitorTeam.FullName,
                    Abbreviation = visitorTeam.Abbreviation, City = visitorTeam.City,
                    Conference = visitorTeam.Conference, Division = visitorTeam.Division,
                };
            }
            else if (lineScoreTeams.TryGetValue(visitorTeamId, out var lsVisitor))
            {
                visitorTeamDto = lsVisitor;
            }
            else continue;

            scores.TryGetValue((gameId, homeTeamId), out var homeScore);
            scores.TryGetValue((gameId, visitorTeamId), out var visitorScore);

            var status = MapStatus(statusText);
            var date = ParseGameDateTime(dateRaw, statusText);

            result.Add(new UpcomingGameDto
            {
                Game = new GameDto
                {
                    Id = gameId,
                    Date = date,
                    Status = status,
                    HomeTeamId = homeTeamId,
                    VisitorTeamId = visitorTeamId,
                    HomeScore = homeScore,
                    VisitorScore = visitorScore,
                    Arena = arena,
                },
                HomeTeam = homeTeamDto,
                VisitorTeam = visitorTeamDto,
            });
        }

        return result;
    }

    private static string MapStatus(string statusText)
    {
        if (statusText.Contains("Final", StringComparison.OrdinalIgnoreCase))
            return "Final";
        if (statusText.Contains("ET", StringComparison.OrdinalIgnoreCase)
            || statusText.Contains("PM", StringComparison.OrdinalIgnoreCase)
            || statusText.Contains("AM", StringComparison.OrdinalIgnoreCase))
            return "Scheduled";
        return "In Progress";
    }
    private static readonly Regex GameTimeRegex =
        new(@"(\d{1,2}:\d{2}\s*[AP]M)\s*ET", RegexOptions.IgnoreCase);

    private static string ParseGameDateTime(string dateRaw, string statusText)
    {
        if (!DateTime.TryParse(dateRaw, out var dateOnly))
        {
            return DateTime.UtcNow.ToString("O");
        }

        // Try to extract time like "7:30 PM ET"
        var match = GameTimeRegex.Match(statusText ?? "");

        DateTime easternDateTime;

        if (match.Success)
        {
            // Parse the time portion
            if (!DateTime.TryParse(
                    match.Groups[1].Value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTime))
            {
                parsedTime = DateTime.MinValue;
            }

            easternDateTime = new DateTime(
                dateOnly.Year,
                dateOnly.Month,
                dateOnly.Day,
                parsedTime.Hour,
                parsedTime.Minute,
                0,
                DateTimeKind.Unspecified
            );
        }
        else
        {
            // Fallback for "Final", "Halftime", etc.
            easternDateTime = new DateTime(
                dateOnly.Year,
                dateOnly.Month,
                dateOnly.Day,
                0,
                0,
                0,
                DateTimeKind.Unspecified
            );
        }

        var utc = TimeZoneInfo.ConvertTimeToUtc(
            easternDateTime,
            Eastern
        );

        return utc.ToString("O");
    }
}
