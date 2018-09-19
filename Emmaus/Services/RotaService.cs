﻿using Emmaus.Helper;
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
        Task<RotaDictionary> GetRota(Enum enumChild);
        Task AddRota(RotaDto rota);
        Task<RotaDto> GetRotaForPersonAndDate(string name, Date date, string role);
        Task DeleteFromRota(RotaDto rota);
    }

    public class RotaService : IRotaService
    {
        private readonly IRotaRepo _rotaRepo;

        public RotaService(IRotaRepo rotaRepo)
        {
            _rotaRepo = rotaRepo;
        }

        public async Task AddRota(RotaDto rota)
        {
            await _rotaRepo.AddRota(rota);
        }

        public async Task DeleteFromRota(RotaDto rota)
        {
            await _rotaRepo.DeleteFromRota(rota);
        }

        public async Task<RotaDictionary> GetRota(Enum enumChild)
        {
            var enumType = enumChild.GetType().Name;
            var names = Enum.GetNames(enumChild.GetType());
            //var names = Enum.GetNames(typeof(YouthClubLeader)).OrderBy(n => n).ToList();

            List<RotaDto> rotas = await _rotaRepo.GetRota(enumType);
            // reutrn a dictionary of dates for a type (youth club) that contain a list of people and jobs for that date.
            // dates: {
            //            01/01/2010:
            //             {
            //                david: ["food","talks"], john: ["food"]
            //             }
            //        } 
            // to diplay, for date in dates.keys.orderby, then for name in names.ordereby, then for job in jobs do dates[date][name][job]
            // field names, "date", then name in names.ordereby
            // to display multiple jobs under a name, 1. concat jobs, 2. use a list
            IOrderedEnumerable<Date> rotaDates = rotas.Select(r => r.Date).ToList()
                                     .DistinctBy(r => new { r.Year, r.Month, r.Day }).ToList()
                                     .OrderBy(d => d.Year).ThenBy(d => d.Month).ThenBy(d => d.Day);

            var rotaJobsDictionary = new RotaDictionary();
            foreach (Date rotaDate in rotaDates)
            {
                var nameJobs = new NameRoles();
                foreach (var name in names)
                {
                    var jobs = new List<string>();
                    var userRotas = rotas.Where(r => r.Date.Equals(rotaDate) && r.Name == name).ToList();
                    userRotas.ForEach(r => jobs.Add(r.Role));
                    if (userRotas.Count == 0) jobs.Add("--");

                    nameJobs.KeyValues.Add(name, jobs);
                }
                rotaJobsDictionary.DateNameJobListPairs.Add(rotaDate, nameJobs);
            }
            return rotaJobsDictionary;
        }

        public async Task<RotaDto> GetRotaForPersonAndDate(string name, Date date, string role)
        {
            return await _rotaRepo.GetRotaForPersonAndDateAndRole(name, date, role);
        }
    }
}