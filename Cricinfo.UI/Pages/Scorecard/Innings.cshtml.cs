using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cricinfo.Models;
using Cricinfo.Parser;
using Cricinfo.UI.ValidationAttributes;

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
        [Required]
        public bool Declared { get; set; }

        public void OnGetFromScorecard(string header, string homeTeam, string awayTeam, string selectedTeam)
        {
            TempData["header"] = header;
            TempData["homeTeam"] = homeTeam;
            TempData["awayTeam"] = awayTeam;
            this.Team = selectedTeam;
        }

        private string matchName(ICollection<string> parsedNames, string name)
        {
            if (name == null)
                throw new ArgumentException("Parameter 'name' cannot be null");

            var firstName = name.Split().First();
            var lastName = name.Split().Last();

            if (parsedNames.Contains(lastName))
            {
                return lastName;
            }
            else if (parsedNames.FirstOrDefault(name => name.StartsWith(firstName.First()) && name.EndsWith(lastName)) != null)
            {
                return parsedNames.First(name => name.StartsWith(firstName.First()) && name.EndsWith(lastName));
            }

            return null;
        }

        private IList<string> getMissingBatsmen(ICollection<string> parsedNames, BattingScorecard[] battingScorecard)
        {
            var missingBatsmen = new List<string>();

            foreach (var bs in battingScorecard)
            {
                if (!parsedNames.Contains(bs.Name))
                {
                    var name = matchName(parsedNames, bs.Name);

                    if (name != null)
                    {
                        bs.Name = name;
                    }
                    else
                    {
                        missingBatsmen.Add(bs.Name);
                    }
                }
                if (bs.Bowler != null && !parsedNames.Contains(bs.Bowler))
                {
                    var name = matchName(parsedNames, bs.Bowler);

                    if (name != null)
                    {
                        bs.Bowler = name;
                    }
                    else
                    {
                        missingBatsmen.Add(bs.Bowler);
                    }
                }
                if (bs.Catcher != null && !parsedNames.Contains(bs.Catcher) && bs.Catcher != "sub")
                {
                    var name = matchName(parsedNames, bs.Catcher);

                    if (name != null)
                    {
                        bs.Catcher = name;
                    }
                    else
                    {
                        missingBatsmen.Add(bs.Catcher);
                    }
                }
            }

            return missingBatsmen;
        }

        private IList<string> getMissingBowlers(ICollection<string> parsedNames, BowlingScorecard[] bowlingScorecard)
        {
            var missingBowlers = new List<string>();

            foreach(var bs in bowlingScorecard)
            {
                if (!parsedNames.Contains(bs.Name))
                {
                    var name = matchName(parsedNames, bs.Name);

                    if (name != null)
                    {
                        bs.Name = name;
                    }
                    else
                    {
                        missingBowlers.Add(bs.Name);
                    }
                }
            }

            return missingBowlers;
        }

        public IActionResult OnPostAddAnotherInnings()
        {
            if (!ModelState.IsValid) { return new PageResult(); }

            var battingScorecard = Parse.parseBattingScorecard(BattingScorecard).ToArray();
            var bowlingScorecard = Parse.parseBowlingScorecard(BowlingScorecard).ToArray();
            var fallOfWicketScorecard = Parse.parseFallOfWicketScorecard(FallOfWicketScorecard);

            var match = JsonSerializer.Deserialize<Match>((string)TempData["matchFromScorecard"]);

            var parsedNames = Parse.parseNames(match.HomeSquad.Union(match.AwaySquad)).Select(name => name.Item3).ToHashSet();

            var missingBatsmen = getMissingBatsmen(parsedNames, battingScorecard);
            if (missingBatsmen.Count > 0)
            {
                ModelState.AddModelError("BattingScorecard",
                    $"Could not find the following player(s): {String.Join(", ", missingBatsmen.Distinct())}");
            }

            var missingBowlers = getMissingBowlers(parsedNames, bowlingScorecard);
            if (missingBowlers.Count > 0)
            {
                ModelState.AddModelError("BowlingScorecard",
                    $"Could not find the following player(s): {String.Join(", ", missingBowlers.Distinct())}");
            }

            if (ModelState.ErrorCount > 0)
            {
                TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                return new PageResult();
            }

            var teamOrder = (int)TempData["teamOrder"];
            var innings = (int)TempData["innings"];
            var score = new Score
            {
                Team = Team,
                Innings = innings,
                Extras = Extras,
                Declared = Declared,
                BattingScorecard = battingScorecard,
                BowlingScorecard = bowlingScorecard,
                FallOfWicketScorecard = fallOfWicketScorecard
            };
            match.Scores = match.Scores == null
                ? new Score[] { score }
                : match.Scores.Append(score).ToArray();

            string header;
            TempData["matchFromScorecard"] = JsonSerializer.Serialize(match);
            TempData["matchType"] = match.MatchType;
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

            var missingBatsmen = getMissingBatsmen(parsedNames, battingScorecard);
            if (missingBatsmen.Count() > 0)
            {
                ModelState.AddModelError("BattingScorecard",
                    $"Could not find the following player(s): {String.Join(", ", missingBatsmen.Distinct())}");
            }

            var missingBowlers = getMissingBowlers(parsedNames, bowlingScorecard);
            if (missingBowlers.Count > 0)
            {
                ModelState.AddModelError("BowlingScorecard",
                    $"Could not find the following player(s): {String.Join(", ", missingBowlers.Distinct())}");
            }

            if (ModelState.ErrorCount > 0)
            {
                TempData["matchFromScorecard"] = TempData["matchFromScorecard"];
                return new PageResult();
            }

            var innings = (int)TempData["innings"];
            var score = new Score
            {
                Team = Team,
                Innings = innings,
                Extras = Extras,
                Declared = Declared,
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

        public IActionResult OnPostReturnToPreviousPage()
        {
            var match = JsonSerializer.Deserialize<Match>((string)TempData["matchFromScorecard"]);
            var teamOrder = (int)TempData.Peek("teamOrder");
            var innings = (int)TempData.Peek("innings");

            string header;
            switch (teamOrder, innings)
            {
                case (1, 1):
                    return RedirectToPage("Scorecard", "FromInnings", match);
                case (2, 1):
                    TempData["teamOrder"] = 1;
                    TempData["innings"] = 1;
                    header = "First Team, First Innings";
                    break;
                case (1, 2):
                    TempData["teamOrder"] = 2;
                    TempData["innings"] = 1;
                    header = "Second Team, First Innings";
                    break;
                case (2, 2):
                    TempData["teamOrder"] = 1;
                    TempData["innings"] = 2;
                    header = "First Team, Second Innings";
                    break;
                default:
                    throw new ArgumentException($"invalid values for 'teamOrder' ({teamOrder}) and/or 'innings' ({innings})");
            }

            match.Scores = match.Scores.Take(match.Scores.Length - 1).ToArray();
            TempData["matchFromScorecard"] = JsonSerializer.Serialize(match);

            return RedirectToPage("Innings", "FromScorecard",
                new
                {
                    header,
                    homeTeam = match.HomeTeam,
                    awayTeam = match.AwayTeam,
                    selectedTeam = (this.Team == match.HomeTeam ? match.AwayTeam : match.HomeTeam)
                });
        }
    }
}
