using IdentityModel;
using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace IMPL.IDS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope()) Database.Initialize(scope.ServiceProvider);
            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }

    public class Database
    {
        public static void Initialize(IServiceProvider provider)
        {
            #region when not use in memory database
            //provider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
            //provider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
            //provider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();
            #endregion

            var context = provider.GetRequiredService<ConfigurationDbContext>();
            if (context.IdentityResources.Any() == false)
            {
                var resources = new List<IdentityResource>
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile()
                };
                foreach (var resource in resources)
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (context.ApiResources.Any() == false)
            {
                var resources = new List<ApiResource>
                {
                    new ApiResource("api1", "My API")
                };
                foreach (var resource in resources)
                {
                    context.ApiResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (context.Clients.Any() == false)
            {
                var clients = new List<Client>
                {
                    new Client
                    {
                        ClientId = "js",
                        ClientName = "JavaScript Client",
                        AllowedGrantTypes = GrantTypes.Code,
                        RequirePkce = true,
                        RequireClientSecret = false,
                        RequireConsent = false,
                        RedirectUris = { "http://localhost:5002/callback.html" },
                        PostLogoutRedirectUris = { "http://localhost:5002/index.html" },
                        AllowedCorsOrigins = { "http://localhost:5002" },
                        AllowedScopes = {"openid", "profile", "api1" }
                    }
                };
                foreach (var client in clients)
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            var manager = provider.GetRequiredService<UserManager<IdentityUser>>();
            var user = manager.FindByNameAsync("bob").Result;
            if (user == null)
            {
                user = new IdentityUser { UserName = "bob" };
                var result = manager.CreateAsync(user, "Pass123$").Result;
                if (result.Succeeded == false)
                    throw new Exception(result.Errors.First().Description);
                user = manager.FindByNameAsync(user.UserName).Result;
                result = manager.AddClaimsAsync(user, new Claim[]
                                {
                                    new Claim(JwtClaimTypes.Name, "Bob Smith"),
                                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                                    new Claim(JwtClaimTypes.Email, "bob@email.com"),
                                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                                    new Claim(JwtClaimTypes.WebSite, "http://www.bob.com"),
                                    new Claim(JwtClaimTypes.Address, @"{ 'province': 'Beijing', 'city': 'Beijing' }",IdentityServerConstants.ClaimValueTypes.Json)
                                })
                                .Result;
                if (result.Succeeded == false)
                    throw new Exception(result.Errors.First().Description);
            }
        }
    }
}

// Install-Package IdentityServer4
// Install-Package IdentityServer4.AccessTokenValidation
// Install-Package IdentityServer4.AspNetIdentity
// Install-Package IdentityServer4.EntityFramework