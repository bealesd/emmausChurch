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
            IEnumerable<RotaItemDto> rota = await _rotaRepo.GetRota(typeofRotaEnum);
            IOrderedEnumerable<DateTime> rotaDates = rota.Select(r => r.DateTime).Distinct()
                                     .OrderBy(d => d);

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
    }
}