﻿using Emmaus.Models;
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
        Task<List<RotaItemDto>> GetRota(Type typeofRotaEnum);
        Task AddRotaItem(RotaItemDto rota);
        Task<RotaItemDto> GetRotaItemForPersonAndDateAndRole(string name, string role, string type, DateTime dateTime);
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

        public async Task<List<RotaItemDto>> GetRota(Type typeofRotaEnum)
        {
            try
            {
                List<RotaItemDto> rota = await GetUnpackedRota(typeofRotaEnum);
                var orderedRota = rota.OrderBy(r => r.DateTime).ToList();
                return orderedRota;
            }
            catch (Exception)
            {
                throw new Exception("Could not get rota");
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
                var id = (await GetRotaItemForPersonAndDateAndRole(rotaItem.Name, rotaItem.Role, rotaItem.Type, rotaItem.DateTime)).Id;
                await DeleteRow(id);
            }
            catch (Exception)
            {
                throw new Exception("Rota not removed");
            }
        }

        public async Task DeleteInvalidNamesFromRota(Type typeofRotaEnum)
        {
            var names = Enum.GetNames(typeofRotaEnum);
            var rota = await GetUnpackedRota(typeofRotaEnum);

            foreach (var rotaItem in rota)
            {
                if (!names.Contains(rotaItem.Name))
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

        private async Task<List<RotaItemDto>> GetUnpackedRota(Type typeofRotaEnum)
        {
            var rotaType = typeofRotaEnum.Name;
            TableQuery<RotaItemDto> query = new TableQuery<RotaItemDto>()
                .Where(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, rotaType));
            TableQuerySegment<RotaItemDto> queryResult = await _table.ExecuteQuerySegmentedAsync(query, null);
            var rota = queryResult.Results;
            return rota;
        }
    }
}