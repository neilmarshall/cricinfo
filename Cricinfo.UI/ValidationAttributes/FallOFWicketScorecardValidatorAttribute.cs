using System;
using System.ComponentModel.DataAnnotations;
using Cricinfo.Parser;
using static Cricinfo.Parser.Exceptions;

namespace Cricinfo.UI.ValidationAttributes
{
    public class FallOFWicketScorecardValidatorAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) { return true; }

            try
            {
                Parse.parseFallOfWicketScorecard((string)value);
                return true;
            }
            catch (FallOfWicketException)
            {
                return false;
            }
        }
    }
}
