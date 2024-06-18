using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace IdentityServer.Nova
{
    public class NovaProfileService : IProfileService
    {
        private UserManager<ApplicationUser> _userManager;

        public NovaProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var user = _userManager.GetUserAsync(context.Subject).Result;

            //var claims = new List<Claim>();
            //claims.AddRange(user.Claims);
            //context.IssuedClaims.AddRange(claims);

            //List<string> claimNames = new List<string>();
            //claimNames.Add("preferred_username");
            //claimNames.AddRange(user.Claims.Select(c => c.Type));

            //context.RequestedClaimTypes = claimNames;
            //context.AddRequestedClaims(context.Subject.Claims);

            return Task.FromResult(0);

        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            var user = _userManager.GetUserAsync(context.Subject).Result;
            context.IsActive = user != null;
            return Task.FromResult(0);
        }
    }
}
