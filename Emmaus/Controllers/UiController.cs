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
            ViewData["Message"] = "About page.";

            return View("About");
        }

        public IActionResult LoadServicesView()
        {
            ViewData["Message"] = "About page.";

            return View("Services");
        }

        public IActionResult LoadWelcomeView()
        {
            ViewData["Message"] = "About page.";

            return View("Welcome");
        }

        public IActionResult LoadLinksView()
        {
            return View("Links");
        }

        public IActionResult LoadContactUsView()
        {
            return View("ContactUs");
        }

        public IActionResult LoadLocalCommunityView()
        {
            return View("LocalCommunity");
        }

        public IActionResult LoadWiderCommunityView()
        {
            return View("WiderCommunity");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
