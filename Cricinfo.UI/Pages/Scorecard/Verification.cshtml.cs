using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cricinfo.UI.Pages.Scorecard
{
    public class VerificationModel : PageModel
    {
        public void OnGet()
        {
            var match = JsonSerializer.Deserialize<Match>((string)TempData["matchFromScorecard"]);
            ViewData["match"] = match;
        }
    }
}
