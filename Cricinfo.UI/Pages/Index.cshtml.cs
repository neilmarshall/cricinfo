using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Cricinfo.Api.Client;

namespace Cricinfo.UI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ICricinfoApiClient _cricinfoApiClient;

        public IndexModel(ILogger<IndexModel> logger, ICricinfoApiClient cricinfoApiClient)
        {
            _logger = logger;
            _cricinfoApiClient = cricinfoApiClient;
        }

        public void OnGet()
        {
        }
    }
}
