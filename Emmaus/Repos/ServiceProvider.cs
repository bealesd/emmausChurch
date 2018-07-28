using System;
using System.Collections.Generic;
using System.IO;
using Emmaus.Models;

namespace Emmaus.Repos
{
    public static class ServiceProvider
    {
        public static List<Service> ReadServices(string filePath)
        {
            var services = new List<Service>();

            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    services.Add(new Service { Date = values[0], Summary = values[1], Speaker = values[2] });
                }
            }
            return services;
        }
    }
}