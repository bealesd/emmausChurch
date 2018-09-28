using Emmaus.Helper;
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
        Task<RotaDictionary> GetRota(Type typeofEnum);
        Task AddRota(RotaItemDto rota);
        Task DeleteFromRota(RotaItemDto rota);
    }

    public class RotaService : IRotaService
    {
        private readonly IRotaRepo _rotaRepo;

        public RotaService(IRotaRepo rotaRepo)
        {
            _rotaRepo = rotaRepo;
            _rotaRepo.DeleteInvalidNamesFromRota(typeof(YouthClubLeader)).Wait();
            _rotaRepo.DeleteInvalidNamesFromRota(typeof(BandLeader)).Wait();
            _rotaRepo.DeleteInvalidNamesFromRota(typeof(ProjectionLeader)).Wait();
        }

        public async Task AddRota(RotaItemDto rotaItem)
        {
            await _rotaRepo.AddRotaItem(rotaItem);
        }

        public async Task DeleteFromRota(RotaItemDto rotaItem)
        {
            await _rotaRepo.DeleteRotaItemFromRota(rotaItem);
        }

        public async Task<RotaDictionary> GetRota(Type typeofRotaEnum)
        {

            var names = Enum.GetNames(typeofRotaEnum);
            List<RotaItemDto> rota = await _rotaRepo.GetRota(typeofRotaEnum.Name);

            IOrderedEnumerable<Date> rotaDates = rota.Select(r => r.Date).ToList()
                                     .DistinctBy(r => new { r.Year, r.Month, r.Day }).ToList()
                                     .OrderBy(d => d.Year).ThenBy(d => d.Month).ThenBy(d => d.Day);

            var rotaJobsDictionary = new RotaDictionary();
            foreach (Date rotaDate in rotaDates)
            {
                var nameJobs = new NameRoles();
                foreach (var name in names)
                {
                    var jobs = new List<string>();
                    var userRota = rota.Where(r => r.Date.Equals(rotaDate) && r.Name == name).ToList();
                    userRota.ForEach(r => jobs.Add(r.Role));
                    if (userRota.Count == 0) jobs.Add("--");

                    nameJobs.KeyValues.Add(name, jobs);
                }
                rotaJobsDictionary.DateNameJobListPairs.Add(rotaDate, nameJobs);
            }
            return rotaJobsDictionary;
        }
    }
}