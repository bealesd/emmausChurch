using Emmaus.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Emmaus.Helper
{
    public static class ServiceListHelper
    {
        public static string Title(List<Service> services, string pageTitle)
        {
            string dateStart = services[0].Date.Trim();
            string dateEnd = services[services.Count - 1].Date.Trim();
            pageTitle = pageTitle.ReplaceWhitespaceWithNbsp();

            var monthStart = dateStart.Split(' ')[1].GetAbbreviatedFromFullName() ?? dateStart.Split(' ')[1];
            var monthEnd = dateEnd.Split(' ')[1].GetAbbreviatedFromFullName() ?? dateEnd.Split(' ')[1];

            return String.Concat(pageTitle, Helper.Nbsp, monthStart, Helper.Nbsp, "-", Helper.Nbsp, monthEnd, Helper.Nbsp, DateTime.Now.Year);
        }

        public static string GetAbbreviatedFromFullName(this string fullname)
        {
            DateTime month;
            return DateTime.TryParseExact(
                    fullname,
                    "MMMM",
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.None,
                    out month)
                ? month.ToString("MMM")
                : null;
        }
    }
}