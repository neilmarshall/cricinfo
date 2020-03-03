using System.ComponentModel.DataAnnotations;

namespace Cricinfo.UI.ValidationAttributes
{
    public class SquadValidatorAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return DataValidator.SquadIsValid((string)value);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} must be a multiline string of {DataValidator.NumberOfPlayers} entries, each formatted as a sigle firstname followed by one or more last names.";
        }
    }
}
