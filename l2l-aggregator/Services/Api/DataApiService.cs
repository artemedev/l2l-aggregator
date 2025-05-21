using l2l_aggregator.Models;
using l2l_aggregator.Services.Api.Interfaces;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Notification;
using l2l_aggregator.Services.Notification.Interface;
using Refit;


//using l2l_aggregator.Services.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Api
{
    public class DataApiService
    {
        private readonly ApiClientFactory _apiClientFactory;
        private readonly DatabaseService _database;
        private readonly INotificationService _notificationService;

        public DataApiService(ApiClientFactory apiClientFactory, DatabaseService database, INotificationService notificationService)
        {
            _apiClientFactory = apiClientFactory;
            _database = database;
            _notificationService = notificationService;
        }

        // ---------------- AUTH ----------------

        public async Task<UserAuthResponse?> LoginAsync(string login, string password)
        {
            return await SafeApiCall(async () =>
            {
                var client = await _apiClientFactory.CreateClientAsync<IAuthApi>();
                var response = await client.UserAuth(new UserAuthRequest { wid = login, spd = password });

                if (response.AUTH_OK == "1")
                {
                    await _database.UserAuth.SaveUserAuthAsync(response);
                    return response;
                }

                return null;
            });
        }

        public async Task<ArmDeviceRegistrationResponse?> RegisterDeviceAsync(ArmDeviceRegistrationRequest data)
        {
            return await SafeApiCall(async () =>
            {
                var client = await _apiClientFactory.CreateClientAsync<IAuthApi>();
                var response = await client.RegisterDevice(data);
                await _database.RegistrationDevice.SaveRegistrationAsync(response);
                return response;
            });
        }

        // ---------------- JOB LIST ----------------

        public async Task<ArmJobResponse?> GetJobsAsync(string userId)
        {
            return await SafeApiCall(async () =>
            {
                var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
                return await client.GetJobs(new ArmJobRequest { userid = userId });
            });
        }

        // ---------------- JOB DETAILS ----------------

        public async Task<ArmJobInfoRecord?> GetJobDetailsAsync(long docId)
        {
            return await SafeApiCall(async () =>
            {
                var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
                var response = await client.GetJob(new ArmJobInfoRequest { docid = docId });
                return response.RECORDSET.FirstOrDefault();
            });
        }

        public async Task<ArmJobSgtinResponse?> GetSgtinAsync(long docId)
        {
            return await SafeApiCall(async () =>
            {
                var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
                return await client.GetJobSgtin(new ArmJobSgtinRequest { docid = docId });
            });
        }

        public async Task<ArmJobSsccResponse?> GetSsccAsync(long docId)
        {
            return await SafeApiCall(async () =>
            {
                var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
                return await client.GetJobSscc(new ArmJobSsccRequest { docid = docId });
            });
        }

        public async Task<ArmJobSgtinResponse?> LoadSgtinAsync(long docId)
        {
            return await SafeApiCall(async () =>
            {
                var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
                return await client.GetJobSgtin(new ArmJobSgtinRequest { docid = docId });
            });
        }

        public async Task<ArmJobSsccResponse?> LoadSsccAsync(long docId)
        {
            return await SafeApiCall(async () =>
            {
                var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
                return await client.GetJobSscc(new ArmJobSsccRequest { docid = docId });
            });
        }
        // ---------------- Обработка ошибок ----------------
        private async Task<T?> SafeApiCall<T>(Func<Task<T>> apiCall)
        {
            try
            {
                return await apiCall();
            }
            catch (ApiException apiEx)
            {
                _notificationService.ShowMessage($"Ошибка API: {apiEx.StatusCode} — {apiEx.Message}", NotificationType.Error);
            }
            catch (HttpRequestException httpEx)
            {
                _notificationService.ShowMessage($"Ошибка HTTP-запроса: {httpEx.Message}", NotificationType.Error);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Непредвиденная ошибка: {ex.Message}", NotificationType.Error);
            }

            return default;
        }
    }
}
