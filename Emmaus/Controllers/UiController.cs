using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Emmaus.Models;
using Emmaus.Repos;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Emmaus.Controllers
{
    public class UiController : Controller
    {
        private SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;

        public UiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }
        public IActionResult LoadLoginView()
        {
            ViewData["Title"] = "Login";
            return View("Login");
        }

        public async Task<IActionResult> CreateUser()
        {
            ViewData["Title"] = "Creating User";

            var user = new ApplicationUser { UserName = "adavid", Email="david_beales@ymail.com" };
            var result = await _userManager.CreateAsync(user, "Password1234!");
            if (result.Succeeded)
            {
                //_logger.LogInformation("User created a new account with password.");

                await _signInManager.SignInAsync(user, isPersistent: false);
                //_logger.LogInformation("User created a new account with password.");
                return RedirectToAction(nameof(LoadLoginView));
            }
            return View("Error");
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel login)
        {
            ViewData["Title"] = "Logged In";
            ViewBag.Login = login;

            var user = new IdentityUser { UserName = login.Username, Email = login.Password };

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, 
            // set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(login.Username,
                login.Password, false, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                //_logger.LogInformation("User logged in.");
                return RedirectToPage("LoggedIn");
            }
            if (result.IsLockedOut)
            {
                //_logger.LogWarning("User account locked out.");
                return RedirectToPage("Error");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return LoadLoginView();
            }
        }

        public async Task<IActionResult> Logout(LoginModel login)
        {
            ViewData["Title"] = "Logged Out";

            await _signInManager.SignOutAsync();
            //_logger.LogInformation("User logged out.");
            return LoadLoginView();
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
            var serviceFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}data/childrenService.csv";
            var childService = ServiceProvider.ReadServices(serviceFilePath);

            ViewData["Title"] = "Child Programme";
            ViewData["services"] = childService;


            return View("ChildServices");
        }

        public IActionResult LoadAdultServicesView()
        {
            var serviceFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}data/adultService.csv";
            var adultService = ServiceProvider.ReadServices(serviceFilePath);

            ViewData["Title"] = "Adult Programme";
            ViewData["services"] = adultService;

            return View("AdultServices");
        }

        public IActionResult LoadWelcomeView()
        {
            ViewData["Title"] = "Welcome";

            return View("Welcome");
        }

        [Authorize]
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
