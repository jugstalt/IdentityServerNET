//using System;
//using IdentityServer.Data;
//using IdentityServerNET;
//using IdentityServer4.Configuration;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.UI;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;

//[assembly: HostingStartup(typeof(IdentityServer.Areas.Identity.IdentityHostingStartup))]
//namespace IdentityServer.Areas.Identity
//{
//    public class IdentityHostingStartup : IHostingStartup
//    {
//        public void Configure(IWebHostBuilder builder)
//        {
//            builder.ConfigureServices((context, services) =>
//            {
//                //services.AddDbContext<IdentityServerContext>(options =>
//                //    options.UseSqlite(
//                //          context.Configuration.GetConnectionString("IdentityServerContextConnection")));

//                //services.AddDefaultIdentity<ApplicationUser>(options => 
//                //        options.SignIn.RequireConfirmedAccount = true
//                //    )
//                //    //.AddEntityFrameworkStores<IdentityServerContext>()
//                //    .AddDefaultTokenProviders();  // added
//            });
//        }
//    }
//}