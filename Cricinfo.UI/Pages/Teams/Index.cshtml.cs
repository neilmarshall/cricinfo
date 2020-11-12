using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cricinfo.Api.Client;

namespace Cricinfo.UI.Pages
{
    public class TeamsModel : PageModel
    {
        private readonly ICricinfoApiClient cricinfoApiClient;

        [BindProperty]
        [Required]
        public string Team { get; set; }

        public TeamsModel(ICricinfoApiClient cricinfoApiClient)
        {
            this.cricinfoApiClient = cricinfoApiClient;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                await this.cricinfoApiClient.CreateTeamAsync(Team);
            }

            return new RedirectResult("Teams");
        }
    }
}
