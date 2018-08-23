using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emmaus.Models;

namespace Emmaus.Repos
{
    public interface IServiceRepo
    {
        Task<IEnumerable<Service>> GetServices(string id);
        Task DeleteService(string id);
        Task AddService(Service service);
    }

    public class ServiceCosmosRepo: IServiceRepo
    {
        public IDocumentDBRepository<Service> _documentDBRepo;

        public ServiceCosmosRepo(IDocumentDBRepository<Service> documentDBRepo)
        {
            _documentDBRepo = documentDBRepo;
        }

        public async Task<IEnumerable<Service>> GetServices(string type)
        {
            try
            {
                return await _documentDBRepo.GetItemsAsync(d => d.Type == type);
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
                await _documentDBRepo.DeleteItemAsync(id);
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
                await _documentDBRepo.CreateItemAsync(service);
            }
            catch (Exception)
            {
                throw new Exception("Service not added");
            }
        }
    }
}