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
        Task<List<RotaItemDto>> GetRota(string type);
        Task AddRotaItem(RotaItemDto rota);
        Task<RotaItemDto> GetRotaItemForPersonAndDateAndRole(string name, Date date, string role, string type);
        Task DeleteRotaItemFromRota(RotaItemDto rota);
        Task DeleteInvalidNamesFromRota(Type typeofRotaEnum);
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

        public async Task<List<RotaItemDto>> GetRota(string rotaType)
        {
            try
            {
                TableQuery<RotaItemDto> query = new TableQuery<RotaItemDto>()
                    .Where(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, rotaType));
                TableQuerySegment<RotaItemDto> queryResult = await _table.ExecuteQuerySegmentedAsync(query, null);

                var rota = (List<RotaItemDto>)queryResult.Results;
                rota.ForEach(r => r.PackDate());
                var orderedRota = rota.OrderBy(s => s.Year).ThenBy(s => s.Month).ThenBy(s => s.Day).ToList();
                return orderedRota;
            }
            catch (Exception)
            {
                throw new Exception("Could not get rota");
            }
        }

        public async Task<RotaItemDto> GetRotaItemForPersonAndDateAndRole(string name, Date date, string role, string rotaType)
        {
            try
            {
                var nameFilter = TableQuery.GenerateFilterCondition("Name", QueryComparisons.Equal, name);
                var typeFilter = TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, rotaType);

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

                TableQuery<RotaItemDto> query = new TableQuery<RotaItemDto>().Where(filter);

                TableQuerySegment<RotaItemDto> results = await _table.ExecuteQuerySegmentedAsync(query, null);

                return results.FirstOrDefault();
            }
            catch (Exception)
            {
                throw new Exception("Could not get rota for person and role");
            }
        }

        public async Task AddRotaItem(RotaItemDto rotaItem)
        {
            try
            {
                rotaItem.PartitionKey = rotaItem.Id;
                rotaItem.RowKey = rotaItem.Id;
                rotaItem.UnPackDate();
                var insertOperation = TableOperation.InsertOrReplace(rotaItem);
                await _table.ExecuteAsync(insertOperation);
            }
            catch (Exception)
            {
                throw new Exception("Rota not added");
            }
        }

        public async Task DeleteRotaItemFromRota(RotaItemDto rotaItem)
        {
            try
            {
                var id = (await GetRotaItemForPersonAndDateAndRole(rotaItem.Name, rotaItem.Date, rotaItem.Role, rotaItem.Type)).Id;
                await DeleteRow(id);
            }
            catch (Exception)
            {
                throw new Exception("Rota not removed");
            }
        }

        private async Task DeleteRow(string rowId)
        {
            var retrieveOperation = TableOperation.Retrieve<RotaItemDto>(rowId, rowId);
            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);
            var deleteEntity = (RotaItemDto)retrievedResult.Result;
            var deleteOperation = TableOperation.Delete(deleteEntity);
            await _table.ExecuteAsync(deleteOperation);
        }

        public async Task DeleteInvalidNamesFromRota(Type typeofRotaEnum)
        {
            var names = Enum.GetNames(typeofRotaEnum);
            var type = typeofRotaEnum.Name;
            var typeFilter = TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type);

            TableQuery<RotaItemDto> query = new TableQuery<RotaItemDto>().Where(typeFilter);
            TableQuerySegment<RotaItemDto> queryResult = await _table.ExecuteQuerySegmentedAsync(query, null);
            var rota = (List<RotaItemDto>)queryResult.Results;

            foreach (var rotaItem in rota)
            {
                if (!names.Contains(rotaItem.Name))
                {
                    await DeleteRow(rotaItem.Id);
                }
            }
        }
    }
}