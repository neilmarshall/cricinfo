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
    }
}
