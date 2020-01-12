using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Api.Models;
using Cricinfo.UI.ValidationAttributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cricinfo.UI.Pages
{
    [BindProperties]
    public class ScorecardModel : PageModel
    {
        [Required]
        public string Venue { get; set; }
        public DateTime DateOfFirstDay { get; set; }
        [Required]
        public string HomeTeam { get; set; }
        [Required]
        public string AwayTeam { get; set; }
        public Result Result { get; set; }
        [Required]
        [SquadValidator]
        public string HomeSquad { get; set; }
        [Required]
        [SquadValidator]
        public string AwaySquad { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) { return new PageResult(); }

            var match = new Match
            {
                Venue = Venue,
                DateOfFirstDay = DateOfFirstDay,
                HomeTeam = HomeTeam,
                AwayTeam = AwayTeam,
                Result = Result,
                HomeSquad = HomeSquad.Split('\n'),
                AwaySquad = AwaySquad.Split('\n'),
                Scores = null
            };

            TempData["matchFromScorecard"] = JsonSerializer.Serialize(match);
            TempData["teamOrder"] = 1;
            TempData["innings"] = 1;

            return RedirectToPage("Innings", "FromScorecard",
                new { header = "First Team, First Innings", homeTeam = match.HomeTeam, awayTeam = match.AwayTeam });
        }
    }
}
