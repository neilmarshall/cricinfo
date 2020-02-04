using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Models;
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
        [Display(Name= "Batting Scorecard")]
        public string BattingScorecard { get; set; }
        [Required]
        [BowlingScorecardValidator]
        [Display(Name = "Bowling Scorecard")]
        public string BowlingScorecard { get; set; }
        [Required]
        [FallOFWicketScorecardValidator]
        [Display(Name = "Fall of Wicket Scorecard")]
        public string FallOfWicketScorecard { get; set; }
        [Range(0, int.MaxValue)]
        public int Extras { get; set; }

        public void OnGetFromScorecard(string header, string homeTeam, string awayTeam, string selectedTeam)
        {
            TempData["header"] = header;
            TempData["homeTeam"] = homeTeam;
            TempData["awayTeam"] = awayTeam;
            this.Team = selectedTeam;
        }

        public IActionResult OnPostAddAnotherInnings()
        {
            if (!ModelState.IsValid) { return new PageResult(); }

            var battingScorecard = Parse.parseBattingScorecard(BattingScorecard).ToArray();
            var bowlingScorecard = Parse.parseBowlingScorecard(BowlingScorecard).ToArray();
            var fallOfWicketScorecard = Parse.parseFallOfWicketScorecard(FallOfWicketScorecard);

            var match = JsonSerializer.Deserialize<Match>((string)TempData["matchFromScorecard"]);

            var parsedNames = Parse.parseNames(match.HomeSquad.Union(match.AwaySquad)).Select(name => name.Item3).ToHashSet();

            foreach(var bs in battingScorecard)
            {
                if (!parsedNames.Contains(bs.Name))
                {
                    ModelState.AddModelError("BattingScorecard", $"Could not find player '{bs.Name}' in either Home or Away squads.");
                    TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                    return new PageResult();
                }
                if (bs.Bowler != null && !parsedNames.Contains(bs.Bowler))
                {
                    ModelState.AddModelError("BattingScorecard", $"Could not find player '{bs.Bowler}' in either Home or Away squads.");
                    TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                    return new PageResult();
                }
                if (bs.Catcher != null && !parsedNames.Contains(bs.Catcher) && bs.Catcher != "sub")
                {
                    ModelState.AddModelError("BattingScorecard", $"Could not find player '{bs.Catcher}' in either Home or Away squads.");
                    TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                    return new PageResult();
                }
            }

            foreach(var bs in bowlingScorecard)
            {
                if (!parsedNames.Contains(bs.Name))
                {
                    ModelState.AddModelError("BowlingScorecard", $"Could not find player '{bs.Name}' in either Home or Away squads.");
                    TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                    return new PageResult();
                }
            }

            var teamOrder = (int)TempData["teamOrder"];
            var innings = (int)TempData["innings"];
            var score = new Score
            {
                Team = Team,
                Innings = innings,
                Extras = Extras,
                BattingScorecard = battingScorecard,
                BowlingScorecard = bowlingScorecard,
                FallOfWicketScorecard = fallOfWicketScorecard
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
                new
                {
                    header,
                    homeTeam = match.HomeTeam,
                    awayTeam = match.AwayTeam,
                    selectedTeam = (this.Team == match.HomeTeam ? match.AwayTeam : match.HomeTeam)
                });
        }

        public IActionResult OnPostSubmitAllInnings()
        {
            if (!ModelState.IsValid) { return new PageResult(); }

            var battingScorecard = Parse.parseBattingScorecard(BattingScorecard).ToArray();
            var bowlingScorecard = Parse.parseBowlingScorecard(BowlingScorecard).ToArray();
            var fallOfWicketScorecard = Parse.parseFallOfWicketScorecard(FallOfWicketScorecard);

            var match = JsonSerializer.Deserialize<Match>((string)TempData["matchFromScorecard"]);

            var parsedNames = Parse.parseNames(match.HomeSquad.Union(match.AwaySquad)).Select(name => name.Item3).ToHashSet();

            foreach (var bs in battingScorecard)
            {
                if (!parsedNames.Contains(bs.Name))
                {
                    ModelState.AddModelError("BattingScorecard", $"Could not find player '{bs.Name}' in either Home or Away squads.");
                    TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                    return new PageResult();
                }
                if (bs.Bowler != null && !parsedNames.Contains(bs.Bowler))
                {
                    ModelState.AddModelError("BattingScorecard", $"Could not find player '{bs.Bowler}' in either Home or Away squads.");
                    TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                    return new PageResult();
                }
                if (bs.Catcher != null && !parsedNames.Contains(bs.Catcher) && bs.Catcher != "sub")
                {
                    ModelState.AddModelError("BattingScorecard", $"Could not find player '{bs.Catcher}' in either Home or Away squads.");
                    TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                    return new PageResult();
                }
            }

            foreach (var bs in bowlingScorecard)
            {
                if (!parsedNames.Contains(bs.Name))
                {
                    ModelState.AddModelError("BowlingScorecard", $"Could not find player '{bs.Name}' in either Home or Away squads.");
                    TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                    return new PageResult();
                }
            }

            var innings = (int)TempData["innings"];
            var score = new Score
            {
                Team = Team,
                Innings = innings,
                Extras = Extras,
                BattingScorecard = battingScorecard,
                BowlingScorecard = bowlingScorecard,
                FallOfWicketScorecard = fallOfWicketScorecard
            };
            match.Scores = match.Scores == null
                ? new Score[] { score }
                : match.Scores.Append(score).ToArray();

            TempData["matchFromScorecard"] = JsonSerializer.Serialize(match);
            return RedirectToPage("Verification");
        }
    }
}
