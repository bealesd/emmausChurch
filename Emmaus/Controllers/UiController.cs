using Emmaus.Logger;
using Emmaus.Models;
using Emmaus.Repos;
using Emmaus.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
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

        [Route("error/404")]
        public async Task<IActionResult> Error404()
        {
            if (Request.Headers["Referer"].Count > 0)
            {
                ViewData["Reffer"] = Request.Headers["Referer"].ToString();

                IStatusCodeReExecuteFeature feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
                Dictionary<string, Microsoft.Extensions.Primitives.StringValues> queryDictionary = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(feature?.OriginalQueryString);
                var requestedUrl = queryDictionary["ReturnUrl"][0].Split('/').Last();
                if (requestedUrl.Contains('?'))
                {
                    requestedUrl = requestedUrl.Split('?').First();
                }
                TempData["Message"] = $"You do not have permission to view: {requestedUrl}";
                return Redirect(ViewData["Reffer"] as string);
            }
            return await LoadWelcomeView();
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
                return RedirectToAction("LoadUserDetailsView");
            }
            catch (Exception e)
            {
                var message = $"{HttpContext.Request.Path}: " + ExceptionHelper.GetaAllMessages(e);
                await AzureLogging.CreateLog(message, login.EmailAddress, LogLevel.Warning);
                TempData["Message"] = "Username or password incorrect";
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
            ViewData["Title"] = "UserManagement";
            ViewData["users"] = (await _identityRepo.GetUsersAsync());
            return View("UserManagement");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            await _identityRepo.DeleteUserAsync(email);
            return await LoadUserManagementView();
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
            await _identityRepo.CreateUserAsync(login.EmailAddress, login.Password);
            await _identityRepo.AddRolesToUserAsync(login.EmailAddress, login.Roles);
            return RedirectToAction(nameof(LoadUserManagementView));
        }

        [Authorize(Roles = "admin,projector,youth,band,services")]
        public async Task<IActionResult> LoadUserDetailsView()
        {
            ViewData["Title"] = "User Details";
            return View("UserDetails");
        }

        [Authorize(Roles = "admin,projector,youth,band,services")]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword)
        {
            ViewData["Title"] = "User Management";
            await _identityRepo.UpdateUserPassword(HttpContext.User.Identity.Name, currentPassword, newPassword);
            TempData["Message"] = $"Password Updates";
            return RedirectToAction(nameof(LoadUserDetailsView));
        }

        [Authorize(Roles = "admin,projector,youth,band,services")]
        public async Task<IActionResult> UpdateEmail(string currentEmail, string newEmail)
        {
            ViewData["Title"] = "User Management";
            await _identityRepo.UpdateUserEmail(currentEmail, newEmail);
            TempData["Message"] = $"Email Updated";
            return RedirectToAction(nameof(LoadUserDetailsView));
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
            await _identityRepo.DeleteRoleAsync(roleName);
            return await LoadRoleManagementView();
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
            await _identityRepo.CreateRoleAsync(roleInfo.Rolename);
            return RedirectToAction(nameof(LoadRoleManagementView));
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
            TempData["Message"] = $"Service by {speaker} has been updated";

            return /*HttpContext.Request.Headers["Referer"].ToString().Split('/').Last()*/TempData["Title"] as string == "KidServiceManagement"/*"LoadKidServiceManagementView"*/ ? await LoadKidServiceManagementView() : await LoadAdultServiceManagementView();
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
            ViewData["LeftMenuConfig"] = "Youth";
            ViewData["names"] = await _rotaService.GetNamesOnRota(RotaType.YouthClub.ToString());
            ViewData["jobs"] = await _rotaService.GetJobsOnRota(RotaType.YouthClub.ToString());
            return View("CreateRotaJobView");
        }


        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> LoadCreateBandView()
        {
            ViewData["Title"] = "Create Band Event";
            ViewData["LeftMenuConfig"] = "Band";
            ViewData["names"] = await _rotaService.GetNamesOnRota(RotaType.Band.ToString());
            ViewData["jobs"] = await _rotaService.GetJobsOnRota(RotaType.Band.ToString());

            return View("CreateRotaJobView");
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> LoadCreateProjectionView()
        {
            ViewData["Title"] = "Create Projection Event";
            ViewData["LeftMenuConfig"] = "Projection";
            ViewData["names"] = await _rotaService.GetNamesOnRota(RotaType.Projection.ToString());
            ViewData["jobs"] = await _rotaService.GetJobsOnRota(RotaType.Projection.ToString());

            return View("CreateRotaJobView");
        }

        [Authorize(Roles = "admin, projector, youth, band, services")]
        public async Task<IActionResult> LoadYouthRotaView()
        {
            RotaDictionary rota = await _rotaService.GetRotaJobs(RotaType.YouthClub.ToString());
            ViewData["rota"] = rota;
            ViewData["names"] = (await _rotaService.GetNamesOnRota(RotaType.YouthClub.ToString())).ToList().OrderBy(n => n);
            ViewData["LeftMenuConfig"] = "Youth";
            ViewData["Title"] = "Youth Rota";
            return View("RotaManagement");
        }

        [Authorize(Roles = "admin, projector, youth, band, services")]
        public async Task<IActionResult> LoadBandRotaView()
        {
            RotaDictionary rotas = await _rotaService.GetRotaJobs(RotaType.Band.ToString());
            ViewData["rota"] = rotas;
            ViewData["names"] = (await _rotaService.GetNamesOnRota(RotaType.Band.ToString())).ToList().OrderBy(n => n);
            ViewData["LeftMenuConfig"] = "Band";
            ViewData["Title"] = "Band Rota";
            return View("RotaManagement");
        }

        [Authorize(Roles = "admin, projector, youth, band, services")]
        public async Task<IActionResult> LoadProjectionRotaView()
        {
            RotaDictionary rotas = await _rotaService.GetRotaJobs(RotaType.Projection.ToString());
            ViewData["names"] = (await _rotaService.GetNamesOnRota(RotaType.Projection.ToString())).ToList().OrderBy(n => n);
            ViewData["rota"] = rotas;
            ViewData["LeftMenuConfig"] = "Projection";
            ViewData["Title"] = "Projection Rota";
            return View("RotaManagement");
        }


        [Authorize(Roles = "admin, youth ")]
        public async Task<IActionResult> AddYouthRota(string dateTime, string name, List<string> roles)
        {
            if (!roles.Any()) throw new Exception("No roles added to youth role.");
            var date = DateTime.Parse(dateTime);
            foreach (var role in roles)
            {
                try
                {
                    var rota = new RotaItemDto()
                    {
                        Type = RotaType.YouthClub.ToString(),
                        DateTime = date,
                        Name = name.TrimEnd(),
                        Role = role,
                        Id = Guid.NewGuid().ToString()
                    };
                    await _rotaService.AddRotaJobs(rota);
                }
                catch (Exception e)
                {
                    if (e.Message == "Already Added")
                    {
                        TempData["Message"] = $"{name} already on rota for {role} on {date.ToShortDateString()}";
                        return await LoadYouthRotaView();
                    }
                    else
                    {
                        throw new Exception("In AddYouthRota", e);
                    }
                }
            }
            TempData["Message"] = $"{name} added to rota on {dateTime}";
            return await LoadYouthRotaView();
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> AddBandRota(string dateTime, string name, List<string> roles)
        {
            if (!roles.Any()) throw new Exception("No roles added to band role.");
            var date = DateTime.Parse(dateTime);
            foreach (var role in roles)
            {
                try
                {
                    var rota = new RotaItemDto()
                    {
                        Type = RotaType.Band.ToString(),
                        DateTime = date,
                        Name = name.TrimEnd(),
                        Role = role,
                        Id = Guid.NewGuid().ToString()
                    };
                    await _rotaService.AddRotaJobs(rota);
                }
                catch (Exception e)
                {
                    if (e.Message == "Already Added")
                    {
                        TempData["Message"] = $"{name} already on rota for {role} on {date.ToShortDateString()}";
                        return await LoadBandRotaView();
                    }
                    else
                    {
                        throw new Exception("In AddBandRota", e);
                    }
                }
            }
            TempData["Message"] = $"{name} added to rota on {dateTime}";
            return await LoadBandRotaView();
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> AddProjectionRota(string dateTime, string name, List<string> roles)
        {
            if (!roles.Any()) throw new Exception("No roles added to projection role.");
            var date = DateTime.Parse(dateTime);
            foreach (var role in roles)
            {
                try
                {
                    var rota = new RotaItemDto()
                    {
                        Type = RotaType.Projection.ToString(),
                        DateTime = date,
                        Name = name.TrimEnd(),
                        Role = role,
                        Id = Guid.NewGuid().ToString()
                    };
                    await _rotaService.AddRotaJobs(rota);
                }
                catch (Exception e)
                {
                    if (e.Message == "Already Added")
                    {
                        TempData["Message"] = $"{name} already on rota for {role} on {date.ToShortDateString()}";
                        return await LoadProjectionRotaView();
                    }
                    else
                    {
                        throw new Exception("In AddProjectionRota", e);
                    }
                }
            }
            TempData["Message"] = $"{name} added to rota on {dateTime}";
            return await LoadProjectionRotaView();
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> DeleteFromRotaYouthClub(string dateTime, string name, string role)
        {
            var date = DateTime.Parse(dateTime);
            var rota = new RotaItemDto()
            {
                Type = RotaType.YouthClub.ToString(),
                DateTime = date,
                Name = name,
                Role = role
            };

            await _rotaService.DeleteJobsFromRota(rota);
            TempData["Message"] = $"Removed {name} from job: {role}, on {date.Date}";
            return await LoadYouthRotaView();
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> DeleteFromRotaProjection(string dateTime, string name, string role)
        {
            var date = DateTime.Parse(dateTime);
            var rota = new RotaItemDto()
            {
                Type = RotaType.Projection.ToString(),
                DateTime = date,
                Name = name,
                Role = role
            };

            await _rotaService.DeleteJobsFromRota(rota);
            TempData["Message"] = $"Removed {name} from job: {role}, on {date.Date}";
            return await LoadProjectionRotaView();
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> DeleteFromRotaBand(string dateTime, string name, string role)
        {
            var date = DateTime.Parse(dateTime);
            var rota = new RotaItemDto()
            {
                Type = RotaType.Band.ToString(),
                DateTime = date,
                Name = name,
                Role = role
            };

            await _rotaService.DeleteJobsFromRota(rota);
            TempData["Message"] = $"Removed {name} from job: {role}, on {date.Date}";
            return await LoadBandRotaView();
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> LoadAddPersonToYouthView()
        {
            ViewData["Title"] = "Add Person To Youth";
            ViewData["LeftMenuConfig"] = "Youth";
            ViewData["names"] = await _rotaService.GetNamesOnRota(RotaType.YouthClub.ToString());
            return View("AddPersonView");
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> LoadAddPersonToBandView()
        {
            ViewData["Title"] = "Add Person To Band";
            ViewData["LeftMenuConfig"] = "Band";
            ViewData["names"] = await _rotaService.GetNamesOnRota(RotaType.Band.ToString());
            return View("AddPersonView");
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> LoadAddPersonToProjectionView()
        {
            ViewData["Title"] = "Add Person To Projection";
            ViewData["LeftMenuConfig"] = "Projection";
            ViewData["names"] = await _rotaService.GetNamesOnRota(RotaType.Projection.ToString());
            return View("AddPersonView");
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> AddPersonToYouth(string name)
        {
            await _rotaService.AddNameToRota(name, RotaType.YouthClub.ToString());
            TempData["Message"] = $"{name} added to youth rota";
            return await LoadAddPersonToYouthView();
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> AddPersonToBand(string name)
        {
            await _rotaService.AddNameToRota(name, RotaType.Band.ToString());
            TempData["Message"] = $"{name} added to band rota";
            return await LoadAddPersonToBandView();
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> AddPersonToProjection(string name)
        {
            await _rotaService.AddNameToRota(name, RotaType.Projection.ToString());
            TempData["Message"] = $"{name} added to projection rota";
            return await LoadAddPersonToProjectionView();
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> DeletePersonFromYouth(string name)
        {
            await _rotaService.DeleteNameFromRota(name, RotaType.YouthClub.ToString());
            TempData["Message"] = $"{name} deleted from youth rota";
            return await LoadAddPersonToYouthView();
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> DeletePersonFromBand(string name)
        {
            await _rotaService.DeleteNameFromRota(name, RotaType.Band.ToString());
            TempData["Message"] = $"{name} deleted from band rota";
            return await LoadAddPersonToBandView();
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> DeletePersonFromProjection(string name)
        {
            await _rotaService.DeleteNameFromRota(name, RotaType.Projection.ToString());
            TempData["Message"] = $"{name} deleted from projection rota";
            return await LoadAddPersonToProjectionView();
        }

        public async Task<IActionResult> LoadAddJobToYouthView()
        {
            ViewData["Title"] = "Add Job To Youth";
            ViewData["LeftMenuConfig"] = "Youth";
            ViewData["names"] = await _rotaService.GetJobsOnRota(RotaType.YouthClub.ToString());
            return View("AddJobView");
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> LoadAddJobToBandView()
        {
            ViewData["Title"] = "Add Job To Band";
            ViewData["LeftMenuConfig"] = "Band";
            ViewData["names"] = await _rotaService.GetJobsOnRota(RotaType.Band.ToString());
            return View("AddJobView");
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> LoadAddJobToProjectionView()
        {
            ViewData["Title"] = "Add Job To Projection";
            ViewData["LeftMenuConfig"] = "Projection";
            ViewData["names"] = await _rotaService.GetJobsOnRota(RotaType.Projection.ToString());
            return View("AddJobView");
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> AddJobToYouth(string name)
        {
            await _rotaService.AddJobToRota(name, RotaType.YouthClub.ToString());
            TempData["Message"] = $"{name} added to youth jobs";
            return await LoadAddJobToYouthView();
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> AddJobToBand(string name)
        {
            await _rotaService.AddJobToRota(name, RotaType.Band.ToString());
            TempData["Message"] = $"{name} added to band jobs";
            return await LoadAddJobToBandView();
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> AddJobToProjection(string name)
        {
            await _rotaService.AddJobToRota(name, RotaType.Projection.ToString());
            TempData["Message"] = $"{name} added to projection jobs";
            return await LoadAddJobToProjectionView();
        }

        [Authorize(Roles = "admin, youth")]
        public async Task<IActionResult> DeleteJobFromYouth(string name)
        {
            await _rotaService.DeleteJobFromRota(name, RotaType.YouthClub.ToString());
            TempData["Message"] = $"{name} added to youth jobs";
            return await LoadAddJobToYouthView();
        }

        [Authorize(Roles = "admin, band")]
        public async Task<IActionResult> DeleteJobFromBand(string name)
        {
            await _rotaService.DeleteJobFromRota(name, RotaType.Band.ToString());
            TempData["Message"] = $"{name} removed to band jobs";
            return await LoadAddJobToBandView();
        }

        [Authorize(Roles = "admin, projector")]
        public async Task<IActionResult> DeleteJobFromProjection(string name)
        {
            await _rotaService.DeleteJobFromRota(name, RotaType.Projection.ToString());
            TempData["Message"] = $"{name} added to projection jobs";
            return await LoadAddJobToProjectionView();
        }

        [Authorize()]
        public async Task<IActionResult> LoadUserProfileView(string name)
        {
            ViewData["Title"] = "User Profile";
            Dictionary<DateTime, Dictionary<string, List<string>>> rotaItems = await _rotaService.GetRotaJobsForPerson(name);
            ViewData["Rota"] = rotaItems;
            ViewData["Name"] = name;
            ViewData["youthJobs"] = await _rotaService.GetJobsOnRota(RotaType.YouthClub.ToString());
            ViewData["bandJobs"] = await _rotaService.GetJobsOnRota(RotaType.Band.ToString());
            ViewData["projectionJobs"] = await _rotaService.GetJobsOnRota(RotaType.Projection.ToString());
            return View("UserProfile");
        }

        [Authorize()]
        public async Task<IActionResult> LoadUserProfilesView()
        {
            ViewData["Title"] = "User Profiles";
            ViewData["Names"] = await _rotaService.GetNames();
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
            TempData["Message"] = errorMessage;
            return View("Error");
        }
    }
}