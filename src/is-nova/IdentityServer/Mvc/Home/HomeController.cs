// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer.Nova;
using IdentityServer.Nova.Extensions;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Services;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace IdentityServer;

[SecurityHeaders]
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ColorSchemeService _colorScheme;
    private readonly IConfiguration _config;

    public HomeController(IIdentityServerInteractionService interaction,
                          IWebHostEnvironment environment,
                          ILogger<HomeController> logger,
                          UserManager<ApplicationUser> userManager,
                          ColorSchemeService colorScheme,
                          IConfiguration config)
    {
        _interaction = interaction;
        _environment = environment;
        _logger = logger;
        _userManager = userManager;
        _colorScheme = colorScheme;
        _config = config;
    }

    async public Task<IActionResult> Index()
    {
        var applicationUser = await _userManager.GetUserAsync(User);

        string redirectAuth = _config["IdentityServer:RedirectAuthUsersToClientUrl"];
        string redirectAnonymous = _config["IdentityServer:RedirectAnonymousUsersToClientUrl"];

        if (!String.IsNullOrEmpty(redirectAuth) &&
            applicationUser != null &&
            !applicationUser.HasAdministratorRole())
        {
            return Redirect(redirectAuth);
        }

        if (!String.IsNullOrEmpty(redirectAnonymous) &&
            String.IsNullOrEmpty(applicationUser?.UserName))
        {
            return Redirect(redirectAnonymous);
        }

        return View(applicationUser);
    }
    public IActionResult About()
    {
        return View();
        //if (_environment.IsDevelopment())
        //{
        //    // only show in development
        //    return View();
        //}

        //_logger.LogInformation("Homepage is disabled in production. Returning 404.");
        //return NotFound();
    }

    async public Task<IActionResult> ToggleColorScheme()
    {
        var applicationUser = await _userManager.GetUserAsync(User);

        switch(_colorScheme.CurrentColorScheme)
        {
            case ColorSchemes.Light:
                _colorScheme.SetColorScheme(ColorSchemes.Dark);
                break;
            default:
                _colorScheme.SetColorScheme(ColorSchemes.Light);
                break;
        }
        


        return View("index", applicationUser);
    }

    /// <summary>
    /// Shows the error page
    /// </summary>
    public async Task<IActionResult> Error(string errorId)
    {
        var vm = new ErrorViewModel();

        // retrieve error details from identityserver
        var message = await _interaction.GetErrorContextAsync(errorId);
        if (message != null)
        {
            vm.Error = message;

            if (!_environment.IsDevelopment())
            {
                // only show in development
                message.ErrorDescription = null;
            }
        }

        return View("Error", vm);
    }
}