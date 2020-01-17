using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Api.Models;
using Cricinfo.Parser;
using Cricinfo.UI.ValidationAttributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cricinfo.UI.Pages
{
    [BindProperties]
    public class InningsModel : PageModel
    {
        public string Team { get; set; }
        [Required]
        [BattingScorecardValidator]
        public string BattingScorecard { get; set; }
        [Required]
        [BowlingScorecardValidator]
        public string BowlingScorecard { get; set; }
        [Required]
        [FallOFWicketScorecardValidator]
        public string FallOfWicketScorecard { get; set; }
        [Range(0, int.MaxValue)]
        public int Extras { get; set; }

        public void OnGetFromScorecard(string header, string homeTeam, string awayTeam)
        {
            ViewData["header"] = header;
            ViewData["homeTeam"] = homeTeam;
            ViewData["awayTeam"] = awayTeam;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) { return new PageResult(); }

            var match = JsonSerializer.Deserialize<Match>((string)TempData["matchFromScorecard"]);
            var teamOrder = (int)TempData["teamOrder"];
            var innings = (int)TempData["innings"];
            var score = new Score
            {
                Team = Team,
                Innings = innings,
                Extras = Extras,
                BattingScorecard = Parse.parseBattingScorecard(BattingScorecard).ToArray(),
                BowlingScorecard = Parse.parseBowlingScorecard(BowlingScorecard).ToArray(),
                FallOfWicketScorecard = Parse.parseFallOfWicketScorecard(FallOfWicketScorecard)
            };
            match.Scores = match.Scores == null
                ? new Score[] { score }
                : match.Scores.Append(score).ToArray();

            string header;
            TempData["matchFromScorecard"] = JsonSerializer.Serialize(match);
            switch (teamOrder, innings)
            {
                case (1, 1):
                    TempData["teamOrder"] = 2;
                    TempData["innings"] = 1;
                    header = "Second Team, First Innings";
                    break;
                case (2, 1):
                    TempData["teamOrder"] = 1;
                    TempData["innings"] = 2;
                    header = "First Team, Second Innings";
                    break;
                case (1, 2):
                    TempData["teamOrder"] = 2;
                    TempData["innings"] = 2;
                    header = "Second Team, Second Innings";
                    break;
                case (2, 2):
                    return RedirectToPage("Verification");
                default:
                    throw new ArgumentException($"invalid values for 'teamOrder' ({teamOrder}) and/or 'innings' ({innings})");
            }

            return RedirectToPage("Innings", "FromScorecard",
                new { header, homeTeam = match.HomeTeam, awayTeam = match.AwayTeam });
        }
    }
}
