using Emmaus.Models;
using System;
using System.Collections.Generic;

namespace Emmaus.Helper
{
    public static class ServiceListHelper
    {
        public static string Title(List<Service> services, string pageTitle)
        {
            string date = services[0].Date.ReplaceWhitespaceWithNbsp();
            string dateEnd = services[services.Count - 1].Date.ReplaceWhitespaceWithNbsp();
            pageTitle = pageTitle.ReplaceWhitespaceWithNbsp();

            return String.Concat(pageTitle, Helper.Nbsp, Helper.Nbsp, date, Helper.Nbsp, "-", Helper.Nbsp, dateEnd, Helper.Nbsp, DateTime.Now.Year);
        }
    }
}