using Emmaus.Logger;
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
        Task<IEnumerable<RotaItemDto>> GetSoughtedRota(string rotaType);
        Task AddRotaItem(RotaItemDto rota);
        Task<IEnumerable<RotaItemDto>> GetRotaItemsForPerson(string name);
        Task<RotaItemDto> GetRotaItemForPersonAndDateAndRole(string name, string role, string type, DateTime dateTime);
        Task DeleteRotaItemFromRota(RotaItemDto rota);
        Task DeleteInvalidNamesFromRota(string rotaType, IEnumerable<string> names);
        Task DeleteInvalidJobsFromRota(string rotaType, IEnumerable<string> jobs);
    }
    public class RotaRepo : IRotaRepo
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        public RotaRepo(string tableKey)
        {
            _storageAccount = CloudStorageAccount.Parse(@tableKey);
            CloudTableClient tableClient = _storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference("rota");
            var createTable = Task.Run(() => _table.CreateIfNotExistsAsync());
            createTable.Wait();
        }

        public async Task<IEnumerable<RotaItemDto>> GetSoughtedRota(string rotaType)
        {
            try
            {
                IEnumerable<RotaItemDto> rota = await GetRota(rotaType);
                var orderedRota = rota.OrderBy(r => r.DateTime).ToList();
                return orderedRota;
            }
            catch (Exception e)
            {
                throw new Exception("Could not get rota", e);
            }
        }

        public async Task<IEnumerable<RotaItemDto>> GetRotaItemsForPerson(string name)
        {
            try
            {
                TableQuery<RotaItemDto> query = new TableQuery<RotaItemDto>()
                                                .Where(TableQuery.GenerateFilterCondition("Name", QueryComparisons.Equal, name));

                TableQuerySegment<RotaItemDto> results = await _table.ExecuteQuerySegmentedAsync(query, null);
                return results.Results;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not get rota for {name}", e);
            }
        }

        public async Task<RotaItemDto> GetRotaItemForPersonAndDateAndRole(string name, string role, string rotaType, DateTime dateTime)
        {
            try
            {
                var nameFilter = TableQuery.GenerateFilterCondition("Name", QueryComparisons.Equal, name);
                var typeFilter = TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, rotaType);
                var roleFilter = TableQuery.GenerateFilterCondition("Role", QueryComparisons.Equal, role);

                var filter = TableQuery.CombineFilters(
                                    roleFilter,
                                    TableOperators.And,
                                    TableQuery.CombineFilters(nameFilter, TableOperators.And, typeFilter));

                TableQuery<RotaItemDto> query = new TableQuery<RotaItemDto>().Where(filter);
                TableQuerySegment<RotaItemDto> results = await _table.ExecuteQuerySegmentedAsync(query, null);
                return results.FirstOrDefault(ri => ri.DateTime == dateTime.ToUniversalTime());
            }
            catch (Exception e)
            {
                throw new Exception("Could not get rota for person and role", e);
            }
        }

        public async Task AddRotaItem(RotaItemDto rotaItem)
        {
            try
            {
                rotaItem.PartitionKey = rotaItem.Id;
                rotaItem.RowKey = rotaItem.Id;
                var insertOperation = TableOperation.InsertOrReplace(rotaItem);
                await _table.ExecuteAsync(insertOperation);
            }
            catch (Exception e)
            {
                throw new Exception("Rota not added", e);
            }
        }

        public async Task DeleteRotaItemFromRota(RotaItemDto rotaItem)
        {
            try
            {
                var id = (await GetRotaItemForPersonAndDateAndRole(rotaItem.Name, rotaItem.Role, rotaItem.Type, rotaItem.DateTime)).Id;
                await DeleteRow(id);
            }
            catch (Exception e)
            {
                throw new Exception("Rota not removed", e);
            }
        }

        public async Task DeleteInvalidNamesFromRota(string rotaType, IEnumerable<string> names)
        {
            var rota = await this.GetRota(rotaType);

            foreach (var rotaItem in rota)
            {
                if (!names.Contains(rotaItem.Name))
                {
                    await DeleteRow(rotaItem.Id);
                }
            }
        }

        public async Task DeleteInvalidJobsFromRota(string rotaType, IEnumerable<string> jobs)
        {
            var rota = await this.GetRota(rotaType);

            foreach (var rotaItem in rota)
            {
                if (!jobs.Contains(rotaItem.Role))
                {
                    await DeleteRow(rotaItem.Id);
                }
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

        private async Task<IEnumerable<RotaItemDto>> GetRota(string rotaType)
        {
            TableQuery<RotaItemDto> query = new TableQuery<RotaItemDto>()
                .Where(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, rotaType));
            TableQuerySegment<RotaItemDto> queryResult = await _table.ExecuteQuerySegmentedAsync(query, null);
            return queryResult.Results;
        }
    }
}