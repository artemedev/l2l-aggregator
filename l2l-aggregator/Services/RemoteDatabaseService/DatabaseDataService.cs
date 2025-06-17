// DatabaseDataService.cs - обновленный для работы с процедурами
using l2l_aggregator.Models;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Repositories.Interfaces;
using l2l_aggregator.Services.Notification.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace l2l_aggregator.Services
{
    public class DatabaseDataService
    {
        private readonly RemoteDatabaseService _remoteDatabaseService;
        private readonly DatabaseService _localDatabaseService;
        private readonly INotificationService _notificationService;

        public DatabaseDataService(
            RemoteDatabaseService remoteDatabaseService,
            DatabaseService localDatabaseService,
            INotificationService notificationService)
        {
            _remoteDatabaseService = remoteDatabaseService;
            _localDatabaseService = localDatabaseService;
            _notificationService = notificationService;
        }

        // ---------------- AUTH ----------------
        public async Task<UserAuthResponse?> LoginAsync(string login, string password)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return null;
                }

                var response = await _remoteDatabaseService.LoginAsync(login, password);

                if (response?.AUTH_OK == "1")
                {
                    await _localDatabaseService.UserAuth.SaveUserAuthAsync(response);

                    // Проверяем права администратора для возможности входа в настройки
                    if (long.TryParse(response.USERID, out var userId))
                    {
                        bool isAdmin = await _remoteDatabaseService.CheckAdminRoleAsync(userId);
                        // Можно сохранить информацию об админских правах в локальной БД или SessionService
                    }

                    return response;
                }

                return null;
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка входа: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        // Проверка прав администратора
        public async Task<bool> CheckAdminRoleAsync(string userId)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return false;
                }

                if (long.TryParse(userId, out var userIdLong))
                {
                    return await _remoteDatabaseService.CheckAdminRoleAsync(userIdLong);
                }

                return false;
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка проверки прав администратора: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        public async Task<ArmDeviceRegistrationResponse?> RegisterDeviceAsync(ArmDeviceRegistrationRequest data)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return null;
                }

                return await _remoteDatabaseService.RegisterDeviceAsync(data);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка регистрации устройства: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        // ---------------- JOB LIST ----------------
        public async Task<ArmJobResponse?> GetJobsAsync(string userId)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return null;
                }

                return await _remoteDatabaseService.GetJobsAsync(userId);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения заданий: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        // ---------------- JOB DETAILS ----------------
        public async Task<ArmJobInfoRecord?> GetJobDetailsAsync(long docId)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return null;
                }

                return await _remoteDatabaseService.GetJobDetailsAsync(docId);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения деталей задания: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        public async Task<ArmJobSgtinResponse?> GetSgtinAsync(long docId)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return null;
                }

                return await _remoteDatabaseService.GetSgtinAsync(docId);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения SGTIN: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        public async Task<ArmJobSsccResponse?> GetSsccAsync(long docId)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return null;
                }

                return await _remoteDatabaseService.GetSsccAsync(docId);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения SSCC: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        public async Task<ArmJobSgtinResponse?> LoadSgtinAsync(long docId)
        {
            return await GetSgtinAsync(docId);
        }

        public async Task<ArmJobSsccResponse?> LoadSsccAsync(long docId)
        {
            return await GetSsccAsync(docId);
        }

        // ---------------- SESSION MANAGEMENT ----------------
        public async Task<long?> StartAggregationSessionAsync(long docId, string userId)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return null;
                }

                if (long.TryParse(userId, out var userIdLong))
                {
                    return await _remoteDatabaseService.StartSessionAsync(docId, userIdLong);
                }

                return null;
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка начала сессии агрегации: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        public async Task<bool> CloseAggregationSessionAsync(string userId)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return false;
                }

                if (long.TryParse(userId, out var userIdLong))
                {
                    return await _remoteDatabaseService.CloseSessionAsync(userIdLong);
                }

                return false;
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка закрытия сессии агрегации: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        public async Task<bool> LogAggregationCompletedAsync(long docId)
        {
            try
            {
                if (!await _remoteDatabaseService.InitializeConnectionAsync())
                {
                    return false;
                }

                return await _remoteDatabaseService.LogAggregationAsync(docId);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка логирования агрегации: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        // ---------------- Connection Management ----------------
        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                await _remoteDatabaseService.SetConnectionStringAsync(connectionString);
                return await _remoteDatabaseService.TestConnectionAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка проверки подключения: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        // Свойства для отслеживания состояния сессии
        public long? CurrentSessionId => _remoteDatabaseService.CurrentSessionId;
        public long? CurrentDeviceId => _remoteDatabaseService.CurrentDeviceId;
    }
}