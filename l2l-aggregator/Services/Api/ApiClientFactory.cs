using l2l_aggregator.Services.Database;
using Refit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Api
{
    public class ApiClientFactory
    {
        private readonly DatabaseService _databaseService;
        private readonly ConcurrentDictionary<Type, object> _clientCache = new();
        private HttpClient? _httpClient;

        public ApiClientFactory(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private async Task<HttpClient> GetHttpClientAsync()
        {
            if (_httpClient != null)
                return _httpClient;

            var serverUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
            if (string.IsNullOrWhiteSpace(serverUri))
                throw new Exception("Сервер не настроен!");

            var handler = new ApiKeyHandler
            {
                InnerHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                }
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(serverUri)
            };

            return _httpClient;
        }

        public async Task<T> CreateClientAsync<T>()
        {
            if (_clientCache.TryGetValue(typeof(T), out var cached))
                return (T)cached;

            var httpClient = await GetHttpClientAsync();
            var client = RestService.For<T>(httpClient);
            _clientCache.TryAdd(typeof(T), client);
            return client;
        }
    }
}
