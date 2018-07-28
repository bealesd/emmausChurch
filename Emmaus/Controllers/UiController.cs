using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Emmaus.Models;
using Emmaus.Repos;

namespace Emmaus.Controllers
{
    public class UiController : Controller
    {


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

            ViewData["Title"] = "Child Services";
            ViewData["services"] = childService;


            return View("ChildServices");
        }

        public IActionResult LoadAdultServicesView()
        {
            var serviceFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}data/adultService.csv";
            var adultService = ServiceProvider.ReadServices(serviceFilePath);

            ViewData["Title"] = "Adult Services";
            ViewData["services"] = adultService;

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
