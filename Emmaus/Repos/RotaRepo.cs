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
                TableQuerySegment<RotaDto> a = await _table.ExecuteQuerySegmentedAsync(query, null);
                IOrderedEnumerable<RotaDto> b = a.OrderBy(s => s.Date.Year).ThenBy(s => s.Date.Month).ThenBy(s => s.Date.Day);
                return (b).ToList();
            }
            catch (Exception)
            {
                throw new Exception("Could not get rota");
            }
        }

        public async Task AddRota(RotaDto rota)
        {
            try
            {
                rota.PartitionKey = rota.Id;
                rota.RowKey = rota.Id;
                TableOperation insertOperation = TableOperation.InsertOrReplace(rota);
                await _table.ExecuteAsync(insertOperation);
            }
            catch (Exception)
            {
                throw new Exception("Rota not added");
            }
        }
    }
}