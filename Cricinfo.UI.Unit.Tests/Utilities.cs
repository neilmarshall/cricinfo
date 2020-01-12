using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cricinfo.UI.Unit.Tests
{
    public static class Utilities
    {
        public static async Task<string> GetCSRFTokenAsync(HttpContent content)
        {
            var html = await content.ReadAsStringAsync();
            var regex = new Regex("__RequestVerificationToken\\\" type=\\\"hidden\" value=\\\"(?<token>[^\"]*)\"");
            var match = regex.Match(html);

            if (match.Success)
            {
                return match.Groups["token"].Value;
            }
            else
            {
                throw new ArgumentException("could not find CSRF token");
            }
        }
    }
}
