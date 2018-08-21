using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Emmaus.Models;
using Emmaus.Repos;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Emmaus.Controllers
{
    public class UiController : Controller
    {
        private SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private IServiceRepo _adultSerivceRepo;
        private IServiceRepo _kidsSerivceRepo;

        public RoleManager<ApplicationUser> RoleManager { get; }

        public UiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            var serviceRepoFactory = new ServiceRepoFactory();
            _adultSerivceRepo = serviceRepoFactory.CreateServiceRepo($"{AppDomain.CurrentDomain.BaseDirectory}data/adultService.csv");
            _kidsSerivceRepo = serviceRepoFactory.CreateServiceRepo($"{AppDomain.CurrentDomain.BaseDirectory}data/kidsService.csv");
        }
        public async Task<IActionResult> LoadLoginView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Login";
            return View("Login");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateUser(UserInfo login)
        {
            ViewData["Title"] = "User Management";

            var user = new ApplicationUser { UserName = login.EmailAddress, Email = login.EmailAddress };
            var result = await _userManager.CreateAsync(user, login.Password);
            if (result.Succeeded)
            {
                Task<IdentityResult> addToRoleResult = _userManager.AddToRoleAsync(user, login.Role);
                addToRoleResult.Wait();
                return RedirectToAction(nameof(LoadUserManagementView));
            }

            await SetIsAdminAndAuthenticatedViewBag();
            return View("Error", "User not created");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadCreateUserView(UserInfo login)
        {
            ViewData["Title"] = "CreateUser";
            await SetIsAdminAndAuthenticatedViewBag();

            return View("CreateUserView");
        }

        [Authorize]
        public async Task<IActionResult> LoadCreateAdultServiceView(UserInfo login)
        {
            ViewData["Title"] = "CreateAdultService";
            await SetIsAdminAndAuthenticatedViewBag();

            return View("CreateAdultServiceView");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadCreateRoleView(UserInfo login)
        {
            ViewData["Title"] = "CreateRole";
            await SetIsAdminAndAuthenticatedViewBag();

            return View("CreateRoleView");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateRole(RoleInfo roleInfo)
        {
            ViewData["Title"] = "Role Management";
            bool roleExists = await _roleManager.RoleExistsAsync(roleInfo.Rolename);
            if (!roleExists)
            {
                var role = new IdentityRole();
                role.Name = roleInfo.Rolename;
                await _roleManager.CreateAsync(role);
                return RedirectToAction(nameof(LoadRoleManagementView));
            }
            await SetIsAdminAndAuthenticatedViewBag();
            return View("Error", "Role Already Exists");
        }


        [Authorize]
        public async Task<IActionResult> LoadLoggedInView(UserInfo login)
        {
            await SetIsAdminAndAuthenticatedViewBag(login.EmailAddress);
            ViewData["Title"] = $"Logged In";

            return View("LoggedIn");
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserInfo login)
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewBag.Login = login;

            var result = await _signInManager.PasswordSignInAsync(login.EmailAddress, login.Password, false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                return await LoadLoggedInView(login);
            }
            if (result.IsLockedOut)
            {
                ViewData["Title"] = "Error";
                return RedirectToPage("Error");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return await LoadLoginView();
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout(UserInfo login)
        {
            ViewData["Title"] = "Logged Out";
            ViewBag.IsAuthenticated = false;

            await _signInManager.SignOutAsync();
            await SetIsAdminAndAuthenticatedViewBag();

            return await LoadLoginView();
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var deletedResult = await _userManager.DeleteAsync(user);
            if (deletedResult.Succeeded)
            {
                return await LoadUserManagementView();
            }
            await SetIsAdminAndAuthenticatedViewBag();
            return View("Error", "Could not delete user");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            var role = _roleManager.Roles.First(r => r.Name == roleName) ?? null;
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
                return await LoadRoleManagementView();

            }
            else
            {
                await SetIsAdminAndAuthenticatedViewBag();
                return View("Error");
            }
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadUserManagementView()
        {
            await SetIsAdminAndAuthenticatedViewBag();

            var users = _userManager.Users.ToList();
            var userInfos = new List<UserInfo>() { };

            foreach (var user in users)
            {
                var applicationUser = await _userManager.FindByEmailAsync(user.Email);
                var role = (await _signInManager.UserManager.GetRolesAsync(applicationUser))[0];
                userInfos.Add(new UserInfo() { EmailAddress = user.Email, Role = role });
            }

            ViewData["Title"] = "UserManagement";
            ViewData["users"] = userInfos;
            return View("UserManagement");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadRoleManagementView()
        {
            await SetIsAdminAndAuthenticatedViewBag();

            List<IdentityRole> roles = _roleManager.Roles.ToList();

            ViewData["Roles"] = roles;
            ViewData["Title"] = "RoleManagement";
            return View("RoleManagement");
        }

        [Authorize]
        public async Task<IActionResult> LoadKidsServiceManagementView()
        {
            await SetIsAdminAndAuthenticatedViewBag();

            ViewData["services"] = _kidsSerivceRepo.GetServices();
            ViewData["Title"] = "KidsServiceManagement";

            return View("KidsServiceManagement");
        }

        [Authorize]
        public async Task<IActionResult> LoadAdultServiceManagementView()
        {
            await SetIsAdminAndAuthenticatedViewBag();

            ViewData["services"] = _adultSerivceRepo.GetServices();
            ViewData["Title"] = "AdultServiceManagement";

            return View("AdultServiceManagement");
        }

        [Authorize]
        public async Task<IActionResult> AddAdultService(string date, string summary, string speaker)
        {
            var service = new Service() { Date = date, Summary = summary, Speaker = speaker };

            _adultSerivceRepo.AddService(service);
            return await LoadAdultServiceManagementView();

            await SetIsAdminAndAuthenticatedViewBag();
            return View("Error", "Could not add service");
        }

        [Authorize]
        public async Task<IActionResult> DeleteAdultService(string stringService)
        {
            var service = new Service();
            service.ParseStringToParameters(stringService);
            _adultSerivceRepo.DeleteService(service);
            return await LoadAdultServiceManagementView();

            await SetIsAdminAndAuthenticatedViewBag();
            return View("Error", "Could not delete service");
        }

        [Authorize]
        public async Task<IActionResult> AddKidService(string date, string summary, string speaker)
        {
            var service = new Service() { Date = date, Summary = summary, Speaker = speaker };

            _kidsSerivceRepo.AddService(service);
            return await LoadAdultServiceManagementView();

            await SetIsAdminAndAuthenticatedViewBag();
            return View("Error", "Could not add service");
        }

        [Authorize]
        public async Task<IActionResult> DeleteKidsService(string stringService)
        {
            var service = new Service();
            service.ParseStringToParameters(stringService);
            _kidsSerivceRepo.DeleteService(service);
            return await LoadAdultServiceManagementView();

            await SetIsAdminAndAuthenticatedViewBag();
            return View("Error", "Could not delete service");
        }

        public async Task<IActionResult> LoadAboutView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "About";
            return View("About");
        }

        public async Task<IActionResult> LoadHistoryView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Church History";
            return View("History");
        }

        public async Task<IActionResult> LoadMeetTheTeamView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Meet The Team";
            return View("MeetTheTeam");
        }

        public async Task<IActionResult> LoadServicesView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Services";
            return View("Services");
        }
        public async Task<IActionResult> LoadChildServicesView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Kids Services";
            ViewData["services"] = _kidsSerivceRepo.GetServices();
            return View("KidServices");
        }

        public async Task<IActionResult> LoadAdultServicesView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Adult Services";
            ViewData["services"] = _adultSerivceRepo.GetServices();
            return View("AdultServices");
        }

        public async Task<IActionResult> LoadWelcomeView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Welcome";
            return View("Welcome");
        }

        public async Task<IActionResult> LoadLinksView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Links";
            return View("Links");
        }

        public async Task<IActionResult> LoadContactUsView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Contacts US";
            return View("ContactUs");
        }

        public async Task<IActionResult> LoadLocalCommunityView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Local Community";
            return View("LocalCommunity");
        }

        public async Task<IActionResult> LoadWiderCommunityView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Wider Community";
            return View("WiderCommunity");
        }

        public async Task<IActionResult> LoadAtHomeView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "At Home Wider Community";
            return View("WiderCommunity");
        }

        public async Task<IActionResult> LoadOverseasView()
        {
            await SetIsAdminAndAuthenticatedViewBag();
            ViewData["Title"] = "Overseas Wider Community";
            return View("WiderCommunity");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<string> GetCurrentUsersRole(string email = null)
        {
            if (email != null)
            {
                var user = await _userManager.FindByEmailAsync(email);
                return (await _signInManager.UserManager.GetRolesAsync(user))[0];
            }
            else
            {
                var username = _userManager.GetUserName(HttpContext.User);
                if (username == null)
                {
                    return null;
                }
                var appUser = await _userManager.FindByEmailAsync(username);
                return (await _signInManager.UserManager.GetRolesAsync(appUser))[0];
            }
        }

        private async Task SetIsAdminAndAuthenticatedViewBag(string email = null, [CallerMemberName] string callerName = "")
        {
            if (callerName == "Logout" || callerName == "LoadLoginView")
            {
                ViewBag.IsAdmin = false;
                ViewBag.IsAuthenticated = false;
                return;
            }

            var role = await GetCurrentUsersRole(email);
            if (role == "admin") ViewBag.IsAdmin = true;
            else ViewBag.IsAdmin = false;

            if (role == null) ViewBag.IsAuthenticated = false;
            else ViewBag.IsAuthenticated = true;
        }
    }
}