using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cricinfo.Parser;

namespace Cricinfo.UI.ValidationAttributes
{
    public class BattingScorecardValidatorAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            try
            {
                Parse.parseBattingScorecard((string)value).ToArray();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
