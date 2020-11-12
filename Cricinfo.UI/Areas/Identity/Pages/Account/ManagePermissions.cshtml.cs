using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering ;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Cricinfo.Services.IdentityStore.Models;

namespace Cricinfo.UI.Areas.Identity.Pages.Account
{
    public class ManagePermissionsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ManagePermissionsModel> _logger;

        public List<SelectListItem> Usernames { get; set; }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public IdentityUserClaim<int>[] Claims { get; set; }

        public ManagePermissionsModel(
            UserManager<ApplicationUser> userManager,
            ILogger<ManagePermissionsModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public void OnGet()
        {
            Usernames = _userManager.Users.Select(u => new SelectListItem
            {
                Value = u.UserName,
                Text = u.UserName
            }).ToList();
        }

        public async Task<IActionResult> OnGetUserPermissions(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Content(string.Empty);

            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
                return Content(string.Empty);

            Claims = user.Claims.OrderBy(c => c.ClaimType).ToArray();

            return Partial("_Permissions", this);
        }

        public async Task<IActionResult> OnPost()
        {
            var user = await _userManager.FindByNameAsync(this.Username);

            if (user == null)
            {
                this._logger.LogWarning($"Failed to find user {this.Username}");
                return RedirectToPage("./ManagePermissions");
            }

            var claimsToAdd = this.Claims
                    .Where(claim => !user.Claims.Select(c => c.ClaimType).Contains(claim.ClaimType))
                    .ToList();
            var claimsToRemove = user.Claims
                    .Where(claim => !this.Claims.Select(c => c.ClaimType).Contains(claim.ClaimType))
                    .ToList();
            var claimsToUpdate = this.Claims
                    .Where(claim => user.Claims.Select(c => c.ClaimType).Contains(claim.ClaimType))
                    .Where(claim => user.Claims.First(c => c.ClaimType == claim.ClaimType).ClaimValue != claim.ClaimValue)
                    .Select(claim => (oldClaim: user.Claims.First(c => c.ClaimType == claim.ClaimType), newClaim: claim))
                    .ToList();

            var claimsCreationResult = claimsToAdd.Count > 0
                ? await _userManager.AddClaimsAsync(user, claimsToAdd.Select(c => new Claim(c.ClaimType, c.ClaimValue)))
                : IdentityResult.Success;
            var claimsRemovalResult = claimsToRemove.Count > 0
                ? await _userManager.RemoveClaimsAsync(user, claimsToRemove.Select(c => new Claim(c.ClaimType, c.ClaimValue)))
                : IdentityResult.Success;
            var claimsUpdateResult = claimsToUpdate.Count > 0
                ? await Task.WhenAll(claimsToUpdate.Select(ctu => _userManager.ReplaceClaimAsync(user,
                    new Claim(ctu.oldClaim.ClaimType, ctu.oldClaim.ClaimValue),
                    new Claim(ctu.newClaim.ClaimType, ctu.newClaim.ClaimValue))))
                : new[] { IdentityResult.Success };

            if (claimsCreationResult.Succeeded && claimsRemovalResult.Succeeded && claimsUpdateResult.All(cur => cur.Succeeded))
            {
                this._logger.LogInformation($"Permissions updated for user {this.Username}");
            }
            else
            {
                this._logger.LogWarning($"Failed to update permissions for user {this.Username}");
            }

            return RedirectToPage("./ManagePermissions");
        }
    }
}
