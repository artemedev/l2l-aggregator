// DatabaseDataService.cs - обновленный для работы с процедурами и статичным адресом БД
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
        private bool _isConnectionInitialized = false;

        public DatabaseDataService(
            RemoteDatabaseService remoteDatabaseService,
            DatabaseService localDatabaseService,
            INotificationService notificationService)
        {
            _remoteDatabaseService = remoteDatabaseService;
            _localDatabaseService = localDatabaseService;
            _notificationService = notificationService;
        }

        // Метод для инициализации подключения (вызывается один раз)
        private async Task<bool> EnsureConnectionAsync()
        {
            if (!_isConnectionInitialized)
            {
                _isConnectionInitialized = await _remoteDatabaseService.InitializeConnectionAsync();
                if (_isConnectionInitialized)
                {
                    _notificationService.ShowMessage("Соединение с удаленной БД установлено", NotificationType.Success);
                }
            }
            return _isConnectionInitialized;
        }

        // Принудительная проверка соединения
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _isConnectionInitialized = false; // Сбрасываем флаг для повторной проверки
                return await EnsureConnectionAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка проверки подключения: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        // ---------------- AUTH ----------------
        public async Task<UserAuthResponse?> LoginAsync(string login, string password)
        {
            try
            {
                if (!await EnsureConnectionAsync())
                {
                    _notificationService.ShowMessage("Нет подключения к удаленной БД", NotificationType.Error);
                    return null;
                }

                var response = await _remoteDatabaseService.LoginAsync(login, password);

                if (response?.AUTH_OK == "1")
                {
                    await _localDatabaseService.UserAuth.SaveUserAuthAsync(response);
                    _notificationService.ShowMessage($"Добро пожаловать, {response.USER_NAME}!", NotificationType.Success);

                    // Проверяем права администратора для возможности входа в настройки
                    if (long.TryParse(response.USERID, out var userId))
                    {
                        bool isAdmin = await _remoteDatabaseService.CheckAdminRoleAsync(userId);
                        if (isAdmin)
                        {
                            _notificationService.ShowMessage("Права администратора подтверждены", NotificationType.Info);
                        }
                        // Можно сохранить информацию об админских правах в локальной БД или SessionService
                    }

                    return response;
                }
                else
                {
                    var errorMsg = response?.ERROR_TEXT ?? "Неверный логин или пароль";
                    _notificationService.ShowMessage(errorMsg, NotificationType.Error);
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
                if (!await EnsureConnectionAsync())
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
                if (!await EnsureConnectionAsync())
                {
                    return null;
                }

                var response = await _remoteDatabaseService.RegisterDeviceAsync(data);
                if (response != null)
                {
                    _notificationService.ShowMessage($"Устройство '{response.DEVICE_NAME}' зарегистрировано", NotificationType.Success);
                }

                return response;
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
                if (!await EnsureConnectionAsync())
                {
                    return null;
                }

                var response = await _remoteDatabaseService.GetJobsAsync(userId);
                if (response?.RECORDSET?.Any() == true)
                {
                    _notificationService.ShowMessage($"Загружено {response.RECORDSET.Count} заданий", NotificationType.Info);
                }
                else
                {
                    _notificationService.ShowMessage("Задания не найдены", NotificationType.Warn);
                }

                return response;
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
                if (!await EnsureConnectionAsync())
                {
                    return null;
                }

                var response = await _remoteDatabaseService.GetJobDetailsAsync(docId);
                if (response != null)
                {
                    _notificationService.ShowMessage($"Задание {response.DOC_NUM} загружено", NotificationType.Success);
                }

                return response;
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
                if (!await EnsureConnectionAsync())
                {
                    return null;
                }

                var response = await _remoteDatabaseService.GetSgtinAsync(docId);
                if (response?.RECORDSET?.Any() == true)
                {
                    _notificationService.ShowMessage($"Загружено {response.RECORDSET.Count} SGTIN кодов", NotificationType.Info);
                }

                return response;
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
                if (!await EnsureConnectionAsync())
                {
                    return null;
                }

                var response = await _remoteDatabaseService.GetSsccAsync(docId);
                if (response?.RECORDSET?.Any() == true)
                {
                    _notificationService.ShowMessage($"Загружено {response.RECORDSET.Count} SSCC кодов", NotificationType.Info);
                }

                return response;
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
                if (!await EnsureConnectionAsync())
                {
                    return null;
                }

                if (long.TryParse(userId, out var userIdLong))
                {
                    var sessionId = await _remoteDatabaseService.StartSessionAsync(docId, userIdLong);
                    if (sessionId.HasValue)
                    {
                        _notificationService.ShowMessage($"Сессия агрегации начата (ID: {sessionId})", NotificationType.Success);
                    }
                    return sessionId;
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
                if (!await EnsureConnectionAsync())
                {
                    return false;
                }

                if (long.TryParse(userId, out var userIdLong))
                {
                    var result = await _remoteDatabaseService.CloseSessionAsync(userIdLong);
                    if (result)
                    {
                        _notificationService.ShowMessage("Сессия агрегации завершена", NotificationType.Success);
                    }
                    return result;
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
                if (!await EnsureConnectionAsync())
                {
                    return false;
                }

                var result = await _remoteDatabaseService.LogAggregationAsync(docId);
                if (result)
                {
                    _notificationService.ShowMessage("Агрегация успешно зарегистрирована", NotificationType.Success);
                }

                return result;
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка логирования агрегации: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        // ---------------- Connection Management ----------------
        [Obsolete("Метод устарел, так как используется статичный адрес БД")]
        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            // Метод оставлен для обратной совместимости, но игнорирует переданную строку подключения
            _notificationService.ShowMessage("Используется статичный адрес БД, переданная строка подключения игнорируется", NotificationType.Warn);
            return await TestConnectionAsync();
        }

        // Получение информации о подключении
        public string GetConnectionInfo()
        {
            return _remoteDatabaseService.ConnectionString;
        }

        // Принудительный сброс состояния подключения
        public void ResetConnection()
        {
            _isConnectionInitialized = false;
            _notificationService.ShowMessage("Состояние подключения сброшено", NotificationType.Info);
        }

        // Свойства для отслеживания состояния сессии
        public long? CurrentSessionId => _remoteDatabaseService.CurrentSessionId;
        public long? CurrentDeviceId => _remoteDatabaseService.CurrentDeviceId;
        public bool IsConnectionInitialized => _isConnectionInitialized;
    }
}