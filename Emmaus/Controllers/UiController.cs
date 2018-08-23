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
using System.Globalization;

namespace Emmaus.Controllers
{
    public class UiController : Controller
    {
        private SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private ServiceCosmosRepo _adultServiceRepo;
        private ServiceCosmosRepo _kidsServiceRepo;

        public RoleManager<ApplicationUser> RoleManager { get; }

        public UiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _adultServiceRepo = new ServiceCosmosRepo(new DocumentDBRepo<ServiceCosmos>());
            _kidsServiceRepo = new ServiceCosmosRepo(new DocumentDBRepo<ServiceCosmos>());
        }

        public async Task<IActionResult> LoadLoginView()
        {
            ViewData["Title"] = "Login";
            return View("Login");
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserInfo login)
        {
            ViewBag.Login = login;

            var result = await _signInManager.PasswordSignInAsync(login.EmailAddress, login.Password, false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                return Redirect("~/ ");
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

            await _signInManager.SignOutAsync();

            return Redirect("~/ ");
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
        public async Task<IActionResult> LoadCreateUserView(UserInfo login)
        {
            ViewData["Title"] = "CreateUser";

            return View("CreateUserView");
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
        public async Task<IActionResult> LoadRoleManagementView()
        {
            List<IdentityRole> roles = _roleManager.Roles.ToList();

            ViewData["Roles"] = roles;
            ViewData["Title"] = "RoleManagement";
            return View("RoleManagement");
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
                return View("Error");
            }
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadCreateRoleView(UserInfo login)
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

        [Authorize]
        public async Task<IActionResult> LoadKidsServiceManagementView()
        {
            ViewData["services"] = await _kidsServiceRepo.GetServices("kid");
            ViewData["Title"] = "KidsServiceManagement";

            return View("KidsServiceManagement");
        }

        [Authorize]
        public async Task<IActionResult> DeleteKidsService(string id)
        {
            await _kidsServiceRepo.DeleteService(id);
            return await LoadAdultServiceManagementView();

            return View("Error", "Could not delete service");
        }

        [Authorize]
        public async Task<IActionResult> LoadCreateKidsServiceView(UserInfo login)
        {
            ViewData["Title"] = "CreateKidsService";

            return View("CreateKidsServiceView");
        }

        [Authorize]
        public async Task<IActionResult> AddKidService(DateTime date, string summary, string speaker)
        {
            var service = new ServiceCosmos() {
                Type ="kid",
                Date = date,
                Summary = summary,
                Speaker = speaker,
                Id = Guid.NewGuid().ToString() };

            await _kidsServiceRepo.AddService(service);
            return await LoadAdultServiceManagementView();

            return View("Error", "Could not add service");
        }

        [Authorize]
        public async Task<IActionResult> LoadAdultServiceManagementView()
        {
            ViewData["services"] = await _adultServiceRepo.GetServices("adult");
            ViewData["Title"] = "AdultServiceManagement";

            return View("AdultServiceManagement");
        }

        [Authorize]
        public async Task<IActionResult> DeleteAdultService(string id)
        {
            await _adultServiceRepo.DeleteService(id);

            return await LoadAdultServiceManagementView();
            return View("Error", "Could not delete service");
        }

        [Authorize]
        public async Task<IActionResult> LoadCreateAdultServiceView(UserInfo login)
        {
            ViewData["Title"] = "CreateAdultService";

            return View("CreateAdultServiceView");
        }

        [Authorize]
        public async Task<IActionResult> AddAdultService(DateTime date, string summary, string speaker)
        {
            var service = new ServiceCosmos()
            {
                Type = "adult",
                Date = date,
                Summary = summary,
                Speaker = speaker,
                Id = Guid.NewGuid().ToString()
            };

            await _kidsServiceRepo.AddService(service);

            return await LoadAdultServiceManagementView();
            return View("Error", "Could not add service");
        }

        public async Task<IActionResult> LoadWelcomeView()
        {
            ViewData["Title"] = "Welcome";
            return View("Welcome");
        }

        public async Task<IActionResult> LoadAboutView()
        {
            ViewData["Title"] = "About";
            return View("About");
        }
        public async Task<IActionResult> LoadHistoryView()
        {
            ViewData["Title"] = "Church History";
            return View("History");
        }

        public async Task<IActionResult> LoadMeetTheTeamView()
        {
            ViewData["Title"] = "Meet The Team";
            return View("MeetTheTeam");
        }

        public async Task<IActionResult> LoadServicesView()
        {
            ViewData["Title"] = "Services";
            return View("Services");
        }

        public async Task<IActionResult> LoadChildServicesView()
        {
            ViewData["Title"] = "Kids Services";
            ViewData["services"] = await _kidsServiceRepo.GetServices("kid");
            return View("KidServices");
        }

        public async Task<IActionResult> LoadAdultServicesView()
        {
            ViewData["Title"] = "Adult Services";
            ViewData["services"] = await _adultServiceRepo.GetServices("adult");
            return View("AdultServices");
        }

        public async Task<IActionResult> LoadLocalCommunityView()
        {
            ViewData["Title"] = "Local Community";
            return View("LocalCommunity");
        }

        public async Task<IActionResult> LoadWiderCommunityView()
        {
            ViewData["Title"] = "Wider Community";
            return View("WiderCommunity");
        }

        public async Task<IActionResult> LoadAtHomeView()
        {
            ViewData["Title"] = "At Home Wider Community";
            return View("WiderCommunity");
        }

        public async Task<IActionResult> LoadOverseasView()
        {
            ViewData["Title"] = "Overseas Wider Community";
            return View("WiderCommunity");
        }

        public async Task<IActionResult> LoadLinksView()
        {
            ViewData["Title"] = "Links";
            return View("Links");
        }

        public async Task<IActionResult> LoadContactUsView()
        {
            ViewData["Title"] = "Contacts US";
            return View("ContactUs");
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
    }
}