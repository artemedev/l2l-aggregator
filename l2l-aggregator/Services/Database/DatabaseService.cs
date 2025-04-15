using l2l_aggregator.Services.Database.Interfaces;

namespace l2l_aggregator.Services.Database
{
    public class DatabaseService
    {
        public IUserAuthRepository UserAuth { get; }
        public IConfigRepository Config { get; }
        public IRegistrationDeviceRepository RegistrationDevice { get; }

        public DatabaseService(
            IUserAuthRepository userAuth,
            IConfigRepository config,
            IRegistrationDeviceRepository registrationDevice)
        {
            UserAuth = userAuth;
            Config = config;
            RegistrationDevice = registrationDevice;
        }
    }
}
