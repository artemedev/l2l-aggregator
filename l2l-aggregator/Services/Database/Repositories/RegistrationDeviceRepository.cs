using Dapper;
using l2l_aggregator.Models;
using l2l_aggregator.Services.Database.Repositories.Interfaces;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database.Repositories
{
    // ========================== Регистрация устройства ==========================
    public class RegistrationDeviceRepository : BaseRepository, IRegistrationDeviceRepository
    {
        public RegistrationDeviceRepository(DatabaseInitializer dbService) : base(dbService) { }
        public Task SaveRegistrationAsync(ArmDeviceRegistrationResponse response) => 
            WithConnectionAsync(conn =>
               conn.ExecuteAsync(
                   @"INSERT INTO REGISTRATION_INFO 
                      (DEVICEID, DEVICE_NAME, LICENSE_DATA, SETTINGS_DATA)
                      VALUES (@DEVICEID, @DEVICE_NAME, @LICENSE_DATA, @SETTINGS_DATA)",
                   new
                   {
                       response.DEVICEID,
                       response.DEVICE_NAME,
                       response.LICENSE_DATA,
                       response.SETTINGS_DATA
                   }));
    }
}
