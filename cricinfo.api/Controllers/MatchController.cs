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
        private ICricInfoRepository _cricInfoRepository;
        private readonly ILogger<MatchController> _logger;

        public MatchController(ICricInfoRepository cricInfoRepository,
            ILogger<MatchController> logger)
        {
            this._cricInfoRepository = cricInfoRepository;
            this._logger = logger;
        }

        [HttpGet(Name="GetMatchAsync")]
        [Route("{id}")]
        public async Task<IActionResult> GetMatchAsync(int? id)
        {
            try
            {
                if (id == null) { return BadRequest(); }

                this._logger.LogInformation($"GET request - Match ID '{id.Value}'");

                var match = await this._cricInfoRepository.GetMatchAsync(id.Value);

                if (match == null) { return NotFound(); }

                return Ok(match);
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

                var (dataCreationResponse, id) = await this._cricInfoRepository.CreateMatchAsync(match);

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

        [HttpGet()]
        [Route("CheckMatchExists")]
        public async Task<IActionResult> CheckMatchExistsAsync(
            [FromQuery]string homeTeam, [FromQuery]string awayTeam, [FromQuery]DateTime? date)
        {
            try
            {
                if (homeTeam == null || awayTeam == null || date == null) { return BadRequest(); }

                this._logger.LogInformation($"GET request - Home Team: '{homeTeam}, Away Team: {awayTeam}, Date: {date}'");

                var result = await this._cricInfoRepository.MatchExistsAsync(homeTeam, awayTeam, date.Value);

                return Ok(result);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);
                return StatusCode(500);
            }

        }
    }
}
