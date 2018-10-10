using Emmaus.Models;
using Emmaus.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmaus.Service
{
    public interface IRotaService
    {
        Task<RotaDictionary> GetRotaJobs(string rotaType);
        Task<Dictionary<DateTime, Dictionary<string, List<string>>>> GetRotaJobsForPerson(string name);
        Task AddRotaJobs(RotaItemDto rota);
        Task DeleteJobsFromRota(RotaItemDto rota);

        Task<IEnumerable<string>> GetNames();
        Task<IEnumerable<string>> GetNamesOnRota(string rotaType);
        Task AddNameToRota(string name, string rotaType);
        Task DeleteNameFromRota(string name, string rotaType);

        Task<IEnumerable<string>> GetJobs();
        Task<IEnumerable<string>> GetJobsOnRota(string rotaType);
        Task AddJobToRota(string name, string rotaType);
        Task DeleteJobFromRota(string name, string rotaType);
    }

    public class RotaService : IRotaService
    {
        private readonly IRotaRepo _rotaRepo;
        private readonly IRotaNamesRepo _rotaNamesRepo;
        private readonly IRotaJobsRepo _rotaJobRepo;

        public RotaService(IRotaRepo rotaRepo, IRotaNamesRepo rotaNamesRepo, IRotaJobsRepo rotaJobsRepo)
        {
            _rotaRepo = rotaRepo;
            _rotaNamesRepo = rotaNamesRepo;
            _rotaJobRepo = rotaJobsRepo;
            DeleteInvalidNamesFromRotaJobs().Wait();
        }

        private async Task DeleteInvalidNamesFromRotaJobs()
        {
            await DeleteInvalidNamesFromRota(RotaType.YouthClub.ToString());
            await DeleteInvalidNamesFromRota(RotaType.YouthClub.ToString());
            await DeleteInvalidNamesFromRota(RotaType.YouthClub.ToString());
        }

        private async Task DeleteInvalidNamesFromRota(string rotaType)
        {
            IEnumerable<string> names = await _rotaNamesRepo.GetNames(rotaType);
            await _rotaRepo.DeleteInvalidNamesFromRota(rotaType, names);
        }

        public async Task AddRotaJobs(RotaItemDto rotaItem)
        {
            if (string.IsNullOrEmpty(rotaItem.Name) || string.IsNullOrEmpty(rotaItem.Role))
                throw new Exception("Could not add rota item as name or role is null or empty.");

            await _rotaRepo.AddRotaItem(rotaItem);
        }

        public async Task DeleteJobsFromRota(RotaItemDto rotaItem)
        {
            if (string.IsNullOrEmpty(rotaItem.Name) || string.IsNullOrEmpty(rotaItem.Role))
                throw new Exception("Could not delete rota item as name or role is null or empty.");
            await _rotaRepo.DeleteRotaItemFromRota(rotaItem);
        }

        public async Task<Dictionary<DateTime, Dictionary<string, List<string>>>> GetRotaJobsForPerson(string name)
        {
            name = name.Trim();
            IEnumerable<RotaItemDto> rota = await _rotaRepo.GetRotaItemsForPerson(name);
            IOrderedEnumerable<DateTime> rotaDates = rota.Select(r => r.DateTime).Distinct().OrderBy(d => d);
            var dateDictionary = new Dictionary<DateTime, Dictionary<string, List<string>>>();
            foreach (DateTime rotaDate in rotaDates)
            {
                var typeDictionary = new Dictionary<string, List<string>>();
                var rotaItems = rota.Where(r => r.DateTime == rotaDate).ToList();
                foreach (RotaItemDto rotaItem in rotaItems)
                {
                    if (!typeDictionary.ContainsKey(rotaItem.Type))
                    {
                        typeDictionary[rotaItem.Type] = new List<string>();
                    }
                    typeDictionary[rotaItem.Type].Add(rotaItem.Role);
                }
                dateDictionary[rotaDate] = typeDictionary;
            }

            return dateDictionary;
        }

        public async Task<RotaDictionary> GetRotaJobs(string rotaType)
        {
            IEnumerable<string> names = await GetNamesOnRota(rotaType);
            IEnumerable<RotaItemDto> rota = await _rotaRepo.GetSoughtedRota(rotaType);
            IOrderedEnumerable<DateTime> rotaDates = rota.Select(r => r.DateTime).Distinct().OrderBy(d => d);

            var rotaJobsDictionary = new RotaDictionary();
            foreach (DateTime rotaDate in rotaDates)
            {
                var nameRoles = new NameRoles();
                foreach (var name in names)
                {
                    var roles = new List<string>();
                    var userRota = rota.Where(r => r.DateTime == rotaDate && r.Name == name).ToList();
                    userRota.ForEach(r => roles.Add(r.Role));
                    if (userRota.Count == 0) roles.Add("--");

                    nameRoles.KeyValues.Add(name, roles);
                }
                rotaJobsDictionary.DateNameJobListPairs.Add(rotaDate, nameRoles);
            }
            return rotaJobsDictionary;
        }

        public async Task<IEnumerable<string>> GetNames() => await _rotaNamesRepo.GetNames();

        public async Task<IEnumerable<string>> GetNamesOnRota(string rotaType) => await _rotaNamesRepo.GetNames(rotaType);

        public async Task AddNameToRota(string name, string rotaType)
        {
            name = name.Trim();
            await _rotaNamesRepo.AddName(name, rotaType);
        }

        public async Task DeleteNameFromRota(string name, string rotaType)
        {
            name = name.Trim();
            await _rotaNamesRepo.DeleteName(name, rotaType);
            await DeleteInvalidNamesFromRota(rotaType);
        }

        public async Task<IEnumerable<string>> GetJobs() => await _rotaJobRepo.GetJobs();

        public async Task<IEnumerable<string>> GetJobsOnRota(string rotaType) => await _rotaJobRepo.GetJobs(rotaType);

        public async Task AddJobToRota(string name, string rotaType)
        {
            name = name.Trim();
            await _rotaJobRepo.AddJob(name, rotaType);
        }

        public async Task DeleteJobFromRota(string name, string rotaType)
        {
            name = name.Trim();
            await _rotaJobRepo.DeleteJob(name, rotaType);
            await DeleteInvalidNamesFromRota(rotaType);
        }
    }
}