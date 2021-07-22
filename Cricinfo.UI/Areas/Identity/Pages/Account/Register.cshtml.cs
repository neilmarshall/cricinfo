using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Cricinfo.Services.IdentityStore.Models;

namespace Cricinfo.UI.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            return Task.CompletedTask;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = Input.Username };
                var userCreationResult = await _userManager.CreateAsync(user, Input.Password);
                user.Id = (await _userManager.FindByNameAsync(user.UserName)).Id;

                IdentityResult claimsCreationResult = null;
                if (userCreationResult.Succeeded)
                {
                    claimsCreationResult = await _userManager.AddClaimsAsync(user, new[]
                    {
                        new Claim("CanAddTeam", "true"),
                        new Claim("CanAddScorecard", "true"),
                        new Claim("CanAddUser", "false"),
                        new Claim("CanManagePermissions", "false")
                    });
                }

                if (userCreationResult.Succeeded && (claimsCreationResult?.Succeeded ?? false))
                {
                    _logger.LogInformation("User created a new account with password.");

                    return LocalRedirect(returnUrl);
                }

                foreach (var error in userCreationResult.Errors.Union(claimsCreationResult.Errors))
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
