using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmaus.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Emmaus.Repos
{
    public class ServiceTableRepo : IServiceRepo
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        public ServiceTableRepo()
        {
            _storageAccount = CloudStorageAccount.Parse(
                @"DefaultEndpointsProtocol=https;AccountName=emmaus;AccountKey=vAqsHtaXxMKRjdusbs4hTYdG1NsYBRAUlhLRw2f+BO2/loKiLnxlJoYjdVwbGpC5dJMdZV9z1hqBPM1gSJNV5w==;EndpointSuffix=core.windows.net");
            CloudTableClient tableClient = _storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference("services");
            var createTable = Task.Run(() => _table.CreateIfNotExistsAsync());
            createTable.Wait();

        }

        public async Task<IEnumerable<Service>> GetServices(string type)
        {
            try
            {
                TableQuery<Service> query = new TableQuery<Service>()
                    .Where(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, type));
                var a = await _table.ExecuteQuerySegmentedAsync(query, null);
                var b = a.OrderBy(s => s.Date.Year).ThenBy(s => s.Date.Month).ThenBy(s => s.Date.Day);
                return (b).ToList();
            }
            catch (Exception)
            {
                throw new Exception("Could not get services");
            }
        }

        public async Task DeleteService(string id)
        {
            try
            {
                TableOperation retrieveOperation = TableOperation.Retrieve<Service>(id, id);
                TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);
                Service deleteEntity = (Service)retrievedResult.Result;

                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
                await _table.ExecuteAsync(deleteOperation);
            }
            catch (Exception)
            {
                throw new Exception("Service not removed");
            }
        }

        public async Task AddService(Service service)
        {
            try
            {
                service.PartitionKey = service.Id;
                service.RowKey = service.Id;
                TableOperation insertOperation = TableOperation.InsertOrReplace(service);
                await _table.ExecuteAsync(insertOperation);
            }
            catch (Exception)
            {
                throw new Exception("Service not added");
            }
        }

        public async Task UpdateService(Service service)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<Service>(service.Id, service.Id);
            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);
            Service retrieveEntity = (Service)retrievedResult.Result;

            retrieveEntity.Text = service.Text;
            retrieveEntity.Story = service.Story;
            retrieveEntity.Speaker = service.Speaker;
            if (service.Date.Year != 1999 || service.Date.Year > DateTime.Now.Year - 1)
            {
                retrieveEntity.Date = service.Date;
            }

            TableOperation tableOperation = TableOperation.Replace(retrieveEntity);
            await _table.ExecuteAsync(tableOperation);
        }
    }
}