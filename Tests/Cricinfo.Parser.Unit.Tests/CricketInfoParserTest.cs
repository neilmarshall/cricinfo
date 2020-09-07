using Cricinfo.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cricinfo.Parser.Unit.Tests
{
    [TestClass]
    public class BattingScorecardParserFixture
    {
        [TestMethod]
        public void BattingScorecardParsedCorrectly()
        {
            var battingFigures = @"Crawley	c de Kock	b Philander	4	15	15	0	0	26.67
                                   Sibley c de Kock   b Rabada    49  108 76  7   0   44.74
                                   Stokes c Elgar b Nortje    47  109 77  6   1   61.04
                                   Archer c van der Dussen b Nortje    4   7   5   1   0   80.00
                                   Denly b Maharaj   38  191 130 5   0   29.23
                                   Pope not out		61  199 144 7   0   42.36
                                   Denly lbw b Pretorius 32  128 79  3   2   39.24
                                   Sibley c & b Maharaj   29  118 90  5   0   32.22\n";
            var parsedBattingFigures = Parse.parseBattingScorecard(battingFigures);
            CollectionAssert.AreEqual(new string[] { "Crawley", "Sibley", "Stokes", "Archer", "Denly", "Pope", "Denly", "Sibley" },
                parsedBattingFigures.Select(bf => bf.Name).ToArray());
            CollectionAssert.AreEqual(new Dismissal[] { Dismissal.Caught, Dismissal.Caught, Dismissal.Caught, Dismissal.Caught, Dismissal.Bowled, Dismissal.NotOut, Dismissal.LBW, Dismissal.CaughtAndBowled },
                parsedBattingFigures.Select(bf => bf.Dismissal).ToArray());
            CollectionAssert.AreEqual(new string[] { "de Kock", "de Kock", "Elgar", "van der Dussen", null, null, null, "Maharaj" },
                parsedBattingFigures.Select(bf => bf.Catcher).ToArray());
            CollectionAssert.AreEqual(new string[] { "Philander", "Rabada", "Nortje", "Nortje", "Maharaj", null, "Pretorius", "Maharaj" },
                parsedBattingFigures.Select(bf => bf.Bowler).ToArray());
            CollectionAssert.AreEqual(new int[] { 4, 49, 47, 4, 38, 61, 32, 29 },
                parsedBattingFigures.Select(bf => bf.Runs).ToArray());
            CollectionAssert.AreEqual(new int[] { 15, 108, 109, 7, 191, 199, 128, 118 },
                parsedBattingFigures.Select(bf => bf.Mins).ToArray());
            CollectionAssert.AreEqual(new int[] { 15, 76, 77, 5, 130, 144, 79, 90 },
                parsedBattingFigures.Select(bf => bf.Balls).ToArray());
            CollectionAssert.AreEqual(new int[] { 0, 7, 6, 1, 5, 7, 3, 5 },
                parsedBattingFigures.Select(bf => bf.Fours).ToArray());
            CollectionAssert.AreEqual(new int[] { 0, 0, 1, 0, 0, 0, 2, 0 },
                parsedBattingFigures.Select(bf => bf.Sixes).ToArray());
        }

        [TestMethod]
        public void BattingScorecardWithMissingEconomyParsedCorrectly()
        {
            var battingFigures = @"Crawley	c de Kock	b Philander	4	15	15	0	0	26.67
                                   Sibley c & b Maharaj   29  118 90  5   0\n";
            var parsedBattingFigures = Parse.parseBattingScorecard(battingFigures);
            CollectionAssert.AreEqual(new string[] { "Crawley", "Sibley" },
                parsedBattingFigures.Select(bf => bf.Name).ToArray());
            CollectionAssert.AreEqual(new Dismissal[] { Dismissal.Caught, Dismissal.CaughtAndBowled },
                parsedBattingFigures.Select(bf => bf.Dismissal).ToArray());
            CollectionAssert.AreEqual(new string[] { "de Kock", "Maharaj" },
                parsedBattingFigures.Select(bf => bf.Catcher).ToArray());
            CollectionAssert.AreEqual(new string[] { "Philander", "Maharaj" },
                parsedBattingFigures.Select(bf => bf.Bowler).ToArray());
            CollectionAssert.AreEqual(new int[] { 4, 29 },
                parsedBattingFigures.Select(bf => bf.Runs).ToArray());
            CollectionAssert.AreEqual(new int[] { 15, 118 },
                parsedBattingFigures.Select(bf => bf.Mins).ToArray());
            CollectionAssert.AreEqual(new int[] { 15, 90 },
                parsedBattingFigures.Select(bf => bf.Balls).ToArray());
            CollectionAssert.AreEqual(new int[] { 0, 5 },
                parsedBattingFigures.Select(bf => bf.Fours).ToArray());
            CollectionAssert.AreEqual(new int[] { 0, 0 },
                parsedBattingFigures.Select(bf => bf.Sixes).ToArray());
        }

        [TestMethod]
        public void BattingScorecardWithRunOutDismissalParsedCorrectly()
        {
            var battingFigures = @"Maharaj	run out (S Curran)		71	148	106	10	3	66.98";
            var parsedBattingFigures = Parse.parseBattingScorecard(battingFigures);
            Assert.AreEqual("Maharaj", parsedBattingFigures.First().Name);
            Assert.AreEqual(Dismissal.RunOut, parsedBattingFigures.First().Dismissal);
            Assert.IsNull(parsedBattingFigures.First().Catcher);
            Assert.IsNull(parsedBattingFigures.First().Bowler);
            Assert.AreEqual(71, parsedBattingFigures.First().Runs);
            Assert.AreEqual(148, parsedBattingFigures.First().Mins);
            Assert.AreEqual(106, parsedBattingFigures.First().Balls);
            Assert.AreEqual(10, parsedBattingFigures.First().Fours);
            Assert.AreEqual(3, parsedBattingFigures.First().Sixes);
        }

        [TestMethod]
        public void BattingScorecardWithStumpedDismissalParsedCorrectly()
        {
            var battingFigures = @"du Plessis  st Bairstow b Parkinson	28	0	45	3	0	62.22";
            var parsedBattingFigures = Parse.parseBattingScorecard(battingFigures);
            Assert.AreEqual("du Plessis", parsedBattingFigures.First().Name);
            Assert.AreEqual(Dismissal.Stumped, parsedBattingFigures.First().Dismissal);
            Assert.IsNull(parsedBattingFigures.First().Catcher);
            Assert.AreEqual("Parkinson", parsedBattingFigures.First().Bowler);
            Assert.AreEqual(28, parsedBattingFigures.First().Runs);
            Assert.AreEqual(0, parsedBattingFigures.First().Mins);
            Assert.AreEqual(45, parsedBattingFigures.First().Balls);
            Assert.AreEqual(3, parsedBattingFigures.First().Fours);
            Assert.AreEqual(0, parsedBattingFigures.First().Sixes);
        }

        [TestMethod]
        public void BattingScorecardWithRetiredDismissalParsedCorrectly()
        {
            var battingFigures = "Bairstow Retired Not Out		100	0	83	11	4	120.48";
            var parsedBattingFigures = Parse.parseBattingScorecard(battingFigures);
            Assert.AreEqual("Bairstow", parsedBattingFigures.First().Name);
            Assert.AreEqual(Dismissal.Retired, parsedBattingFigures.First().Dismissal);
            Assert.IsNull(parsedBattingFigures.First().Catcher);
            Assert.IsNull(parsedBattingFigures.First().Bowler);
            Assert.AreEqual(100, parsedBattingFigures.First().Runs);
            Assert.AreEqual(0, parsedBattingFigures.First().Mins);
            Assert.AreEqual(83, parsedBattingFigures.First().Balls);
            Assert.AreEqual(11, parsedBattingFigures.First().Fours);
            Assert.AreEqual(4, parsedBattingFigures.First().Sixes);
        }

        [TestMethod]
        public void BattingScorecardWithHitWicketDismissalParsedCorrectly()
        {
            var battingFigures = "Bairstow  Hit Wicket  b Starc 9   16  11  2   0   81.82";
            var parsedBattingFigures = Parse.parseBattingScorecard(battingFigures);
            Assert.AreEqual("Bairstow", parsedBattingFigures.First().Name);
            Assert.AreEqual(Dismissal.HitWicket, parsedBattingFigures.First().Dismissal);
            Assert.IsNull(parsedBattingFigures.First().Catcher);
            Assert.AreEqual("Starc", parsedBattingFigures.First().Bowler);
            Assert.AreEqual(9, parsedBattingFigures.First().Runs);
            Assert.AreEqual(16, parsedBattingFigures.First().Mins);
            Assert.AreEqual(11, parsedBattingFigures.First().Balls);
            Assert.AreEqual(2, parsedBattingFigures.First().Fours);
            Assert.AreEqual(0, parsedBattingFigures.First().Sixes);
        }

        [TestMethod]
        [ExpectedException(typeof(Exceptions.BattingFiguresException))]
        public void BattingScorecardErrorsOnInvalidInput()
        {
            var battingFigures = @"Crawley	c de Kock	4	15	15	0	0	26.67";
            Parse.parseBattingScorecard(battingFigures).ToArray();
        }

        [TestMethod]
        public void BattingScorecardParsesNamesWithApostrophes()
        {
            var battingFigures = @"K O'Brien	c Willey	b Rashid	22	68	36	0	0	61.11";
            var parsedBattingFigures = Parse.parseBattingScorecard(battingFigures);
            Assert.AreEqual("K O'Brien", parsedBattingFigures.First().Name);
        }
    }

    [TestClass]
    public class BowlingScorecardParserFixture
    {
        [TestMethod]
        public void BowlingScorecardParsedCorrectly()
        {
            var bowlingFigures = @"Anderson	18.0	9	23	2	1.28
                                   Broad    23.0    8   37  1   1.61
                                   Bess 33.0    14  57  1   1.73
                                   S Curran 16.0    4   37  1   2.31
                                   Denly    18.0    4   42  2   2.33
                                   Root 6.0 0   11  0   1.83
                                   Stokes   23.4    8   35  3   1.48\n";
            var parsedBowlingFigures = Parse.parseBowlingScorecard(bowlingFigures);
            CollectionAssert.AreEqual(new string[] { "Anderson", "Broad", "Bess", "S Curran", "Denly", "Root", "Stokes" },
                parsedBowlingFigures.Select(bf => bf.Name).ToArray());
            CollectionAssert.AreEqual(new float[] { 18.0f, 23.0f, 33.0f, 16.0f, 18.0f, 6.0f, 23.4f },
                parsedBowlingFigures.Select(bf => bf.Overs).ToArray());
            CollectionAssert.AreEqual(new int[] { 9, 8, 14, 4, 4, 0, 8 },
                parsedBowlingFigures.Select(bf => bf.Maidens).ToArray());
            CollectionAssert.AreEqual(new int[] { 23, 37, 57, 37, 42, 11, 35 },
                parsedBowlingFigures.Select(bf => bf.Runs).ToArray());
            CollectionAssert.AreEqual(new int[] { 2, 1, 1, 1, 2, 0, 3 },
                parsedBowlingFigures.Select(bf => bf.Wickets).ToArray());
        }

        [TestMethod]
        public void BowlingScorecardWithMissingEconomyParsedCorrectly()
        {
            var bowlingFigures = @"Anderson	18.0	9	23	2	1.28
                                   Stokes   23.4    8   35  3\n";
            var parsedBowlingFigures = Parse.parseBowlingScorecard(bowlingFigures);
            CollectionAssert.AreEqual(new string[] { "Anderson", "Stokes" },
                parsedBowlingFigures.Select(bf => bf.Name).ToArray());
            CollectionAssert.AreEqual(new float[] { 18.0f, 23.4f },
                parsedBowlingFigures.Select(bf => bf.Overs).ToArray());
            CollectionAssert.AreEqual(new int[] { 9, 8 },
                parsedBowlingFigures.Select(bf => bf.Maidens).ToArray());
            CollectionAssert.AreEqual(new int[] { 23, 35 },
                parsedBowlingFigures.Select(bf => bf.Runs).ToArray());
            CollectionAssert.AreEqual(new int[] { 2, 3 },
                parsedBowlingFigures.Select(bf => bf.Wickets).ToArray());
        }

        [TestMethod]
        [ExpectedException(typeof(Exceptions.BowlingFiguresException))]
        public void BowlingScorecardErrorsOnInvalidInput()
        {
            var bowlingFigures = @"Anderson	18.0	7";
            Parse.parseBowlingScorecard(bowlingFigures).ToArray();
        }

        [TestMethod]
        public void BowlingScorecardParsesNamesWithApostrophes()
        {
            var bowlingFigures = @"K O'Brien	18.0	9	23	2	1.28";
            var parsedBowlingFigures = Parse.parseBowlingScorecard(bowlingFigures);
            Assert.AreEqual("K O'Brien", parsedBowlingFigures.First().Name);
        }
    }

    [TestClass]
    public class FallOfWicketScorecardParserFixture
    {
        [TestMethod]
        public void FallOfWicketScorecardParsedCorrectly()
        {
            var fallOfWicketData = @"71-1 (28.6 ovs)	Elgar
                                     123-2(54.2 ovs) Hamza
                                     129-3(58.5 ovs) Mahraj
                                     164-4(76.2 ovs) du Plessis
                                     171-5(86.5 ovs) Malan
                                     237-6(120.4 ovs)    de Kock
                                     237-7(125.5 ovs)    van der Dussen
                                     241-8(133.3 ovs)    Pretorius
                                     241-9(133.4 ovs)    Nortje
                                     248-10(137.4 ovs)   Philander\n";
            var parsedFallOfWicketData = Parse.parseFallOfWicketScorecard(fallOfWicketData);
            CollectionAssert.AreEqual(new int[] { 71, 123, 129, 164, 171, 237, 237, 241, 241, 248 }, parsedFallOfWicketData);
        }

        [TestMethod]
        [ExpectedException(typeof(Exceptions.FallOfWicketException))]
        public void FallOfWicketScorecardErrorsOnInvalidInput()
        {
            var fallOfWicketData = @"7abc1-1 (28.6 ovs)	Elgar";
            Parse.parseFallOfWicketScorecard(fallOfWicketData).ToArray();
        }
    }

    [TestClass]
    public class NameParserFixture
    {
        [DataTestMethod]
        [DataRow("Faf du Plessis(c)", "Faf", "du Plessis", "du Plessis")]
        [DataRow("Temba Bavuma", "Temba", "Bavuma", "Bavuma")]
        [DataRow("Quinton de Kock(wk)", "Quinton", "de Kock", "de Kock")]
        [DataRow("Quinton de Kock(c)(wk)", "Quinton", "de Kock", "de Kock")]
        [DataRow("Kevin O'Brien", "Kevin", "O'Brien", "O'Brien")]
        public void ParseNameParsesNamesCorrectly(string rawString, string expectedFirstName,
            string expectedLastName, string expectedLookupCode)
        {
            var (actualFirstName, actualLastName, actualLookupCode) = Parse.parseNames(new[] { rawString }).First();
            Assert.AreEqual(expectedFirstName, actualFirstName);
            Assert.AreEqual(expectedLastName, actualLastName);
            Assert.AreEqual(expectedLookupCode, actualLookupCode);
        }

        private static IEnumerable<object[]> DuplicateSurnamesTestData()
        {
            yield return new object[]
            {
                new string[] { "Aa Name1", "Aa Name2", "Ba Name1" },
                new Tuple<string, string, string>[]
                {
                    Tuple.Create("Aa", "Name1", "Aa Name1"),
                    Tuple.Create("Aa", "Name2", "Name2"),
                    Tuple.Create("Ba", "Name1", "Ba Name1")
                }
            };
            yield return new object[]
            {
                new string[] { "Abid Ali", "Azhar Ali(c)" },
                new Tuple<string, string, string>[]
                {
                    Tuple.Create("Abid", "Ali", "Abid Ali"),
                    Tuple.Create("Azhar", "Ali", "Azhar Ali"),
                }
            };
        }

        [DataTestMethod]
        [DynamicData(nameof(DuplicateSurnamesTestData), DynamicDataSourceType.Method)]
        public void ParseNamesHandlesDuplicateSurnamesCorrectly(IEnumerable<string> namesToParse, Tuple<string, string, string>[] expected)
        {
            var parsedNames = Parse.parseNames(namesToParse).ToArray();
            CollectionAssert.AreEqual(expected, parsedNames);
        }

        [TestMethod]
        [ExpectedException(typeof(Exceptions.PlayerNameException))]
        public void ParseNamesErrorsOnDuplicateNames()
        {
            var parsedNames = Parse.parseNames(
                new[] { "Aa Name1", "Aa Name1", "Ba Name1" });
        }
    }
}
