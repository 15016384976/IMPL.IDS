using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IMPL.IDS.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IMPL.IDS.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IIdentityServerInteractionService _interactionService;
        private readonly IClientStore _clientStore;
        private readonly IEventService _eventService;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IIdentityServerInteractionService interactionService, IClientStore clientStore, IEventService eventService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _interactionService = interactionService;
            _clientStore = clientStore;
            _eventService = eventService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            var model = new LoginModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string button)
        {
            var context = await _interactionService.GetAuthorizationContextAsync(model.ReturnUrl);
            if (button == "CANCEL")
            {
                if (context == null)
                {
                    return Redirect("~/");
                }
                else
                {
                    await _interactionService.GrantConsentAsync(context, ConsentResponse.Denied);
                    if (string.IsNullOrWhiteSpace(context.ClientId) == false)
                    {
                        var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);
                        if (client?.RequirePkce == true)
                        {
                            return View("Redirect", new RedirectModel { RedirectUrl = model.ReturnUrl });
                        }
                    }
                    return Redirect(model.ReturnUrl);
                }
            }
            if (string.IsNullOrEmpty(model.Username)) ModelState.AddModelError(string.Empty, "账号不能为空。");
            if (string.IsNullOrEmpty(model.Password)) ModelState.AddModelError(string.Empty, "密码不能为空。");
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberLogin, false);
                if (result.Succeeded == true)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    await _eventService.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));
                    if (context == null)
                    {
                        if (string.IsNullOrEmpty(model.ReturnUrl))
                        {
                            return Redirect("~/");
                        }
                        else if (Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        else
                        {
                            throw new Exception("无效的返回地址。");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(context.ClientId) == false)
                        {
                            var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);
                            if (client?.RequirePkce == true)
                            {
                                return View("Redirect", new RedirectModel { RedirectUrl = model.ReturnUrl });
                            }
                        }
                        return Redirect(model.ReturnUrl);
                    }
                }
                ModelState.AddModelError(string.Empty, "账号或密码错误，请重新输入。");
                await _eventService.RaiseAsync(new UserLoginFailureEvent(model.Username, "无效的用户凭证。"));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            var model = new LogoutModel { LogoutId = logoutId };
            return await Logout(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutModel model)
        {
            var context = await _interactionService.GetLogoutContextAsync(model.LogoutId);
            model.PostLogoutRedirectUri = context?.PostLogoutRedirectUri;
            if (User?.Identity.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
                await _eventService.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }
            return View(model);
        }
    }
}
