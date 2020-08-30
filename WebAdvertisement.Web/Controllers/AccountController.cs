using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdvertisement.Web.Models.Accounts;

namespace WebAdvertisement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<CognitoUser> _signInManager;
        private readonly UserManager<CognitoUser> _userManager;
        private readonly CognitoUserPool _pool;

        public AccountController(SignInManager<CognitoUser> signInManager, UserManager<CognitoUser> userManager,
            CognitoUserPool pool)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _pool = pool;
        }
        public async Task<IActionResult> SignUp()
        {
            var model = new SignUpViewModel();
            return View(model);
        }
        
        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = _pool.GetUser(model.Email);
                    if (user.Status != null)
                    {
                        ModelState.AddModelError("UserExists", "User with this email already exists");
                        return View(model);
                    }

                    var cognitoAttributeName = CognitoAttribute.Name.AttributeName;
                    user.Attributes.Add(cognitoAttributeName, model.Email);
                    var createdUser = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
                    if (createdUser.Succeeded)
                    {
                       return RedirectToAction("Confirm");
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public async Task<IActionResult> Confirm()
        {
            var model = new ConfirmViewModel();
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user is null)
                {
                    ModelState.AddModelError("NotFound", "A user with the given email not found");
                    return View(model);
                }

                var result = await ((CognitoUserManager<CognitoUser>)_userManager).ConfirmSignUpAsync(user, model.Code, true);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError(item.Code, item.Description);
                }

                return View(model);
            }

            return View(model);
        }
        
        [HttpGet]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            return View(model);
        }

        [HttpPost]
        [ActionName("login")]
        public async Task<IActionResult> LoginSignIn(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");
                
                ModelState.AddModelError("LoginError","Email or password do not match");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PasswordRecreate(PasswordRecreateViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> RedirectToForgotPassword(PasswordRecreateViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email ?? string.Empty);
            if (user is null)
            {
                ModelState.AddModelError("NotFound", "A user with the given email not found");
                return RedirectToAction("PasswordRecreate");
            }

            var result = await ((CognitoUserManager<CognitoUser>)_userManager).ResetPasswordAsync(user);
            if(result.Succeeded)
                return RedirectToAction("ForgetPassword");

            return RedirectToAction("PasswordRecreate");
        }

        [HttpGet]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> RecreatePassword(ForgetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user is null)
                {
                    ModelState.AddModelError("NotFound", "A user with the given email not found");
                    return View(model);
                }

                try
                {
                    await user.ConfirmForgotPasswordAsync(model.Code, model.NewPassword);
                    return RedirectToAction("Index", "Home");
                }
                catch(Exception ex)
                {
                    throw new Exception("Something went wrong.");
                }
            }

            return View(model);
        }
    }
}