using l2l_aggregator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database.Interfaces
{
    public interface IRegistrationDeviceRepository
    {
        Task SaveRegistrationAsync(ArmDeviceRegistrationResponse response);
    }
}
