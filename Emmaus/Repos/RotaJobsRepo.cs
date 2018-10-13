using Emmaus.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmaus.Repos
{
    public interface IRotaJobsRepo
    {
        Task<IEnumerable<string>> GetJobs(string rotaType);
        Task<IEnumerable<string>> GetJobs();
        Task AddJob(string name, string rotaType);
        Task DeleteJob(string name, string rotaType);
        Task<RotaTypeNameDto> GetJob(string name, string rotaType);
    }
    public class RotaJobsRepo : IRotaJobsRepo
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        public RotaJobsRepo(string tableKey)
        {
            _storageAccount = CloudStorageAccount.Parse(@tableKey);
            CloudTableClient tableClient = _storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference("rotaJobs");
            var createTable = Task.Run(() => _table.CreateIfNotExistsAsync());
            createTable.Wait();
        }

        public async Task<IEnumerable<string>> GetJobs(string rotaType)
        {
            try
            {
                var typeFilter = TableQuery.GenerateFilterCondition("RotaType", QueryComparisons.Equal, rotaType);
                TableQuery<RotaTypeNameDto> query = new TableQuery<RotaTypeNameDto>().Where(typeFilter);
                TableQuerySegment<RotaTypeNameDto> results = await _table.ExecuteQuerySegmentedAsync(query, null);

                return results.Select(rn => rn.Name);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not get jobs for: {rotaType}.", e);
            }
        }

        public async Task<IEnumerable<string>> GetJobs()
        {
            try
            {
                var query = new TableQuery<RotaTypeNameDto>();
                TableQuerySegment<RotaTypeNameDto> results = await _table.ExecuteQuerySegmentedAsync(query, null);

                return results.Select(rn => rn.Name).Distinct();
            }
            catch (Exception e)
            {
                throw new Exception($"Could not get all jobs", e);
            }
        }

        public async Task<RotaTypeNameDto> GetJob(string name, string rotaType)
        {
            try
            {
                var typeFilter = TableQuery.GenerateFilterCondition("RotaType", QueryComparisons.Equal, rotaType);
                var nameFilter = TableQuery.GenerateFilterCondition("Name", QueryComparisons.Equal, name);
                var filter = TableQuery.CombineFilters(typeFilter, TableOperators.And, nameFilter);
                TableQuery<RotaTypeNameDto> query = new TableQuery<RotaTypeNameDto>().Where(filter);

                TableQuerySegment<RotaTypeNameDto> results = await _table.ExecuteQuerySegmentedAsync(query, null);

                return results.First();
            }
            catch (Exception e)
            {
                throw new Exception($"Could not get {name} on: {rotaType}.", e);
            }
        }

        public async Task AddJob(string name, string rotaType)
        {
            try
            {
                var guid = Guid.NewGuid().ToString();
                var roteTypeNameDto = new RotaTypeNameDto()
                {
                    Name = name,
                    RotaType = rotaType,
                    PartitionKey = guid,
                    RowKey = guid
                };

                var insertOperation = TableOperation.InsertOrReplace(roteTypeNameDto);
                await _table.ExecuteAsync(insertOperation);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not add people on: {rotaType}.", e);
            }
        }

        public async Task DeleteJob(string name, string rotaType)
        {
            try
            {
                var rowId = (await GetJob(name, rotaType)).RowKey;
                await DeleteRow(rowId);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not delete {name} from {rotaType}.", e);
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
    }
}