using System;
using Microsoft.AspNetCore.Mvc;
using Emmaus.Models;
using Emmaus.Repos;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using Emmaus.Helper;

namespace Emmaus.Controllers
{
    public class UiController : Controller
    {
        private SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private IServiceRepo _serviceRepo;

        public RoleManager<ApplicationUser> RoleManager { get; }

        public UiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, IServiceRepo serviceRepo)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _serviceRepo = serviceRepo;
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

        [Authorize(Roles = "powerUser")]
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

        [Authorize(Roles = "powerUser")]
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

        [Authorize(Roles = "powerUser")]
        public async Task<IActionResult> LoadCreateUserView(UserInfo login)
        {
            ViewData["Title"] = "CreateUser";

            return View("CreateUserView");
        }

        [Authorize(Roles = "powerUser")]
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

        [Authorize(Roles = "powerUser")]
        public async Task<IActionResult> LoadRoleManagementView()
        {
            List<IdentityRole> roles = _roleManager.Roles.ToList();

            ViewData["Roles"] = roles;
            ViewData["Title"] = "RoleManagement";
            return View("RoleManagement");
        }

        [Authorize(Roles = "powerUser")]
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

        [Authorize(Roles = "powerUser")]
        public async Task<IActionResult> LoadCreateRoleView(UserInfo login)
        {
            ViewData["Title"] = "CreateRole";
            return View("CreateRoleView");
        }

        [Authorize(Roles = "powerUser")]
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
        public async Task<IActionResult> LoadKidServiceManagementView()
        {
            ViewData["services"] = await _serviceRepo.GetServices("kid");
            ViewData["Title"] = "KidServiceManagement";

            return View("KidServiceManagement");
        }

        [Authorize]
        public async Task<IActionResult> DeleteKidService(string id)
        {
            await _serviceRepo.DeleteService(id);
            return await LoadKidServiceManagementView();
        }

        [Authorize]
        public async Task<IActionResult> EditKidService(DateTime dateTime, string story, string text, string speaker, string id)
        {
            var service = new Service() { Date = dateTime, Story = story, Text = text, Speaker = speaker, Id = id };
            await _serviceRepo.UpdateService(service);
            return await LoadKidServiceManagementView();
        }

        [Authorize]
        public async Task<IActionResult> EditAdultService(DateTime dateTime, string story, string text, string speaker, string id)
        {
            var service = new Service() { Date = dateTime, Story = story, Text = text, Speaker = speaker, Id = id };
            await _serviceRepo.UpdateService(service);
            return await LoadAdultServiceManagementView();
        }

        [Authorize]
        public async Task<IActionResult> LoadCreateKidServiceView(UserInfo login)
        {
            ViewData["Title"] = "CreateKidService";

            return View("CreateKidServiceView");
        }

        [Authorize]
        public async Task<IActionResult> AddKidService(DateTime dateTime, string story, string text, string speaker)
        {
            DateTime d = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 12, 12, 12);
            var service = new Service() {
                Type ="kid",
                Date = d,
                Story = story,
                Text = text,
                Speaker = speaker,
                Id = Guid.NewGuid().ToString() };

            await _serviceRepo.AddService(service);
            return await LoadKidServiceManagementView();

            return View("Error", "Could not add service");
        }

        [Authorize]
        public async Task<IActionResult> LoadAdultServiceManagementView()
        {
            ViewData["services"] = await _serviceRepo.GetServices("adult");
            ViewData["Title"] = "AdultServiceManagement";

            return View("AdultServiceManagement");
        }

        [Authorize]
        public async Task<IActionResult> DeleteAdultService(string id)
        {
            await _serviceRepo.DeleteService(id);

            return await LoadAdultServiceManagementView();
        }

        [Authorize]
        public async Task<IActionResult> LoadCreateAdultServiceView(UserInfo login)
        {
            ViewData["Title"] = "CreateAdultService";

            return View("CreateAdultServiceView");
        }

        [Authorize]
        public async Task<IActionResult> AddAdultService(DateTime dateTime, string story, string text, string speaker)
        {
            DateTime d = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 12, 12, 12);
            var service = new Service()
            {
                Type = "adult",
                Date = d,
                Story = story,
                Text = text,
                Speaker = speaker,
                Id = Guid.NewGuid().ToString()
            };

            await _serviceRepo.AddService(service);

            return await LoadAdultServiceManagementView();
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
            ViewData["services"] = await _serviceRepo.GetServices("kid");
            return View("KidServices");
        }

        public async Task<IActionResult> LoadAdultServicesView()
        {
            ViewData["Title"] = "Adult Services";
            ViewData["services"] = await _serviceRepo.GetServices("adult");
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

        public async Task<IActionResult> LoadError()
        {
            ViewData["Title"] = "Error";
            return View("Error");
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