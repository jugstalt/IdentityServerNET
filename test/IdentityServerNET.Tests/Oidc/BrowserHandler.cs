// Based on IdentityServer4 integration test infrastructure.
// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Tests.Oidc;

/// <summary>
/// A <see cref="DelegatingHandler"/> that emulates a browser by persisting cookies across
/// requests and following redirects. This is what makes interactive flows (e.g. authorization
/// code) testable against an in-memory <see cref="Microsoft.AspNetCore.TestHost.TestServer"/>.
/// Ported from the IdentityServer4 integration tests (originally by Damian Hickey).
/// </summary>
public class BrowserHandler : DelegatingHandler
{
    private readonly CookieContainer _cookieContainer = new();

    public bool AllowCookies { get; set; } = true;
    public bool AllowAutoRedirect { get; set; } = true;
    public int ErrorRedirectLimit { get; set; } = 20;
    public int StopRedirectingAfter { get; set; } = int.MaxValue;

    public BrowserHandler(HttpMessageHandler next)
        : base(next)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await SendCookiesAsync(request, cancellationToken);

        int redirectCount = 0;

        while (AllowAutoRedirect &&
            (300 <= (int)response.StatusCode && (int)response.StatusCode < 400) &&
            redirectCount < StopRedirectingAfter)
        {
            if (redirectCount >= ErrorRedirectLimit)
            {
                throw new InvalidOperationException($"Too many redirects. Error limit = {redirectCount}");
            }

            var location = response.Headers.Location!;
            if (!location.IsAbsoluteUri)
            {
                location = new Uri(response.RequestMessage!.RequestUri!, location);
            }

            request = new HttpRequestMessage(HttpMethod.Get, location);

            response = await SendCookiesAsync(request, cancellationToken).ConfigureAwait(false);

            redirectCount++;
        }

        return response;
    }

    public Cookie? GetCookie(string uri, string name)
        => _cookieContainer.GetCookies(new Uri(uri)).Cast<Cookie>().FirstOrDefault(x => x.Name == name);

    public void RemoveCookie(string uri, string name)
    {
        var cookie = _cookieContainer.GetCookies(new Uri(uri)).Cast<Cookie>().FirstOrDefault(x => x.Name == name);
        if (cookie != null)
        {
            cookie.Expired = true;
        }
    }

    private async Task<HttpResponseMessage> SendCookiesAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (AllowCookies)
        {
            string cookieHeader = _cookieContainer.GetCookieHeader(request.RequestUri!);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (AllowCookies && response.Headers.Contains("Set-Cookie"))
        {
            var responseCookieHeader = string.Join(",", response.Headers.GetValues("Set-Cookie"));
            _cookieContainer.SetCookies(request.RequestUri!, responseCookieHeader);
        }

        return response;
    }
}
