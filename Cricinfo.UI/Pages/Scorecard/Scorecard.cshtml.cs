using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Cricinfo.Api.Client;
using Cricinfo.Models;
using Cricinfo.Models.Enums;
using Cricinfo.UI.ValidationAttributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cricinfo.UI.Pages
{
    [BindProperties]
    public class ScorecardModel : PageModel
    {
        private readonly ICricinfoApiClient _cricinfoApiClient;

        [Required]
        public string Venue { get; set; }
        [Display(Name="Match Type")]
        public MatchType MatchType { get; set; }
        [Display(Name="Date of First Day")]
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

        public ScorecardModel(ICricinfoApiClient cricinfoApiClient)
        {
            this._cricinfoApiClient = cricinfoApiClient;
        }

        public void OnGet()
        {
        }

        public void OnGetFromInnings(Match match)
        {
            this.Venue = match.Venue;
            this.MatchType = match.MatchType;
            this.DateOfFirstDay = match.DateOfFirstDay;
            this.HomeTeam = match.HomeTeam;
            this.AwayTeam = match.AwayTeam;
            this.Result = match.Result;
            this.HomeSquad = String.Join(Environment.NewLine, match.HomeSquad);
            this.AwaySquad = String.Join(Environment.NewLine, match.AwaySquad);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) { return new PageResult(); }

            if (this._cricinfoApiClient.MatchExistsAsync(HomeTeam, AwayTeam, DateOfFirstDay).Result)
            {
                ModelState.AddModelError(string.Empty, "A record already exists for the specified teams and date.");
                return Page();
            }

            TextInfo ti = new CultureInfo("en-UK", false).TextInfo;

            var match = new Match
            {
                Venue = Venue,
                MatchType = MatchType,
                DateOfFirstDay = DateOfFirstDay,
                HomeTeam = ti.ToTitleCase(HomeTeam),
                AwayTeam = ti.ToTitleCase(AwayTeam),
                Result = Result,
                HomeSquad = HomeSquad.Trim().Split(Environment.NewLine),
                AwaySquad = AwaySquad.Trim().Split(Environment.NewLine),
                Scores = null
            };

            TempData["matchFromScorecard"] = JsonSerializer.Serialize(match);
            TempData["matchType"] = match.MatchType;
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
