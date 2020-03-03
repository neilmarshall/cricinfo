using System.ComponentModel.DataAnnotations;

namespace Cricinfo.UI.ValidationAttributes
{
    public class BattingScorecardValidatorAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return DataValidator.BattingScorecardIsValid((string)value);
        }
    }
}
