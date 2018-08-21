using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emmaus.Models;

namespace Emmaus.Repos
{
    public interface IServiceRepo
    {
        List<Service> GetServices();
        void DeleteService(Service service);
        void AddService(Service service);
    }

    public interface IServiceRepoFactory
    {
        IServiceRepo CreateServiceRepo(string filePath);
    }

    public class ServiceRepoFactory : IServiceRepoFactory
    {
        public IServiceRepo CreateServiceRepo(string filePath)
        {
            return new ServiceRepo(filePath);
        }
    }

    public class ServiceRepo: IServiceRepo
    {
        private List<Service> _services = new List<Service>() { };
        private readonly string _filePath;

        public ServiceRepo(string filepath)
        {
            _filePath = filepath;
        }
 

        private void ClearCache()
        {
            _services.Clear();
        }

        public List<Service> GetServices()
        {
            Load();
            return _services;
        }

        public void Load()
        {
            using (var reader = new StreamReader(_filePath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    if (values.Length == 3)
                    {
                        _services.Add(new Service { Date = values[0], Summary = values[1], Speaker = values[2] });
                    }
                }
            }
        }

        public void Save()
        {
            using (var reader = new StreamWriter(_filePath, false))
            {
                foreach (var service in _services)
                {
                    var csvService = string.Join(',', new List<string>() { service.Date, service.Speaker, service.Summary });
                    reader.WriteLine(csvService + "\n");
                }
            }
            ClearCache();
        }

        public void DeleteService(Service service)
        {
            Load();
            var serviceToRemove = _services.FirstOrDefault(s => s.Date == service.Date && s.Summary == service.Summary && s.Speaker == service.Speaker);
            var serviceRemoved = _services.Remove(serviceToRemove);
            if (!serviceRemoved)
            {
                throw new Exception("Service not removed");
            }
            Save();
        }

        public void AddService(Service service)
        {
            Load();
            _services.Add(service);
            Save();
        }
    }
}