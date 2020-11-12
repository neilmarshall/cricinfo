using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Cricinfo.Services.IdentityStore.Models;
using Cricinfo.UI.Areas.Identity.Pages.Account;

namespace Cricinfo.UI.Unit.Tests
{
    [TestClass]
    public class ManagePermissionsFixtures
    {
        [TestMethod]
        public async Task POST_EndpointUpdatesClaimsCorrectly()
        {
            var mockUserClaimStore = new Mock<IUserClaimStore<ApplicationUser>>();

            mockUserClaimStore
                .Setup(us => us.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new ApplicationUser
                {
                    Claims = new List<IdentityUserClaim<int>>
                    {
                        new IdentityUserClaim<int> { ClaimType = "ClaimType1", ClaimValue = "ClaimValue1" },
                        new IdentityUserClaim<int> { ClaimType = "ClaimType2", ClaimValue = "ClaimValue2" }
                    }
                }));

            mockUserClaimStore
                .Setup(us => us.ReplaceClaimAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<Claim>(),
                    It.IsAny<Claim>(),
                    It.IsAny<CancellationToken>()))
                .Verifiable();

            mockUserClaimStore
                .Setup(us => us.UpdateAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Success));

            var managePermissionsModel = new ManagePermissionsModel(
                new UserManager<ApplicationUser>(mockUserClaimStore.Object, null, null, null, null, null, null, null, null),
                new Mock<ILogger<ManagePermissionsModel>>().Object)
            {
                Username = "TestUser",
                Claims = new[]
                {
                    new IdentityUserClaim<int> { ClaimType = "ClaimType1", ClaimValue = "ClaimValue1-x" },
                    new IdentityUserClaim<int> { ClaimType = "ClaimType2", ClaimValue = "ClaimValue2-x" }
                }
            };

            await managePermissionsModel.OnPost();

            mockUserClaimStore
                .Verify(
                    us => us.ReplaceClaimAsync(
                        It.IsAny<ApplicationUser>(),
                        It.Is<Claim>(c => c.Type == "ClaimType1" && c.Value == "ClaimValue1"),
                        It.Is<Claim>(c => c.Type == "ClaimType1" && c.Value == "ClaimValue1-x"),
                        It.IsAny<CancellationToken>()),
                    Times.Once);

            mockUserClaimStore
                .Verify(
                    us => us.ReplaceClaimAsync(
                        It.IsAny<ApplicationUser>(),
                        It.Is<Claim>(c => c.Type == "ClaimType2" && c.Value == "ClaimValue2"),
                        It.Is<Claim>(c => c.Type == "ClaimType2" && c.Value == "ClaimValue2-x"),
                        It.IsAny<CancellationToken>()),
                    Times.Once);

            mockUserClaimStore
                .Verify(
                    us => us.ReplaceClaimAsync(
                        It.IsAny<ApplicationUser>(),
                        It.IsAny<Claim>(),
                        It.IsAny<Claim>(),
                        It.IsAny<CancellationToken>()),
                    Times.Exactly(2));
        }
    }
}
