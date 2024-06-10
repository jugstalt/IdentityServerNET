using IdentityModel;
using IdentityServer.Nova;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer.Nova.UserInteraction;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IUserStoreFactory _userStoreFactory;

        public RegisterModel(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IUserStoreFactory userStoreFactory)
        {
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _userStoreFactory = userStoreFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email *")]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Given Name *")]
            public string GivenName { get; set; }

            [Required]
            [Display(Name = "Family Name *")]
            public string FamilyName { get; set; }

            //[Required]
            [Display(Name = "Company")]
            public string Company { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password *")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password *")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Promotion Code *")]
            public string PromotionCode { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (_configuration.DenyRegisterAccount())
            {
                return RedirectToPage("/");
            }

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (_configuration.DenyRegisterAccount())
            {
                return RedirectToPage("/");
            }


            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var userDbContext = await _userStoreFactory.CreateUserDbContextInstance();
                var editor = userDbContext?.ContextConfiguration?.RegisterAccountEditor ?? new RegisterAccountEditor();

                if (!String.IsNullOrEmpty(editor.PromotionCode) &&
                    editor.PromotionCode != Input.PromotionCode)
                {
                    ModelState.AddModelError("PromotionCode", "Sorry, you are not allowed to register. Invalid/Wrong Promotion Code");
                    return Page();
                }

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    Claims = new List<Claim>()
                };
                if (editor.ShowGivenName)
                {
                    user.Claims.Add(new Claim(JwtClaimTypes.GivenName, Input.GivenName));
                }
                if (editor.ShowFamilyName)
                {
                    user.Claims.Add(new Claim(JwtClaimTypes.FamilyName, Input.FamilyName));
                }
                if (editor.ShowCompany)
                {
                    user.Claims.Add(new Claim("company", Input.Company));
                }

                if (userDbContext is IUserDbContextPreActions)
                {
                    user = await ((IUserDbContextPreActions)userDbContext).PreCreateAsync(user, new System.Threading.CancellationToken());
                }

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }


            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
