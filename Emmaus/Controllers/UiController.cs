using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Emmaus.Models;

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
            ViewData["Title"] = "ChildServices";

            return View("ChildServices");
        }

        public IActionResult LoadAdultServicesView()
        {
            ViewData["Title"] = "AdultServices";

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
            ViewData["Title"] = "ContactsUS";
            return View("ContactUs");
        }

        public IActionResult LoadLocalCommunityView()
        {
            ViewData["Title"] = "LocalCommunity";
            return View("LocalCommunity");
        }

        public IActionResult LoadWiderCommunityView()
        {
            ViewData["Title"] = "WiderCommunity";
            return View("WiderCommunity");
        }

        public IActionResult LoadAtHomeView()
        {
            ViewData["Title"] = "AtHomeWiderCommunity";
            return View("WiderCommunity");
        }

        public IActionResult LoadOverseasView()
        {
            ViewData["Title"] = "OverseasWiderCommunity";
            return View("WiderCommunity");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
