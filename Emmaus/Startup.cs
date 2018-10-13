using Emmaus.Data;
using Emmaus.Logger;
using Emmaus.Models;
using Emmaus.Repos;
using Emmaus.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Emmaus
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
         
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                 options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 6;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

                options.LoginPath = "/Ui/LoadLoginView";
                options.SlidingExpiration = false;
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var tableKey = Configuration.GetSection("TableConfig")["Key"];

            services.AddSingleton<IServiceRepo>(new ServiceRepo());
            services.AddSingleton<IRotaService>(new RotaService(new RotaRepo(tableKey), new RotaNamesRepo(tableKey), new RotaJobsRepo(tableKey)));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            app.UseStatusCodePagesWithReExecute("/error/{0}");

            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Ui}/{action=LoadWelcomeView}/{id?}");
            });

            UserManager<ApplicationUser> userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            RoleManager<IdentityRole> roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            SignInManager<ApplicationUser> signInManager = serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
            var identityRepo = new IdentityRepo(userManager, roleManager, signInManager);

            identityRepo.CreateRolesIfRequiredAsync(Enum.GetNames(typeof(Roles))).Wait();

            var email = Configuration.GetSection("AdminSettings")["email"];
            var password = Configuration.GetSection("AdminSettings")["password"];

            identityRepo.CreateAdminUserIfRequiredAsync(email, password).Wait();
        }
    }

    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                var name = string.IsNullOrEmpty(context.User.Identity.Name) ? "Not logged in" : context.User.Identity.Name;
                var message = $"{context.Request.Path}: " + ExceptionHelper.GetaAllMessages(e);
                var id = await AzureLogging.CreateLog(message, name, LogLevel.Error);

                await HandleExceptionAsync(context, id);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, string logId)
        {
            return Task.Run(() => context.Response.Redirect($"/Ui/LoadError?errorMessage={logId}"));
        }
    }

    public static class ExceptionHelper
    {
        public static string GetaAllMessages(this Exception exp)
        {
            var message = string.Empty;
            Exception innerException = exp;

            do
            {
                message += ". " + (string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message);
                innerException = innerException.InnerException;
            }
            while (innerException != null);

            return message;
        }
    }
}