using Emmaus.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Emmaus.Helper
{
    public static class ServiceListHelper
    {
        public static readonly string Nbsp = "&nbsp;";

        public static string Title(IEnumerable<Service> services, string pageTitle)
        {
            pageTitle = pageTitle.ReplaceWhitespaceWithNbsp();

            var servicesList = services.ToList();
            if (servicesList.Count <= 1) return pageTitle;

            var monthStart = servicesList[0].Date.ToString("MMM", CultureInfo.CurrentCulture);
            var monthEnd = servicesList[servicesList.Count - 1].Date.ToString("MMM", CultureInfo.CurrentCulture);

            return String.Concat(
                pageTitle, Nbsp, 
                monthStart, Nbsp,
                "-", Nbsp,
                monthEnd, Nbsp, 
                servicesList[0].Date.Year);
        }

        public static string ReplaceWhitespaceWithNbsp(this string inputString)
        {
            string newString = string.Empty;
            foreach (var item in inputString)
            {
                if (char.IsWhiteSpace(item))
                {
                    newString += Nbsp;
                }
                else
                {
                    newString += item;
                }
            }
            return newString;
        }
    }
}