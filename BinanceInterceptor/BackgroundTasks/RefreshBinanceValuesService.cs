using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using BinanceInterceptor.Models;
using RestSharp;

namespace BinanceInterceptor.BackgroundTasks
{
    public class RefreshBinanceValuesService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;

        private RestClient _restClient;

        //private HttpClient _httpClient;
        public RefreshBinanceValuesService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            //_httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _restClient = new RestClient();
            _restClient.AddDefaultHeader("Content-Type", "application/x-www-form-urlencoded");

            //_httpClient = _httpClientFactory.CreateClient();

            List<ApiEndpoint> binanceEndpoints = new List<ApiEndpoint>();
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "usTicker", Url = "https://api.binance.us/api/v3/ticker/24hr?symbol=BTCUSDT" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "usExchangeInfo", Url = "https://api.binance.us/api/v3/exchangeInfo" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "usPing", Url = "https://api.binance.us/api/v3/ping" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "usDepthBtcUsdt", Url = "https://api.binance.us/api/v3/depth?symbol=BTCUSDT&limit=1000" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "usTime", Url = "https://api.binance.us/api/v3/time" });

            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.com/api/v3/ticker/24hr?symbol=BTCUSDT" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comExchangeInfo", Url = "https://api.binance.com/api/v3/exchangeInfo " });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comPing", Url = "https://api.binance.com/api/v3/ping" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comDepthBtcUsdt", Url = "https://api.binance.com/api/v3/depth?symbol=BTCUSDT&limit=1000" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTime", Url = "https://api.binance.com/api/v3/time" });

            var timer = new PeriodicTimer(TimeSpan.FromSeconds(4));
            while (await timer.WaitForNextTickAsync())
            {
                Debug.WriteLine("Making binance calls...");

                foreach(ApiEndpoint binanceEndpoint in binanceEndpoints)
                {
                    await GetApiDataAsync(binanceEndpoint);
                }
            }
        }

        protected async Task GetApiDataAsync(ApiEndpoint apiEndpoint)
        {
            //var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, apiEndpoint.Url);
            //var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);
            //var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);
            //var httpResponseMessage = await _httpClient.GetAsync(apiEndpoint.Url);

            var response = await _restClient.ExecuteAsync(new RestRequest { Method = Method.Get, Resource = apiEndpoint.Url });

            if (response.IsSuccessful)
            {
                //var result = await httpResponseMessage.Content.ReadAsStringAsync();
                //_memoryCache.Set(apiEndpoint.MemoryCacheKey, result);
                _memoryCache.Set(apiEndpoint.MemoryCacheKey, response);
                //Debug.WriteLine("Setting memory cache: " + apiEndpoint.MemoryCacheKey + "\nResult: " + result);
            }
            else
            {
                Debug.WriteLine("Error on calling time endpoint.\nStatus Code: " + response.StatusCode +
                    "\nMessage: " + response.ErrorMessage + "\n" + response);
            }

            //if (httpResponseMessage.IsSuccessStatusCode)
            //{
            //    //var result = await httpResponseMessage.Content.ReadAsStringAsync();
            //    //_memoryCache.Set(apiEndpoint.MemoryCacheKey, result);
            //    _memoryCache.Set(apiEndpoint.MemoryCacheKey, httpResponseMessage);
            //    //Debug.WriteLine("Setting memory cache: " + apiEndpoint.MemoryCacheKey + "\nResult: " + result);
            //}
            //else
            //{
            //    Debug.WriteLine("Error on calling time endpoint.\nStatus Code: " + httpResponseMessage.StatusCode +
            //        "\nMessage: " + httpResponseMessage.ReasonPhrase + "\n" + httpResponseMessage);
            //}
        }
    }
}
