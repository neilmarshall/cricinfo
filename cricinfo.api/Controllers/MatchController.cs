using System;
using System.Threading.Tasks;
using cricinfo.api.Models;
using cricinfo.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace cricinfo.api.Controllers
{
    [ApiController]
    [Route("api/")]
    public class MatchController : ControllerBase
    {
        private ICricInfoRepository _cricInfoRepository;

        public MatchController(ICricInfoRepository cricInfoRepository)
        {
            this._cricInfoRepository = cricInfoRepository;
        }

        [HttpGet()]
        public async Task<IActionResult> GetMatchAsync(int id)
        {
            var match = await this._cricInfoRepository.GetMatchAsync(id);

            if (match == null) { return NotFound(); }

            return Ok(match);
        }

        [HttpPost()]
        public async Task<IActionResult> CreateMatchAsync(Match match)
        {
            if (match == null) { return BadRequest(); }

            var (dataCreationResponse, id) = await this._cricInfoRepository.CreateMatchAsync(match);

            if (dataCreationResponse == DataCreationResponse.DuplicateContent) { return StatusCode(409); }
            if (dataCreationResponse == DataCreationResponse.Failure) { return StatusCode(500); }

            return CreatedAtAction(nameof(GetMatchAsync), new { id = id }, match);
        }
    }
}
