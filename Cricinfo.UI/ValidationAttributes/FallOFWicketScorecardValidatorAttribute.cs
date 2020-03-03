using System.ComponentModel.DataAnnotations;

namespace Cricinfo.UI.ValidationAttributes
{
    public class FallOFWicketScorecardValidatorAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return DataValidator.FallOfWicketScorecardIsValid((string)value);
        }
    }
}
