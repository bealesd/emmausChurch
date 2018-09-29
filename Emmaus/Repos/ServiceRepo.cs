using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmaus.Repos
{
    public interface IServiceRepo
    {
        Task<IEnumerable<Models.Service>> GetServices(string serviceType);
        Task DeleteService(string id);
        Task AddService(Models.Service service);
        Task UpdateService(Models.Service service);
    }

    public class ServiceRepo : IServiceRepo
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _table;
        public ServiceRepo()
        {
            _storageAccount = CloudStorageAccount.Parse(
                @"DefaultEndpointsProtocol=https;AccountName=emmaus;AccountKey=vAqsHtaXxMKRjdusbs4hTYdG1NsYBRAUlhLRw2f+BO2/loKiLnxlJoYjdVwbGpC5dJMdZV9z1hqBPM1gSJNV5w==;EndpointSuffix=core.windows.net");
            CloudTableClient tableClient = _storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference("services");
            var createTable = Task.Run(() => _table.CreateIfNotExistsAsync());
            createTable.Wait();

        }

        public async Task<IEnumerable<Models.Service>> GetServices(string serviceType)
        {
            try
            {
                TableQuery<Models.Service> serviceQuery = new TableQuery<Models.Service>()
                    .Where(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, serviceType));
                TableQuerySegment<Models.Service> servicesQuery = await _table.ExecuteQuerySegmentedAsync(serviceQuery, null);
                IOrderedEnumerable<Models.Service> services = servicesQuery
                                                                    .OrderBy(s => s.Date.Year)
                                                                    .ThenBy(s => s.Date.Month)
                                                                    .ThenBy(s => s.Date.Day);
                return services.ToList();
            }
            catch (Exception e)
            {
                throw new Exception("Could not get services", e);
            }
        }

        public async Task DeleteService(string id)
        {
            try
            {
                var retrieveOperation = TableOperation.Retrieve<Models.Service>(id, id);
                TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);
                var deleteEntity = (Models.Service)retrievedResult.Result;

                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
                await _table.ExecuteAsync(deleteOperation);
            }
            catch (Exception e)
            {
                throw new Exception("Service not removed", e);
            }
        }

        public async Task AddService(Models.Service service)
        {
            try
            {
                service.PartitionKey = service.Id;
                service.RowKey = service.Id;
                TableOperation insertOperation = TableOperation.InsertOrReplace(service);
                await _table.ExecuteAsync(insertOperation);
            }
            catch (Exception e)
            {
                throw new Exception("Service not added", e);
            }
        }

        public async Task UpdateService(Models.Service service)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<Models.Service>(service.Id, service.Id);
            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);
            var retrieveEntity = (Models.Service)retrievedResult.Result;

            retrieveEntity.Text = service.Text;
            retrieveEntity.Story = service.Story;
            retrieveEntity.Speaker = service.Speaker;
            service.Date = service.Date.AddHours(12);
            if (service.Date.Year != 1999 || service.Date.Year > DateTime.Now.Year - 1)
            {
                retrieveEntity.Date = service.Date;
            }

            TableOperation tableOperation = TableOperation.Replace(retrieveEntity);
            await _table.ExecuteAsync(tableOperation);
        }
    }
}