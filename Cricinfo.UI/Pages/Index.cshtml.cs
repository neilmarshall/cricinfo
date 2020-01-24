using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cricinfo.Api.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

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
