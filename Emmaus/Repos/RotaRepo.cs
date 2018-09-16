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
        Task<RotaDto> GetRotaForPersonAndDateAndRole(string name, Date date, string role);
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
            Task<bool> createTable = Task.Run(() => _table.CreateIfNotExistsAsync());
            createTable.Wait();
        }

        public async Task<List<RotaDto>> GetRota(string type)
        {
            try
            {
                TableQuery<RotaDto> query = new TableQuery<RotaDto>()
                    .Where(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type));
                TableQuerySegment<RotaDto> queryResult = await _table.ExecuteQuerySegmentedAsync(query, null);
                //populate date
                //IOrderedEnumerable<RotaDto> rotas = queryResult.OrderBy(s => s.Date.Year).ThenBy(s => s.Date.Month).ThenBy(s => s.Date.Day);
                //var a = rotas.
                var a = (List<RotaDto>)queryResult.Results;
                a.ForEach( r => r.PackDate());
                var b = a.OrderBy(s => s.Year).ThenBy(s => s.Month).ThenBy(s => s.Day).ToList();
                //IOrderedEnumerable<RotaDto>
                return b;
            }
            catch (Exception e)
            {
                throw new Exception("Could not get rota");
            }
        }

        public async Task<RotaDto> GetRotaForPersonAndDateAndRole(string name, Date date, string role)
        {
            try
            {
                string nameFilter = TableQuery.GenerateFilterCondition(
                   "Name", QueryComparisons.Equal,name);

                string yearFilter = TableQuery.GenerateFilterConditionForInt("Year", QueryComparisons.Equal, date.Year);
                string monthFilter = TableQuery.GenerateFilterConditionForInt("Month", QueryComparisons.Equal, date.Month);
                string dayFilter = TableQuery.GenerateFilterConditionForInt("Day", QueryComparisons.Equal, date.Day);

                string roleFilter = TableQuery.GenerateFilterCondition("Role", QueryComparisons.Equal, role);

                string filter = TableQuery.CombineFilters(
                                    TableQuery.CombineFilters(yearFilter, TableOperators.And, monthFilter),
                                    TableOperators.And,
                                    TableQuery.CombineFilters(dayFilter, TableOperators.And, roleFilter)
                                    );

                TableQuery<RotaDto> query = new TableQuery<RotaDto>().Where(filter);

                TableQuerySegment<RotaDto> a = await _table.ExecuteQuerySegmentedAsync(query, null);

                return a.FirstOrDefault();
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
                TableOperation insertOperation = TableOperation.InsertOrReplace(rota);
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
                var id = (await GetRotaForPersonAndDateAndRole(rota.Name, rota.Date, rota.Role)).Id;

                TableOperation retrieveOperation = TableOperation.Retrieve<RotaDto>(id, id);
                TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);
                RotaDto deleteEntity = (RotaDto)retrievedResult.Result;

                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
                await _table.ExecuteAsync(deleteOperation);
            }
            catch (Exception)
            {
                throw new Exception("Rota not removed");
            }
        }
    }
}