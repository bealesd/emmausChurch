using Emmaus.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmaus.Repos
{
    public interface IRotaRepo
    {
        Task<List<RotaDto>> GetRota(string type);
        Task AddRota(RotaDto rota);
        Task<RotaDto> GetRotaForPersonAndDateAndRole(string name, Date date, string role, string type);
        Task DeleteFromRota(RotaDto rota);
    }
    public class RotaRepo : IRotaRepo
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        public RotaRepo()
        {
            _storageAccount = CloudStorageAccount.Parse(
                @"DefaultEndpointsProtocol=https;AccountName=emmaus;AccountKey=vAqsHtaXxMKRjdusbs4hTYdG1NsYBRAUlhLRw2f+BO2/loKiLnxlJoYjdVwbGpC5dJMdZV9z1hqBPM1gSJNV5w==;EndpointSuffix=core.windows.net");
            CloudTableClient tableClient = _storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference("rotas");
            var createTable = Task.Run(() => _table.CreateIfNotExistsAsync());
            createTable.Wait();
        }

        public async Task<List<RotaDto>> GetRota(string type)
        {
            try
            {
                TableQuery<RotaDto> query = new TableQuery<RotaDto>()
                    .Where(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type));
                TableQuerySegment<RotaDto> queryResult = await _table.ExecuteQuerySegmentedAsync(query, null);

                var rotas = (List<RotaDto>)queryResult.Results;
                rotas.ForEach(r => r.PackDate());
                var orderedRotas = rotas.OrderBy(s => s.Year).ThenBy(s => s.Month).ThenBy(s => s.Day).ToList();
                return orderedRotas;
            }
            catch (Exception)
            {
                throw new Exception("Could not get rota");
            }
        }

        public async Task<RotaDto> GetRotaForPersonAndDateAndRole(string name, Date date, string role, string type)
        {
            try
            {
                var nameFilter = TableQuery.GenerateFilterCondition("Name", QueryComparisons.Equal, name);
                var typeFilter = TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type);

                var yearFilter = TableQuery.GenerateFilterConditionForInt("Year", QueryComparisons.Equal, date.Year);
                var monthFilter = TableQuery.GenerateFilterConditionForInt("Month", QueryComparisons.Equal, date.Month);
                var dayFilter = TableQuery.GenerateFilterConditionForInt("Day", QueryComparisons.Equal, date.Day);

                var roleFilter = TableQuery.GenerateFilterCondition("Role", QueryComparisons.Equal, role);

                var filter = TableQuery.CombineFilters(
                                TableQuery.CombineFilters(
                                    TableQuery.CombineFilters(yearFilter, TableOperators.And, monthFilter),
                                    TableOperators.And,
                                    TableQuery.CombineFilters(dayFilter, TableOperators.And, roleFilter)
                                    ),
                                TableOperators.And,
                                TableQuery.CombineFilters(nameFilter, TableOperators.And, typeFilter)
                             );

                TableQuery<RotaDto> query = new TableQuery<RotaDto>().Where(filter);

                TableQuerySegment<RotaDto> results = await _table.ExecuteQuerySegmentedAsync(query, null);

                return results.FirstOrDefault();
            }
            catch (Exception)
            {
                throw new Exception("Could not get rota for person and role");
            }
        }

        public async Task AddRota(RotaDto rota)
        {
            try
            {
                rota.PartitionKey = rota.Id;
                rota.RowKey = rota.Id;
                rota.UnPackDate();
                var insertOperation = TableOperation.InsertOrReplace(rota);
                await _table.ExecuteAsync(insertOperation);
            }
            catch (Exception)
            {
                throw new Exception("Rota not added");
            }
        }

        public async Task DeleteFromRota(RotaDto rota)
        {
            try
            {
                var id = (await GetRotaForPersonAndDateAndRole(rota.Name, rota.Date, rota.Role, rota.Type)).Id;

                var retrieveOperation = TableOperation.Retrieve<RotaDto>(id, id);
                TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);
                var deleteEntity = (RotaDto)retrievedResult.Result;

                var deleteOperation = TableOperation.Delete(deleteEntity);
                await _table.ExecuteAsync(deleteOperation);
            }
            catch (Exception)
            {
                throw new Exception("Rota not removed");
            }
        }
    }
}