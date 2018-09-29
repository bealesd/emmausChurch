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
                options.AccessDeniedPath = "/Ui/LoadLoginView";
                options.SlidingExpiration = false;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSingleton<IServiceRepo>(new ServiceRepo());
            services.AddSingleton<IRotaService>(new RotaService(new RotaRepo()));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseMiddleware(typeof(ErrorHandlingMiddleware));
                //app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            }

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

            var email = "david_beales@ymail.com";
            var password = "FucyeegPeavpoj6";
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
                //var message = string.IsNullOrEmpty(e.Message) ? "No message" : $"{context.Request.Path}: " + e.Message;
                var message = $"{context.Request.Path}: " + ExceptionHelper.GetaAllMessages(e);
                await AzureLogging.CreateLog(message, name, LogLevel.Error);

                await HandleExceptionAsync(context);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context)
        {
            return Task.Run(() => context.Response.Redirect("/Ui/LoadError"));
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