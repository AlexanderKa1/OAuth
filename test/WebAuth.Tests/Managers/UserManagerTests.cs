﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using Common.OpenIdConnect;
using Core.Clients;
using Core.Kyc;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using WebAuth.Managers;
using Xunit;

namespace WebAuth.Tests.Managers
{
    public class UserManagerTests
    {
        private static UserManager CreateUserManager(IPersonalDataRepository personalDataRepository = null)
        {
            if (personalDataRepository == null)
            {
                personalDataRepository = Substitute.For<IPersonalDataRepository>();
            }

            var kycDocumentRepository = Substitute.For<IKycDocumentsRepository>();
            var httpAccessor = Substitute.For<IHttpContextAccessor>();
            var userManager = new UserManager(personalDataRepository, kycDocumentRepository, httpAccessor);
            return userManager;
        }

        [Fact]
        public async Task Email_IsRequired_ForUserIdentity()
        {
            //arrange
            var client = new ClientAccount
            {
                Email = null,
                Id = "test"
            };

            var personalData = new FullPersonalData();

            //act
            var personalDataRepository = Substitute.For<IPersonalDataRepository>();
            personalDataRepository.GetAsync(Arg.Any<string>()).ReturnsForAnyArgs(personalData);

            var userManager = CreateUserManager(personalDataRepository);

            //assert
            await
                Assert.ThrowsAsync<ArgumentNullException>(
                    async () => await userManager.CreateUserIdentityAsync(client, "test"));
        }

        [Fact]
        public void Identity_ShouldContainCountry_IfScopeIsAddress()
        {
            //arrange
            var scopes = new List<string>
            {
                OpenIdConnectConstants.Scopes.Address
            };

            var country = "Test";
            var claims = new List<Claim>
            {
                new Claim(OpenIdConnectConstantsExt.Claims.Country, country)
            };

            //act
            var userManager = CreateUserManager();
            var result = userManager.CreateIdentity(scopes, claims);

            //assert
            Assert.Equal(country, result.GetClaim(OpenIdConnectConstantsExt.Claims.Country));
        }

        [Fact]
        public void Identity_ShouldContainData_ForSpecifiedScope()
        {
            //arrange
            var scopes = new List<string>
            {
                OpenIdConnectConstants.Scopes.Email,
                OpenIdConnectConstants.Scopes.Profile
            };
            var claims = new List<Claim>
            {
                new Claim(OpenIdConnectConstants.Claims.Email, "test@test.com"),
                new Claim(OpenIdConnectConstants.Claims.FamilyName, "Smith")
            };

            //act
            var userManager = CreateUserManager();
            var result = userManager.CreateIdentity(scopes, claims);

            //assert
            Assert.Equal("test@test.com", result.GetClaim(OpenIdConnectConstants.Claims.Email));
            Assert.Equal("Smith", result.GetClaim(OpenIdConnectConstants.Claims.FamilyName));
        }

        [Fact]
        public void Identity_ShouldContainDocuments_IfScopeIsProfile()
        {
            //arrange
            var scopes = new List<string>
            {
                OpenIdConnectConstants.Scopes.Profile
            };

            var documents = "proofOfAddress,idCard";
            var claims = new List<Claim>
            {
                new Claim(OpenIdConnectConstantsExt.Claims.Documents, documents)
            };

            //act
            var userManager = CreateUserManager();
            var result = userManager.CreateIdentity(scopes, claims);

            //assert
            Assert.Equal(documents, result.GetClaim(OpenIdConnectConstantsExt.Claims.Documents));
        }

        [Fact]
        public void Identity_ShouldNotContainEmail_IfScopeContainsProfileOnly()
        {
            //arrange
            var scopes = new List<string>
            {
                OpenIdConnectConstants.Scopes.Profile
            };
            var claims = new List<Claim>
            {
                new Claim(OpenIdConnectConstants.Claims.Email, "test@test.com")
            };

            //act
            var userManager = CreateUserManager();
            var result = userManager.CreateIdentity(scopes, claims);

            //assert
            Assert.Equal(null, result.GetClaim(OpenIdConnectConstants.Claims.Email));
        }

        [Fact]
        public void Identity_ShouldNotContainEmail_IfScopeIsEmpty()
        {
            //arrange
            var scopes = new List<string>();
            var claims = new List<Claim>
            {
                new Claim(OpenIdConnectConstants.Claims.Email, "test@test.com")
            };

            //act
            var userManager = CreateUserManager();
            var result = userManager.CreateIdentity(scopes, claims);

            //assert
            Assert.Equal(null, result.GetClaim(OpenIdConnectConstants.Claims.Email));
        }

        [Fact]
        public void Identity_ShouldNotContainFirstOrLastName_IfScopeContainsEmailOnly()
        {
            //arrange
            var scopes = new List<string>
            {
                OpenIdConnectConstants.Scopes.Email
            };
            var claims = new List<Claim>
            {
                new Claim(OpenIdConnectConstants.Claims.GivenName, "John"),
                new Claim(OpenIdConnectConstants.Claims.FamilyName, "Smith")
            };

            //act
            var userManager = CreateUserManager();
            var result = userManager.CreateIdentity(scopes, claims);

            //assert
            Assert.Equal(null, result.GetClaim(OpenIdConnectConstants.Claims.GivenName));
            Assert.Equal(null, result.GetClaim(OpenIdConnectConstants.Claims.FamilyName));
        }

        [Fact]
        public void Identity_ShouldNotContainFirstOrLastName_IfScopeIsEmpty()
        {
            //arrange
            var scopes = new List<string>();
            var claims = new List<Claim>
            {
                new Claim(OpenIdConnectConstants.Claims.GivenName, "John"),
                new Claim(OpenIdConnectConstants.Claims.FamilyName, "Smith")
            };

            //act
            var userManager = CreateUserManager();
            var result = userManager.CreateIdentity(scopes, claims);

            //assert
            Assert.Equal(null, result.GetClaim(OpenIdConnectConstants.Claims.GivenName));
            Assert.Equal(null, result.GetClaim(OpenIdConnectConstants.Claims.FamilyName));
        }

        [Fact]
        public async Task OptionalFields_AreAdded_ToUserIdentity()
        {
            //arrange
            var client = new ClientAccount
            {
                Email = "test@test.com",
                Id = "test"
            };

            var personalData = new FullPersonalData
            {
                FirstName = "test",
                LastName = "test",
                ContactPhone = "11",
                Country = "Test"
            };

            //act
            var personalDataRepository = Substitute.For<IPersonalDataRepository>();
            personalDataRepository.GetAsync(Arg.Any<string>()).ReturnsForAnyArgs(personalData);

            var userManager = CreateUserManager(personalDataRepository);
            var result = await userManager.CreateUserIdentityAsync(client, "test");

            //assert
            Assert.Equal(personalData.FirstName, result.GetClaim(OpenIdConnectConstants.Claims.GivenName));
            Assert.Equal(personalData.LastName, result.GetClaim(OpenIdConnectConstants.Claims.FamilyName));
            Assert.Equal(client.Email, result.GetClaim(OpenIdConnectConstants.Claims.Email));
            Assert.Equal(personalData.Country, result.GetClaim(OpenIdConnectConstantsExt.Claims.Country));
            Assert.Equal(7, result.Claims.Count());
        }
    }
}