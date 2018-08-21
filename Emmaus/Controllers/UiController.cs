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

        public UiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            IServiceRepoFactory serviceRepoFactory)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _adultSerivceRepo = serviceRepoFactory.CreateServiceRepo($"{AppDomain.CurrentDomain.BaseDirectory}data/adultService.csv");
            _kidsSerivceRepo = serviceRepoFactory.CreateServiceRepo($"{AppDomain.CurrentDomain.BaseDirectory}data/kidsService.csv");
        }
        public IActionResult LoadLoginView()
        {
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

            return View("Error", "User not created");
        }

        [Authorize(Roles = "admin")]
        public IActionResult LoadCreateUserView(UserInfo login)
        {
            ViewData["Title"] = "CreateUser";

            return View("CreateUserView");
        }

        [Authorize]
        public IActionResult LoadCreateAdultServiceView(UserInfo login)
        {
            ViewData["Title"] = "CreateAdultService";

            return View("CreateAdultServiceView");
        }

        [Authorize(Roles = "admin")]
        public IActionResult LoadCreateRoleView(UserInfo login)
        {
            ViewData["Title"] = "CreateRole";

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
            return View("Error", "Role Already Exists");
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserInfo login)
        {
            ViewBag.Login = login;

            var result = await _signInManager.PasswordSignInAsync(login.EmailAddress, login.Password, false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(login.EmailAddress);
                var role = (await _signInManager.UserManager.GetRolesAsync(user))[0];
                ViewData["Title"] = $"Logged In As {role}";
                return View("LoggedIn");
            }
            if (result.IsLockedOut)
            {
                ViewData["Title"] = "Error";
                return RedirectToPage("Error");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return LoadLoginView();
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout(UserInfo login)
        {
            ViewData["Title"] = "Logged Out";

            await _signInManager.SignOutAsync();
            return LoadLoginView();
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

            return View("Error", "Could not delete user");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            var role = _roleManager.Roles.First(r => r.Name == roleName) ?? null;
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
                return LoadRoleManagementView();

            }
            else
            {
                return View("Error");
            }
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadUserManagementView()
        {
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
        public IActionResult LoadRoleManagementView()
        {
            List<IdentityRole> roles = _roleManager.Roles.ToList();

            ViewData["Roles"] = roles;
            ViewData["Title"] = "RoleManagement";
            return View("RoleManagement");
        }

        [Authorize]
        public IActionResult LoadEditKidSericeView()
        {
            var serviceFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}data/kidsService.csv";
            var childService = ServiceProvider.ReadServices(serviceFilePath);

            ViewData["services"] = childService;
            ViewData["Title"] = "KidsServiceManagement";

            return View("KidsServiceManagement");
        }

        [Authorize]
        public IActionResult LoadAdultServiceManagementView()
        {
            var serviceFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}data/adultService.csv";
            var adultService = ServiceProvider.ReadServices(serviceFilePath);

            ViewData["services"] = adultService;
            ViewData["Title"] = "AdultServiceManagement";

            return View("AdultServiceManagement");
        }

        [Authorize]
        public IActionResult AddAdultService(string date, string summary, string speaker)
        {
            var service = new Service() { Date=date, Summary = summary, Speaker = speaker};

            var serviceFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}data/adultService.csv";
            var added = ServiceProvider.AddService(serviceFilePath, service);
            if (added)
            {
                return LoadAdultServiceManagementView();
            }
            return View("Error", "Could not add service");
        }
        [Authorize]
        public IActionResult DeleteAdultService(string stringService)
        {
            var service = new Service();
            service.ParseStringToParameters(stringService);

            var serviceFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}data/adultService.csv";
            var deleteted = ServiceProvider.DeleteService(serviceFilePath, service);
            if (deleteted)
            {
                return LoadAdultServiceManagementView();
            }
            return View("Error", "Could not delete service");
        }
        
        public IActionResult LoadAboutView()
        {
            ViewData["Title"] = "About";
            return View("About");
        }

        public IActionResult LoadHistoryView()
        {
            ViewData["Title"] = "Church History";
            return View("History");
        }

        public IActionResult LoadMeetTheTeamView()
        {
            ViewData["Title"] = "Meet The Team";
            return View("MeetTheTeam");
        }

        public IActionResult LoadServicesView()
        {
            ViewData["Title"] = "Services";

            return View("Services");
        }
        public IActionResult LoadChildServicesView()
        {
            ViewData["Title"] = "Kids Services";
            ViewData["services"] = _kidsSerivceRepo.GetServices();

            return View("KidServices");
        }

        public IActionResult LoadAdultServicesView()
        {
            ViewData["Title"] = "Adult Services";
            ViewData["services"] = _adultSerivceRepo.GetServices();

            return View("AdultServices");
        }

        public IActionResult LoadWelcomeView()
        {
            ViewData["Title"] = "Welcome";

            return View("Welcome");
        }

        public IActionResult LoadLinksView()
        {
            ViewData["Title"] = "Links";
            return View("Links");
        }

        public IActionResult LoadContactUsView()
        {
            ViewData["Title"] = "Contacts US";
            return View("ContactUs");
        }

        public IActionResult LoadLocalCommunityView()
        {
            ViewData["Title"] = "Local Community";
            return View("LocalCommunity");
        }

        public IActionResult LoadWiderCommunityView()
        {
            ViewData["Title"] = "Wider Community";
            return View("WiderCommunity");
        }

        public IActionResult LoadAtHomeView()
        {
            ViewData["Title"] = "At Home Wider Community";
            return View("WiderCommunity");
        }

        public IActionResult LoadOverseasView()
        {
            ViewData["Title"] = "Overseas Wider Community";
            return View("WiderCommunity");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}