using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace Emmaus.Logger
{
    public class AzureLogging
    {
        private static LoggerRepo factory = null;
        public static LoggerRepo LoggerRepo
        {
            get
            {
                if (factory == null)
                    factory = new LoggerRepo();

                return factory;
            }
            set { factory = value; }
        }

        public static async Task<string> CreateLog(string message, string user, LogLevel logLevel) => await LoggerRepo.CreateAndStoreLog(message, user, logLevel);
    }
}
public class LoggerRepo
{
    private CloudStorageAccount storageAccount;
    private CloudTable table;
    public LoggerRepo()
    {
        storageAccount = CloudStorageAccount.Parse(
            @"DefaultEndpointsProtocol=https;AccountName=emmaus;AccountKey=vAqsHtaXxMKRjdusbs4hTYdG1NsYBRAUlhLRw2f+BO2/loKiLnxlJoYjdVwbGpC5dJMdZV9z1hqBPM1gSJNV5w==;EndpointSuffix=core.windows.net");
        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        table = tableClient.GetTableReference("logging");
        var createTable = Task.Run(() => table.CreateIfNotExistsAsync());
        createTable.Wait();
    }

    public async Task<string> CreateAndStoreLog(string message, string user, LogLevel logLevel)
    {
        var log = new Log() { Message = message, User = user, LogLevel = logLevel, Id = Guid.NewGuid().ToString() };

        log.PartitionKey = log.Id;
        log.RowKey = log.Id;
        var insertOperation = TableOperation.InsertOrReplace(log);
        await table.ExecuteAsync(insertOperation);
        return log.Id;
    }
}

public class Log: TableEntity
{
    public string Id { get; set; }
    public LogLevel LogLevel { get; set; }
    public string Message { get; set; }
    public string User { get; set; }
}

public enum LogLevel
{
    Information, Warning, Error
}