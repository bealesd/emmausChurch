using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmaus.Models;

namespace Emmaus.Repos
{
    public interface IServiceRepo
    {
        List<ServiceCosmos> GetServices();
        void DeleteService(ServiceCosmos service);
        void AddService(ServiceCosmos service);
    }

    public class ServiceCosmosRepo
    {
        public DocumentDBRepo<ServiceCosmos> _documentDBRepo;

        public ServiceCosmosRepo(DocumentDBRepo<ServiceCosmos> documentDBRepo)
        {
            _documentDBRepo = documentDBRepo;
        }

        public async Task<IEnumerable<ServiceCosmos>> GetServices(string type)
        {
            return await _documentDBRepo.GetItemsAsync(d => d.Type == type);
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

        public async Task AddService(ServiceCosmos service)
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