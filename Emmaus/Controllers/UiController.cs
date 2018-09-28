using Emmaus.Models;
using Emmaus.Repos;
using Emmaus.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmaus.Controllers
{
    public class UiController : Controller
    {
        private SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private IServiceRepo _serviceRepo;
        private readonly IRotaService _rotaService;

        public RoleManager<ApplicationUser> RoleManager { get; }

        public UiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, IServiceRepo serviceRepo, IRotaService rotaService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _serviceRepo = serviceRepo;
            _rotaService = rotaService;
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

            Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(login.EmailAddress, login.Password, false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                return RedirectToAction("LoadAboutView");
            }
            if (result.IsLockedOut)
            {
                ViewData["Message"] = "Account locked out";
                return await LoadLoginView();
            }
            else
            {
                ViewData["Message"] = "Username or password incorrect";
                return await LoadLoginView();
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
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

            foreach (ApplicationUser user in users)
            {
                ApplicationUser applicationUser = await _userManager.FindByEmailAsync(user.Email);
                var roles = (await _signInManager.UserManager.GetRolesAsync(applicationUser)).OrderBy(r => r).ToList();
                userInfos.Add(new UserInfo() { EmailAddress = user.Email, Roles = roles });
            }

            ViewData["Title"] = "UserManagement";
            ViewData["users"] = userInfos.OrderBy(u => u.EmailAddress).ToList();
            return View("UserManagement");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(email);
            IdentityResult deletedResult = await _userManager.DeleteAsync(user);
            if (deletedResult.Succeeded)
            {
                return await LoadUserManagementView();
            }
            return View("Error", "Could not delete user");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadCreateUserView()
        {
            ViewData["Title"] = "CreateUser";

            return View("CreateUserView");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateUser(UserInfo login)
        {
            ViewData["Title"] = "User Management";

            var user = new ApplicationUser { UserName = login.EmailAddress, Email = login.EmailAddress };
            IdentityResult result = await _userManager.CreateAsync(user, login.Password);
            if (result.Succeeded)
            {
                foreach (var role in login.Roles)
                {
                    IdentityResult addToRoleResult = await _userManager.AddToRoleAsync(user, role);
                }

                return RedirectToAction(nameof(LoadUserManagementView));
            }

            return View("Error", "User not created");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadRoleManagementView()
        {
            var roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();

            ViewData["Roles"] = roles;
            ViewData["Title"] = "RoleManagement";
            return View("RoleManagement");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            IdentityRole role = _roleManager.Roles.First(r => r.Name == roleName) ?? null;
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
        public async Task<IActionResult> LoadCreateRoleView()
        {
            ViewData["Title"] = "CreateRole";
            return View("CreateRoleView");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateRole(RoleInfo roleInfo)
        {
            ViewData["Title"] = "Role Management";
            var roleExists = await _roleManager.RoleExistsAsync(roleInfo.Rolename);
            if (!roleExists)
            {
                var role = new IdentityRole();
                role.Name = roleInfo.Rolename;
                await _roleManager.CreateAsync(role);
                return RedirectToAction(nameof(LoadRoleManagementView));
            }

            return View("Error", "Role Already Exists");
        }

        [Authorize(Roles = "admin,projector,youth,band,services")]
        public async Task<IActionResult> LoadKidServiceManagementView()
        {
            ViewData["services"] = await _serviceRepo.GetServices("kid");
            ViewData["Title"] = "KidServiceManagement";
            return View("ServiceManagement");
        }

        [Authorize(Roles = "admin,projector,youth,band,services")]
        public async Task<IActionResult> LoadAdultServiceManagementView()
        {
            ViewData["services"] = await _serviceRepo.GetServices("adult");
            ViewData["Title"] = "AdultServiceManagement";

            return View("ServiceManagement");
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> DeleteKidService(string id)
        {
            await _serviceRepo.DeleteService(id);
            return await LoadKidServiceManagementView();
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> DeleteAdultService(string id)
        {
            await _serviceRepo.DeleteService(id);
            return await LoadAdultServiceManagementView();
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> EditService(DateTime dateTime, string story, string text, string speaker, string id)
        {
            var service = new Models.Service() { Date = dateTime, Story = story, Text = text, Speaker = speaker, Id = id };
            await _serviceRepo.UpdateService(service);
            ViewData["Message"] = $"Service by {speaker} has been updated";
            return HttpContext.Request.Headers["Referer"].ToString().Split('/').Last() == "LoadKidServiceManagementView" ? await LoadKidServiceManagementView() : await LoadAdultServiceManagementView();
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> LoadCreateKidServiceView()
        {
            ViewData["Title"] = "CreateKidService";

            return View("CreateKidServiceView");
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> LoadCreateAdultServiceView()
        {
            ViewData["Title"] = "CreateAdultService";

            return View("CreateAdultServiceView");
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> AddKidService(DateTime dateTime, string story, string text, string speaker)
        {
            var date = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 12, 12, 12);
            var service = new Models.Service()
            {
                Type = "kid",
                Date = date,
                Story = story,
                Text = text,
                Speaker = speaker,
                Id = Guid.NewGuid().ToString()
            };

            await _serviceRepo.AddService(service);
            return await LoadKidServiceManagementView();
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> AddAdultService(DateTime dateTime, string story, string text, string speaker)
        {
            var date = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 12, 12, 12);
            var service = new Models.Service()
            {
                Type = "adult",
                Date = date,
                Story = story,
                Text = text,
                Speaker = speaker,
                Id = Guid.NewGuid().ToString()
            };

            await _serviceRepo.AddService(service);
            return await LoadAdultServiceManagementView();
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> LoadCreateYouthView()
        {
            ViewData["Title"] = "Create Youth Event";
            return View("CreateEventView");
        }


        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> LoadCreateBandView()
        {
            ViewData["Title"] = "Create Band Event";
            return View("CreateEventView");
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> LoadCreateProjectionView()
        {
            ViewData["Title"] = "Create Projection Event";
            return View("CreateEventView");
        }


        [Authorize(Roles = "admin, projector, youth, band, services")]
        public async Task<IActionResult> LoadYouthRotaView()
        {
            RotaDictionary rota = await _rotaService.GetRota(typeof(YouthClubLeader));
            ViewData["rota"] = rota;
            ViewData["names"] = Enum.GetNames(typeof(YouthClubLeader)).OrderBy(n => n).ToList();
            ViewData["Title"] = "Youth Rota";
            return View("RotaManagement");
        }

        [Authorize(Roles = "admin, projector, youth, band, services")]
        public async Task<IActionResult> LoadBandRotaView()
        {
            RotaDictionary rotas = await _rotaService.GetRota(typeof(BandLeader));
            ViewData["rota"] = rotas;
            ViewData["names"] = Enum.GetNames(typeof(BandLeader)).OrderBy(n => n).ToList();
            ViewData["Title"] = "Band Rota";
            return View("RotaManagement");
        }

        [Authorize(Roles = "admin, projector, youth, band, services")]
        public async Task<IActionResult> LoadProjectionRotaView()
        {
            RotaDictionary rotas = await _rotaService.GetRota(typeof(ProjectionLeader));
            ViewData["rota"] = rotas;
            ViewData["names"] = Enum.GetNames(typeof(ProjectionLeader)).OrderBy(n => n).ToList();
            ViewData["Title"] = "Projection Rota";
            return View("RotaManagement");
        }

        [Authorize(Roles = "admin, youth ")]
        public async Task<IActionResult> AddYouthClubRota(string dateTime, string name, List<string> roles)
        {
            foreach (var role in roles)
            {
                var rota = new RotaItemDto()
                {
                    Type = typeof(YouthClubLeader).Name,
                    DateTime = DateTime.Parse(dateTime),
                    Name = name,
                    Role = role,
                    Id = Guid.NewGuid().ToString()
                };
                await _rotaService.AddRota(rota);
            }
            return await LoadYouthRotaView();
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> AddBandRota(string dateTime, string name, List<string> roles)
        {
            foreach (var role in roles)
            {
                var rota = new RotaItemDto()
                {
                    Type = typeof(BandLeader).Name,
                    DateTime = DateTime.Parse(dateTime),
                    Name = name,
                    Role = role,
                    Id = Guid.NewGuid().ToString()
                };
                await _rotaService.AddRota(rota);
            }

            return await LoadBandRotaView();
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> AddProjectionRota(string dateTime, string name, List<string> roles)
        {
            foreach (var role in roles)
            {
                var rota = new RotaItemDto()
                {
                    Type = typeof(ProjectionLeader).Name,
                    DateTime = DateTime.Parse(dateTime),
                    Name = name,
                    Role = role,
                    Id = Guid.NewGuid().ToString()
                };
                await _rotaService.AddRota(rota);
            }

            return await LoadProjectionRotaView();
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> DeleteFromRotaYouthClub(string dateTime, string name, string role)
        {
            var rota = new RotaItemDto()
            {
                Type = typeof(YouthClubLeader).Name,
                DateTime = DateTime.Parse(dateTime),
                Name = name,
                Role = role
            };

            await _rotaService.DeleteFromRota(rota);

            return await LoadYouthRotaView();
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> DeleteFromRotaProjection(string dateTime, string name, string role)
        {
            var rota = new RotaItemDto()
            {
                Type = typeof(ProjectionLeader).Name,
                DateTime = DateTime.Parse(dateTime),
                Name = name,
                Role = role
            };

            await _rotaService.DeleteFromRota(rota);

            return await LoadProjectionRotaView();
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> DeleteFromRotaBand(string dateTime, string name, string role)
        {
            var rota = new RotaItemDto()
            {
                Type = typeof(BandLeader).Name,
                DateTime = DateTime.Parse(dateTime),
                Name = name,
                Role = role
            };

            await _rotaService.DeleteFromRota(rota);

            return await LoadBandRotaView();
        }

        public async Task<IActionResult> LoadWelcomeView()
        {
            ViewData["Title"] = "Welcome";
            return View("Welcome");
        }

        public async Task<IActionResult> LoadEventsView()
        {
            ViewData["Title"] = "Event";
            return View("Events");
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
            return View("ServicesInformation");
        }

        public async Task<IActionResult> LoadAdultServicesView()
        {
            ViewData["Title"] = "Adult Services";
            ViewData["services"] = await _serviceRepo.GetServices("adult");
            return View("ServicesInformation");
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
                ApplicationUser user = await _userManager.FindByEmailAsync(email);
                return (await _signInManager.UserManager.GetRolesAsync(user))[0];
            }
            else
            {
                var username = _userManager.GetUserName(HttpContext.User);
                if (username == null)
                {
                    return null;
                }
                ApplicationUser appUser = await _userManager.FindByEmailAsync(username);
                return (await _signInManager.UserManager.GetRolesAsync(appUser))[0];
            }
        }
    }
}