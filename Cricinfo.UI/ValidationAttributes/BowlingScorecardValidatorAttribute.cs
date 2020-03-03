using System.ComponentModel.DataAnnotations;

namespace Cricinfo.UI.ValidationAttributes
{
    public class BowlingScorecardValidatorAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return DataValidator.BowlingScorecardIsValid((string)value);
        }
    }
}
