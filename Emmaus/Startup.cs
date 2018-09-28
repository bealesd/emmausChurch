using Emmaus.Data;
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
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

                options.LoginPath = "/Ui/LoadLoginView";
                options.AccessDeniedPath = "/Ui/LoadLoginView";
                options.SlidingExpiration = false;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            //services.AddSingleton<IServiceRepo>(new ServiceCosmosRepo(new DocumentDBRepo<Service>()));
            services.AddSingleton<IServiceRepo>(new ServiceRepo());
            //services.AddSingleton<IRotaRepo>(new RotaRepo());
            services.AddSingleton<IRotaService>(new RotaService(new RotaRepo()));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                //app.UseMiddleware(typeof(ErrorHandlingMiddleware));
                app.UseDeveloperExceptionPage();
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

            CreateSuperUser(serviceProvider).Wait();
        }

        public async Task CreateSuperUser(IServiceProvider serviceProvider)
        {
            UserManager<ApplicationUser> _userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            RoleManager<IdentityRole> _roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var roleExists = await _roleManager.RoleExistsAsync(Roles.admin.ToString());
            if (!roleExists)
            {
                var role = new IdentityRole();
                role.Name = Roles.admin.ToString();
                IdentityResult roleCreated = await _roleManager.CreateAsync(role);
            }

            var email = "david_beales@ymail.com";
            ApplicationUser user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser { UserName = email, Email = email };
                IdentityResult createUserResult = await _userManager.CreateAsync(user, "FucyeegPeavpoj6");
            }
            System.Collections.Generic.IList<string> usersCurrentRoles = await _userManager.GetRolesAsync(user);
            if (!usersCurrentRoles.Contains(Roles.admin.ToString()))
            {
                IdentityResult addToRoleResult = await _userManager.AddToRoleAsync(user, Roles.admin.ToString());
            }

            //create roles if dont exists
            foreach (var roleName in Enum.GetNames(typeof(Roles)))
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var newRole = new IdentityRole();
                    newRole.Name = roleName;
                    await _roleManager.CreateAsync(newRole);
                }
            }
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            return Task.Run(() => context.Response.Redirect("/Ui/LoadError"));
        }
    }
}