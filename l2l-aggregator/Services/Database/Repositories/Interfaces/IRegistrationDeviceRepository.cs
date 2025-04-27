using l2l_aggregator.Models;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database.Repositories.Interfaces
{
    public interface IRegistrationDeviceRepository
    {
        Task SaveRegistrationAsync(ArmDeviceRegistrationResponse response);
    }
}
