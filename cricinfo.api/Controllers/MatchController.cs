using System;
using System.Threading.Tasks;
using Cricinfo.Models;
using Cricinfo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cricinfo.Api.Controllers
{
    [ApiController]
    [Route("api/")]
    public class MatchController : ControllerBase
    {
        private readonly ICricInfoCommandService cricInfoCommandService;
        private readonly ICricInfoQueryService cricInfoQueryService;
        private readonly ILogger<MatchController> _logger;

        public MatchController(ICricInfoCommandService cricInfoCommandService,
            ICricInfoQueryService cricInfoQueryService,
            ILogger<MatchController> logger)
        {
            this.cricInfoCommandService = cricInfoCommandService;
            this.cricInfoQueryService = cricInfoQueryService;
            this._logger = logger;
        }

        [HttpGet()]
        [Route("CheckMatchExists")]
        public async Task<IActionResult> CheckMatchExistsAsync(
            [FromQuery] string homeTeam, [FromQuery] string awayTeam, [FromQuery] DateTime? date)
        {
            try
            {
                if (homeTeam == null || awayTeam == null || date == null) { return BadRequest(); }

                this._logger.LogInformation($"GET request - Home Team: '{homeTeam}, Away Team: {awayTeam}, Date: {date}'");

                var result = await this.cricInfoQueryService.MatchExistsAsync(homeTeam, awayTeam, date.Value);

                return Ok(result);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }
        }

        [HttpPost()]
        public async Task<IActionResult> CreateMatchAsync([FromBody]Match match)
        {
            try
            {
                if (match == null) { return BadRequest(); }

                this._logger.LogInformation($"POST request - Home Team: '{match.HomeTeam}, Away Team: {match.AwayTeam}, Date: {match.DateOfFirstDay.ToLongDateString()}'");

                var (dataCreationResponse, id) = await this.cricInfoCommandService.CreateMatchAsync(match);

                if (dataCreationResponse == DataCreationResponse.DuplicateContent) { return StatusCode(409); }
                if (dataCreationResponse == DataCreationResponse.Failure) { return StatusCode(500); }
                if (dataCreationResponse == DataCreationResponse.Success && id == null) { return StatusCode(500); }

                this._logger.LogInformation($"Success - Content created at id '{id}'");

                return CreatedAtRoute("GetMatchAsync", new { id = id }, match);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }
        }

        [HttpPost()]
        [Route("Team")]
        public async Task<IActionResult> CreateTeamAsync([FromQuery] string team)
        {
            try
            {
                if (team == null || team == "") { return BadRequest(); }

                var dataCreationResponse = await this.cricInfoCommandService.CreateTeamAsync(team);

                if (dataCreationResponse == DataCreationResponse.DuplicateContent) { return StatusCode(409); }
                if (dataCreationResponse == DataCreationResponse.Failure) { return StatusCode(500); }

                return StatusCode(201);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }
        }

        [HttpGet(Name = "GetMatchAsync")]
        [Route("Match/{id?}")]
        public async Task<IActionResult> GetMatchAsync(int? id)
        {
            try
            {
                if (id == null)
                {
                    return Ok(await this.cricInfoQueryService.GetAllMatchesAsync());
                }

                this._logger.LogInformation($"GET request - Match ID '{id.Value}'");

                var match = await this.cricInfoQueryService.GetMatchAsync(id.Value);

                if (match == null) { return NotFound(); }

                return Ok(match);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }
        }

        [HttpGet()]
        [Route("Teams")]
        public async Task<IActionResult> GetTeamsAsync()
        {
            try
            {
                return Ok(await this.cricInfoQueryService.GetTeamsAsync());
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }
        }
    }
}
