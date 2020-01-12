using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cricinfo.UI.ValidationAttributes
{
    public class SquadValidatorAttribute : ValidationAttribute
    {
        public static int NumberOfPlayers { get => 2; }  // TODO: change to 10 on release

        public override bool IsValid(object value)
        {
            if (value == null) { return false; }
            var squad = ((string)value).Split('\n');
            return squad.Count() == NumberOfPlayers;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} must be a multiline string of {NumberOfPlayers} entries.";
        }
    }
}
