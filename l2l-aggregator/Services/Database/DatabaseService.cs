﻿using l2l_aggregator.Services.Database.Repositories.Interfaces;

namespace l2l_aggregator.Services.Database
{
    public class DatabaseService
    {
        public IUserAuthRepository UserAuth { get; }
        public IConfigRepository Config { get; }
        public IRegistrationDeviceRepository RegistrationDevice { get; }
        public INotificationLogRepository NotificationLog { get; }

        public IAggregationStateRepository AggregationState { get; }
        public DatabaseService(
            IUserAuthRepository userAuth,
            IConfigRepository config,
            IRegistrationDeviceRepository registrationDevice,
            INotificationLogRepository notificationLog,
            IAggregationStateRepository aggregationState)
        {
            UserAuth = userAuth;
            Config = config;
            RegistrationDevice = registrationDevice;
            NotificationLog = notificationLog;
            AggregationState = aggregationState;
        }
    }
}
