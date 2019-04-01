using IdentityServer4.AccessTokenValidation;
using IMPL.IDS.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace IMPL.IDS
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(opts =>
                    {
                        opts.UseInMemoryDatabase("IMPL.IDS");
                    });
            services.AddIdentity<IdentityUser, IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddIdentityServer(opts =>
                    {
                        opts.Events.RaiseErrorEvents = true;
                        opts.Events.RaiseInformationEvents = true;
                        opts.Events.RaiseFailureEvents = true;
                        opts.Events.RaiseSuccessEvents = true;
                    })
                    .AddDeveloperSigningCredential()
                    .AddConfigurationStore(opts =>
                    {
                        opts.ConfigureDbContext = builder =>
                        {
                            builder.UseInMemoryDatabase("IMPL.IDS");
                        };
                    })
                    .AddOperationalStore(opts =>
                    {
                        opts.ConfigureDbContext = builder =>
                        {
                            builder.UseInMemoryDatabase("IMPL.IDS");
                        };
                        opts.EnableTokenCleanup = true;
                    })
                    .AddAspNetIdentity<IdentityUser>();
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                    .AddIdentityServerAuthentication(opts =>
                    {
                        opts.Authority = "http://localhost:5000";
                        opts.RequireHttpsMetadata = false;
                        opts.ApiName = "api1";
                    });
            services.AddCors(opts =>
            {
                opts.AddPolicy("cors", builder =>
                {
                    builder.WithOrigins("http://localhost:5002")
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.UseCors("cors");
            app.UseIdentityServer();
            app.UseMvcWithDefaultRoute();
        }
    }

    [Route("TestCookieScheme")]
    [Authorize]
    public class TestCookieSchemeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }

    [Route("TestBearerScheme")]
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    public class TestBearerSchemeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return new JsonResult(from claim in User.Claims select new { claim.Type, claim.Value });
        }
    }
}