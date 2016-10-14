﻿using System.Threading.Tasks;
using Core.Clients;
using Core.Kyc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using WebAuth.Controllers;
using WebAuth.Managers;
using WebAuth.Models;
using Xunit;

namespace WebAuth.Tests.Controllers
{
    public class AuthenticationControllerTests
    {
        [Fact]
        public async Task RegisterPost_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            //arrange
            var registrationViewModel = new RegistrationViewModel();

            //act
            var controller = CreateAuthenticationController();
            controller.ModelState.AddModelError("Email", "Is required");

            var result = await controller.Register(registrationViewModel);

            //assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task SigninPost_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            //arrange
            var signinViewModel = new SigninViewModel();

            //act
            var controller = CreateAuthenticationController();
            controller.ModelState.AddModelError("Email", "Is required");

            var result = await controller.Signin(signinViewModel);

            //assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task SigninPost_ReturnsErrorPage_WhenModelClientDoesntExist()
        {
            //arrange
            var signinViewModel = new SigninViewModel();

            //act
            var clientRepository = Substitute.For<IClientAccountsRepository>();
            clientRepository.AuthenticateAsync(null, null).Returns((IClientAccount)null);

            var controller = CreateAuthenticationController(clientRepository);

            var result = await controller.Signin(signinViewModel);

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<LoginViewModel>(viewResult.ViewData.Model);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.Equal("Login", viewResult.ViewName);

        }

        private static AuthenticationController CreateAuthenticationController(IClientAccountsRepository clientRepository = null)
        {
            if (clientRepository == null)
                clientRepository = Substitute.For<IClientAccountsRepository>();

            var userManager = Substitute.For<IUserManager>();
            var srvKycManager = Substitute.For<ISrvKycManager>();

            return new AuthenticationController(clientRepository, userManager, srvKycManager);
        }
    }
}