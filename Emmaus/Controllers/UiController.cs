using Emmaus.Logger;
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
        private readonly IIdentityRepo _identityRepo;
        private readonly IServiceRepo _serviceRepo;
        private readonly IRotaService _rotaService;

        public RoleManager<ApplicationUser> RoleManager { get; }

        public UiController(UserManager<ApplicationUser> userManager,
                            RoleManager<IdentityRole> roleManager,
                            SignInManager<ApplicationUser> signInManager,
                            IServiceRepo serviceRepo, IRotaService rotaService)
        {
            _identityRepo = new IdentityRepo(userManager, roleManager, signInManager);
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
            try
            {
                ViewBag.Login = login;
                await _identityRepo.SignInAsync(login.EmailAddress, login.Password);
                var message = $"User logged in: {login.EmailAddress}";
                await AzureLogging.CreateLog(message, login.EmailAddress, LogLevel.Information);
                return RedirectToAction("LoadAboutView");
            }
            catch (Exception e)
            {
                var message = $"{HttpContext.Request.Path}: " + ExceptionHelper.GetaAllMessages(e);
                await AzureLogging.CreateLog(message, login.EmailAddress, LogLevel.Warning);
                ViewData["Message"] = "Username or password incorrect";
                return await LoadLoginView();
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            ViewData["Title"] = "Logged Out";
            await _identityRepo.LogoutAsync();
            return Redirect("~/ ");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadUserManagementView()
        {
            //try
            //{
                ViewData["Title"] = "UserManagement";
                ViewData["users"] = (await _identityRepo.GetUsersAsync());
                return View("UserManagement");
            //}
            //catch (Exception)
            //{
            //    return LoadError("Could not retrieve users.");
            //}
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            //try
            //{
                await _identityRepo.DeleteUserAsync(email);
                return await LoadUserManagementView();
            //}
            //catch (Exception)
            //{
            //    return LoadError("Could not delete user");
            //}
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
            //try
            //{
                await _identityRepo.CreateUserAsync(login.EmailAddress, login.Password);
                await _identityRepo.AddRolesToUserAsync(login.EmailAddress, login.Roles);
                return RedirectToAction(nameof(LoadUserManagementView));
            //}
            //catch (Exception)
            //{
            //    return LoadError("User not created");
            //}
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> LoadRoleManagementView()
        {
            IEnumerable<string> roles = _identityRepo.GetRoles();
            ViewData["Roles"] = roles.ToAsyncEnumerable();
            ViewData["Title"] = "RoleManagement";
            return View("RoleManagement");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            //try
            //{
                await _identityRepo.DeleteRoleAsync(roleName);
                return await LoadRoleManagementView();
            //}
            //catch (Exception)
            //{
            //    return LoadError("Role deletion error.");
            //}
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
            //try
            //{
                await _identityRepo.CreateRoleAsync(roleInfo.Rolename);
                return RedirectToAction(nameof(LoadRoleManagementView));
            //}
            //catch (Exception)
            //{
            //    return LoadError("Role not created.");
            //}
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
            //try
            //{
                await _serviceRepo.DeleteService(id);
                return await LoadKidServiceManagementView();
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> DeleteAdultService(string id)
        {
            //try
            //{
                await _serviceRepo.DeleteService(id);
                return await LoadAdultServiceManagementView();
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> EditService(DateTime dateTime, string story, string text, string speaker, string id)
        {
            //try
            //{
                var service = new Models.Service() { Date = dateTime, Story = story, Text = text, Speaker = speaker, Id = id };
                await _serviceRepo.UpdateService(service);
                ViewData["Message"] = $"Service by {speaker} has been updated";
                return HttpContext.Request.Headers["Referer"].ToString().Split('/').Last() == "LoadKidServiceManagementView" ? await LoadKidServiceManagementView() : await LoadAdultServiceManagementView();
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
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
            //try
            //{
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
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
        }

        [Authorize(Roles = "admin, services")]
        public async Task<IActionResult> AddAdultService(DateTime dateTime, string story, string text, string speaker)
        {
            //try
            //{
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
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
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
            ViewData["names"] = Enum.GetNames(typeof(YouthClubLeader)).OrderBy(n => n).ToAsyncEnumerable().ToEnumerable();
            ViewData["Title"] = "Youth Rota";
            return View("RotaManagement");
        }

        [Authorize(Roles = "admin, projector, youth, band, services")]
        public async Task<IActionResult> LoadBandRotaView()
        {
            RotaDictionary rotas = await _rotaService.GetRota(typeof(BandLeader));
            ViewData["rota"] = rotas;
            ViewData["names"] = Enum.GetNames(typeof(BandLeader)).OrderBy(n => n).ToAsyncEnumerable().ToEnumerable();
            ViewData["Title"] = "Band Rota";
            return View("RotaManagement");
        }

        [Authorize(Roles = "admin, projector, youth, band, services")]
        public async Task<IActionResult> LoadProjectionRotaView()
        {
            RotaDictionary rotas = await _rotaService.GetRota(typeof(ProjectionLeader));
            ViewData["rota"] = rotas;
            ViewData["names"] = Enum.GetNames(typeof(ProjectionLeader)).OrderBy(n => n).ToAsyncEnumerable().ToEnumerable();
            ViewData["Title"] = "Projection Rota";
            return View("RotaManagement");
        }

        [Authorize(Roles = "admin, youth ")]
        public async Task<IActionResult> AddYouthClubRota(string dateTime, string name, List<string> roles)
        {
            //try
            //{
                if (string.IsNullOrEmpty(dateTime) || !roles.Any())
                {
                    throw new Exception("No date or roles added to youth clun role.");
                }

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
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}

        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> AddBandRota(string dateTime, string name, List<string> roles)
        {
            //try
            //{
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
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> AddProjectionRota(string dateTime, string name, List<string> roles)
        {
            //try
            //{
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
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> DeleteFromRotaYouthClub(string dateTime, string name, string role)
        {
            //try
            //{
                var rota = new RotaItemDto()
                {
                    Type = typeof(YouthClubLeader).Name,
                    DateTime = DateTime.Parse(dateTime),
                    Name = name,
                    Role = role
                };

                await _rotaService.DeleteFromRota(rota);
                return await LoadYouthRotaView();
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> DeleteFromRotaProjection(string dateTime, string name, string role)
        {
            //try
            //{
                var rota = new RotaItemDto()
                {
                    Type = typeof(ProjectionLeader).Name,
                    DateTime = DateTime.Parse(dateTime),
                    Name = name,
                    Role = role
                };

                await _rotaService.DeleteFromRota(rota);

                return await LoadProjectionRotaView();
            //}
            //catch (Exception)
            //{
            //    return LoadError("Something went wrong.");
            //}
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

        [Authorize()]
        public async Task<IActionResult> LoadUserProfileView(string name)
        {
            ViewData["Title"] = "User Profile";
            var rotaItems= await _rotaService.GetRotaItemsForPerson(name);
            ViewData["Rota"] = rotaItems;
            ViewData["Name"] = name;
            return View("UserProfile");
        }

        [Authorize()]
        public async Task<IActionResult> LoadUserProfilesView()
        {
            ViewData["Title"] = "User Profiles";
            return View("UserProfiles");
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

        public IActionResult LoadError(string errorMessage = null)
        {
            ViewData["Title"] = "Error";
            ViewData["Message"] = errorMessage;
            return View("Error");
        }
    }
}