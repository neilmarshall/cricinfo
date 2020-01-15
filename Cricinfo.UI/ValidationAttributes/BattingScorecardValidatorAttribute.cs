using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cricinfo.Parser;
using static Cricinfo.Parser.Exceptions;

namespace Cricinfo.UI.ValidationAttributes
{
    public class BattingScorecardValidatorAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) { return false; }

            try
            {
                Parse.parseBattingScorecard((string)value).ToArray();
                return true;
            }
            catch (BattingFiguresException)
            {
                return false;
            }
        }
    }
}
