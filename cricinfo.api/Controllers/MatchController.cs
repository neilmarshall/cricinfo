using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Cricinfo.Models;
using Cricinfo.Services;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Cricinfo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        /// <summary>
        /// Check if a match exists for the given teams and date
        /// </summary>
        /// <param name="homeTeam"></param>
        /// <param name="awayTeam"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet("Exists")]
        [ProducesResponseType(typeof(bool), Status200OK)]
        public async Task<IActionResult> ExistsAsync(
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

        /// <summary>
        /// Get match details for specified match
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Match), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> GetAsync(int id)
        {
            try
            {
                this._logger.LogInformation($"GET request - Match ID '{id}'");

                var match = await this.cricInfoQueryService.GetMatchAsync(id);

                if (match == null) { return NotFound(); }

                return Ok(match);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Get match details for all matches
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(Match[]), Status200OK)]
        public async Task<IActionResult> GetAsync()
        {
            try
            {
                return Ok(await this.cricInfoQueryService.GetAllMatchesAsync());
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Add new match details
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        [HttpPost()]
        [ProducesResponseType(Status201Created)]
        [ProducesResponseType(Status409Conflict)]
        public async Task<IActionResult> PostAsync([FromBody] Match match)
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

                return CreatedAtAction(nameof(GetAsync), new { id }, match);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }
        }
    }
}
