using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cricinfo.Parser;
using static Cricinfo.Parser.Exceptions;

namespace Cricinfo.UI.ValidationAttributes
{
    public class SquadValidatorAttribute : ValidationAttribute
    {
        public static int NumberOfPlayers { get => 11; }

        public override bool IsValid(object value)
        {
            if (value == null) { return false; }

            var squad = ((string)value).Trim().Split('\n');

            if (squad.Count() != NumberOfPlayers) { return false; }

            try
            {
                Parse.parseNames(squad).ToArray();
                return true;
            }
            catch (PlayerNameException)
            {
                return false;
            }
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} must be a multiline string of {NumberOfPlayers} entries, each formatted as a sigle firstname followed by one or more last names.";
        }
    }
}
