using l2l_aggregator.Models;
using l2l_aggregator.Services.Api.Interfaces;
using l2l_aggregator.Services.Database;

//using l2l_aggregator.Services.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Api
{
    public class DataApiService
    {
        private readonly ApiClientFactory _apiClientFactory;
        private readonly DatabaseService _database;

        public DataApiService(ApiClientFactory apiClientFactory, DatabaseService database)
        {
            _apiClientFactory = apiClientFactory;
            _database = database;
        }

        // ---------------- AUTH ----------------

        public async Task<UserAuthResponse?> LoginAsync(string login, string password)
        {
            var client = await _apiClientFactory.CreateClientAsync<IAuthApi>();
            var response = await client.UserAuth(new UserAuthRequest { wid = login, spd = password });

            if (response.AUTH_OK == "1")
            {
                await _database.UserAuth.SaveUserAuthAsync(response);
                return response;
            }

            return null;
        }

        public async Task<ArmDeviceRegistrationResponse> RegisterDeviceAsync(ArmDeviceRegistrationRequest data)
        {
            var client = await _apiClientFactory.CreateClientAsync<IAuthApi>();
            var response = await client.RegisterDevice(data);
            await _database.RegistrationDevice.SaveRegistrationAsync(response);
            return response;
        }

        // ---------------- JOB LIST ----------------

        public async Task<ArmJobResponse> GetJobsAsync(string userId)
        {
            var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
            return await client.GetJobs(new ArmJobRequest { userid = userId });
        }

        // ---------------- JOB DETAILS ----------------
        public async Task<ArmJobInfoRecord?> GetJobDetailsAsync(long docId)
        {
            var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
            var jobInfoTask = client.GetJob(new ArmJobInfoRequest { docid = docId });
            return jobInfoTask.Result.RECORDSET.FirstOrDefault();
        }

        public async Task<ArmJobSgtinResponse> GetSgtinAsync(long docId)
        {
            var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
            return await client.GetJobSgtin(new ArmJobSgtinRequest { docid = docId });
        }

        public async Task<ArmJobSsccResponse> GetSsccAsync(long docId)
        {
            var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
            return await client.GetJobSscc(new ArmJobSsccRequest { docid = docId });
        }
        public async Task<ArmJobSgtinResponse> LoadSgtinAsync(long docId)
        {
            var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
            var request = new ArmJobSgtinRequest { docid = docId };
            return await client.GetJobSgtin(request);
        }

        public async Task<ArmJobSsccResponse> LoadSsccAsync(long docId)
        {
            var client = await _apiClientFactory.CreateClientAsync<ITaskApi>();
            var request = new ArmJobSsccRequest { docid = docId };
            return await client.GetJobSscc(request);
        }
    }
}
