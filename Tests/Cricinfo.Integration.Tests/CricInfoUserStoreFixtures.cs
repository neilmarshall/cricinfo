using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cricinfo.Services.IdentityStore;
using Cricinfo.Services.IdentityStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cricinfo.Integration.Tests
{
    [TestClass]
    public class CricInfoUserStoreFixtures
    {
        private static IUserStore<ApplicationUser> userStore;

        [ClassInitialize]
        public static void Initialize(TestContext _)
        {
            var dbConnectionString = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location).ConnectionStrings.ConnectionStrings["DbConnectionString"].ConnectionString;
            userStore = new CricInfoUserStore<CricInfoUserStoreFixtures>(dbConnectionString);
        }

        [TestMethod]
        public async Task CreateUserFIxture()
        {
            var expected = new ApplicationUser
            {
                UserName = "Test User",
                PasswordHash = "abcdefhijklmnopqrstuvwxyz",
                Claims = new List<IdentityUserClaim<int>>
                {
                    new IdentityUserClaim<int> { ClaimType = "TestClaimType1", ClaimValue = "TestClaimValue1" },
                    new IdentityUserClaim<int> { ClaimType = "TestClaimType2", ClaimValue = "TestClaimValue2" }
                }
            };

            var response = await userStore.CreateAsync(expected, System.Threading.CancellationToken.None);

            Assert.IsTrue(response.Succeeded);

            var actualByName = await userStore.FindByNameAsync(expected.UserName, System.Threading.CancellationToken.None);
            Assert.IsNotNull(actualByName);
            Assert.AreEqual(expected.UserName, actualByName.UserName);
            Assert.AreEqual(expected.PasswordHash, actualByName.PasswordHash);
            CollectionAssert.AreEqual(
                expected.Claims.Select(iuc => (iuc.ClaimType, iuc.ClaimValue)).ToArray(),
                actualByName.Claims.Select(iuc => (iuc.ClaimType, iuc.ClaimValue)).ToArray());

            var actualById = await userStore.FindByIdAsync(actualByName.Id.ToString(), System.Threading.CancellationToken.None);
            Assert.IsNotNull(actualById);
            Assert.AreEqual(expected.UserName, actualById.UserName);
            Assert.AreEqual(expected.PasswordHash, actualById.PasswordHash);
            CollectionAssert.AreEqual(
                expected.Claims.Select(iuc => (iuc.ClaimType, iuc.ClaimValue)).ToArray(),
                actualById.Claims.Select(iuc => (iuc.ClaimType, iuc.ClaimValue)).ToArray());

            await userStore.DeleteAsync(expected, System.Threading.CancellationToken.None);
        }

        [TestMethod]
        public async Task UpdateUserFIxture()
        {
            var expected = new ApplicationUser
            {
                UserName = "Test User",
                PasswordHash = "abcdefhijklmnopqrstuvwxyz",
                Claims = new List<IdentityUserClaim<int>>
                {
                    new IdentityUserClaim<int> { ClaimType = "TestClaimType1", ClaimValue = "TestClaimValue1" },
                    new IdentityUserClaim<int> { ClaimType = "TestClaimType2", ClaimValue = "TestClaimValue2" }
                }
            };

            var response = await userStore.CreateAsync(expected, System.Threading.CancellationToken.None);

            var actualByName = await userStore.FindByNameAsync(expected.UserName, System.Threading.CancellationToken.None);

            expected.Id = actualByName.Id;
            expected.PasswordHash = "lknlnascac";
            expected.Claims[1].ClaimValue = "TestClaimValue2 - UPDATED";
            expected.Claims.RemoveAt(0);
            expected.Claims.Add(new IdentityUserClaim<int> { ClaimType = "TestClaimType3", ClaimValue = "TestClaimValue3" });

            await userStore.UpdateAsync(expected, System.Threading.CancellationToken.None);

            actualByName = await userStore.FindByNameAsync(actualByName.UserName, System.Threading.CancellationToken.None);

            Assert.AreEqual(expected.UserName, actualByName.UserName);
            Assert.AreEqual(expected.PasswordHash, actualByName.PasswordHash);
            CollectionAssert.AreEqual(
                expected.Claims.Select(iuc => (iuc.ClaimType, iuc.ClaimValue)).ToArray(),
                actualByName.Claims.Select(iuc => (iuc.ClaimType, iuc.ClaimValue)).ToArray());

            await userStore.DeleteAsync(expected, System.Threading.CancellationToken.None);
        }
    }
}
