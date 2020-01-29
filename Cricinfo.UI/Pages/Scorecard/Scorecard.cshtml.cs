using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Cricinfo.Models;
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
        [Display(Name= "Date of First Day")]
        public DateTime DateOfFirstDay { get; set; }
        [Required]
        [Display(Name="Home Team")]
        public string HomeTeam { get; set; }
        [Required]
        [Display(Name = "Away Team")]
        public string AwayTeam { get; set; }
        public Result Result { get; set; }
        [Required]
        [SquadValidator]
        [Display(Name = "Home Squad")]
        public string HomeSquad { get; set; }
        [Required]
        [SquadValidator]
        [Display(Name = "Away Squad")]
        public string AwaySquad { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) { return new PageResult(); }

            TextInfo ti = new CultureInfo("en-UK", false).TextInfo;

            var match = new Match
            {
                Venue = Venue,
                DateOfFirstDay = DateOfFirstDay,
                HomeTeam = ti.ToTitleCase(HomeTeam),
                AwayTeam = ti.ToTitleCase(AwayTeam),
                Result = Result,
                HomeSquad = HomeSquad.Trim().Split('\n'),
                AwaySquad = AwaySquad.Trim().Split('\n'),
                Scores = null
            };

            TempData["matchFromScorecard"] = JsonSerializer.Serialize(match);
            TempData["teamOrder"] = 1;
            TempData["innings"] = 1;

            return RedirectToPage("Innings", "FromScorecard",
                new
                {
                    header = "First Team, First Innings",
                    homeTeam = match.HomeTeam,
                    awayTeam = match.AwayTeam,
                    selectedTeam = match.HomeTeam
                });
        }
    }
}
