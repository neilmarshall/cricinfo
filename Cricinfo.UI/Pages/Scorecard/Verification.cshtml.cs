using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Api.Client;
using Cricinfo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Cricinfo.UI.Pages.Scorecard
{
    public class VerificationModel : PageModel
    {
        private readonly ICricinfoApiClient _cricinfoApiClient;
        private readonly ILogger<VerificationModel> _logger;

        public VerificationModel(ICricinfoApiClient cricinfoApiClient,
            ILogger<VerificationModel> logger)
        {
            this._cricinfoApiClient = cricinfoApiClient;
            this._logger = logger;
        }

        public void OnGet()
        {
            var match = JsonSerializer.Deserialize<Match>((string)TempData.Peek("matchFromScorecard"));
            ViewData["match"] = match;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var match = JsonSerializer.Deserialize<Match>((string)TempData.Peek("matchFromScorecard"));
            try
            {
                await this._cricinfoApiClient.CreateMatchAsync(match);
                return RedirectToPage("/Index");
            }
            catch (System.Exception e)
            {
                this._logger.LogError(e.Message);
                ViewData["match"] = match;
                ViewData["errorOccurred"] = true;
                return Page();
            }
        }

        public IActionResult OnPostReturnToPreviousPageAsync()
        {
            var teamOrder = (int)TempData.Peek("teamOrder");
            var innings = (int)TempData.Peek("innings");

            var match = JsonSerializer.Deserialize<Match>((string)TempData.Peek("matchFromScorecard"));
            match.Scores = match.Scores.Take(match.Scores.Length - 1).ToArray();
            TempData["matchFromScorecard"] = JsonSerializer.Serialize(match);

            return RedirectToPage("Innings", "FromScorecard",
                new
                {
                    header= $"{(teamOrder == 1 ? "First" : "Second")} Team, {(innings == 1 ? "First" : "Second")} Innings",
                    homeTeam = match.HomeTeam,
                    awayTeam = match.AwayTeam,
                    selectedTeam = (match.Scores.Last().Team == match.HomeTeam ? match.AwayTeam : match.HomeTeam)
                });
        }
    }
}
