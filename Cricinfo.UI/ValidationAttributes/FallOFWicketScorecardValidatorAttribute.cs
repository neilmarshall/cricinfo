using System;
using System.ComponentModel.DataAnnotations;
using Cricinfo.Parser;

namespace Cricinfo.UI.ValidationAttributes
{
    public class FallOFWicketScorecardValidatorAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            try
            {
                Parse.parseFallOfWicketScorecard((string)value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
