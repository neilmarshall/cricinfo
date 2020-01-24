using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Api.Client;
using Cricinfo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cricinfo.UI.Pages.Scorecard
{
    public class VerificationModel : PageModel
    {
        private readonly ICricinfoApiClient _cricinfoApiClient;

        public VerificationModel(ICricinfoApiClient cricinfoApiClient)
        {
            this._cricinfoApiClient = cricinfoApiClient;
        }

        public void OnGet()
        {
            var match = JsonSerializer.Deserialize<Match>((string)TempData.Peek("matchFromScorecard"));
            ViewData["match"] = match;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var match = JsonSerializer.Deserialize<Match>((string)TempData["matchFromScorecard"]);
            await this._cricinfoApiClient.CreateMatchAsync(match);
            return RedirectToPage("/Index");
        }
    }
}
