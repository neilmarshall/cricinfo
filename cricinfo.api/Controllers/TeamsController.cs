using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Cricinfo.Services.Matchdata;

namespace Cricinfo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly ICricInfoCommandService cricInfoCommandService;
        private readonly ICricInfoQueryService cricInfoQueryService;
        private readonly ILogger<MatchController> _logger;

        public TeamsController(ICricInfoCommandService cricInfoCommandService,
            ICricInfoQueryService cricInfoQueryService,
            ILogger<MatchController> logger)
        {
            this.cricInfoCommandService = cricInfoCommandService;
            this.cricInfoQueryService = cricInfoQueryService;
            this._logger = logger;
        }

        /// <summary>
        /// Get a list of all teams
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAsync()
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

        /// <summary>
        /// Add a new team
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        [HttpPost()]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> PostAsync([FromQuery] string team)
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
    }
}
