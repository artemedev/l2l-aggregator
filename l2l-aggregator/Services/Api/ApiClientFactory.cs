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
    //public interface IApiClientFactory
    //{
    //    Task<T> CreateClientAsync<T>(bool bypassSsl = false);
    //}

    //public class ApiClientFactory : IApiClientFactory
    //{
    //    private readonly DatabaseService _databaseService;

    //    public ApiClientFactory(DatabaseService databaseService)
    //    {
    //        _databaseService = databaseService;
    //    }

    //    public async Task<T> CreateClientAsync<T>(bool bypassSsl = false)
    //    {
    //        var serverUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
    //        if (string.IsNullOrWhiteSpace(serverUri))
    //            throw new InvalidOperationException("Server URI is not configured");

    //        var handler = new HttpClientHandler();
    //        if (bypassSsl)
    //            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

    //        var client = new HttpClient(handler) { BaseAddress = new Uri(serverUri) };
    //        return RestService.For<T>(client);
    //    }
    //}
    //public class ApiClientFactory
    //{
    //    private readonly DatabaseService _databaseService;

    //    public ApiClientFactory(DatabaseService databaseService)
    //    {
    //        _databaseService = databaseService;
    //    }

    //    public async Task<T> CreateClientAsync<T>()
    //    {
    //        var serverUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
    //        if (string.IsNullOrWhiteSpace(serverUri))
    //        {
    //            throw new Exception("Сервер не настроен!");
    //        }

    //        var handler = new ApiKeyHandler
    //        {
    //            InnerHandler = new HttpClientHandler
    //            {
    //                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
    //            }
    //        };

    //        var httpClient = new HttpClient(handler)
    //        {
    //            BaseAddress = new Uri(serverUri)
    //        };

    //        return RestService.For<T>(httpClient);
    //    }
    //}
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
