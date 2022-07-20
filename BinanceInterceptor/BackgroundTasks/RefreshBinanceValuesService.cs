using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using BinanceInterceptor.Models;

namespace BinanceInterceptor.BackgroundTasks
{
    public class RefreshBinanceValuesService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;

        private HttpClient _httpClient;
        public RefreshBinanceValuesService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _httpClient = _httpClientFactory.CreateClient();
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(4));

            List<ApiEndpoint> binanceEndpoints = new List<ApiEndpoint>();
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comexchangeInfo", Url = "https://api.binance.us/api/v3/exchangeInfo" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });
            binanceEndpoints.Add(new ApiEndpoint { MemoryCacheKey = "comTicker", Url = "https://api.binance.us/api/v3/ticker/24hr" });

            while (await timer.WaitForNextTickAsync())
            {
                Debug.WriteLine("Making binance calls...");

                //Call all binance endpoints and store result in memory
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.binance.us/api/v3/ticker/24hr");
                var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var tickerResult = await httpResponseMessage.Content.ReadAsStringAsync();
                    //_memoryCache.Remove("ticker");
                    _memoryCache.Set("ticker", tickerResult);
                    //Debug.WriteLineLine("Ticker result: " + tickerResult);
                }
                else
                {
                    Debug.WriteLine("Error on calling ticker endpoint: " + httpResponseMessage);
                }

                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.binance.us/api/v3/exchangeInfo");
                httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var exchangeInfoResult = await httpResponseMessage.Content.ReadAsStringAsync();
                    //_memoryCache.Remove("exchangeInfo");
                    _memoryCache.Set("exchangeInfo", exchangeInfoResult);
                    //Debug.WriteLineLine("Exchange result: " + exchangeInfoResult);

                }
                else
                {
                    Debug.WriteLine("Error on calling exchangeInfo endpoint: " + httpResponseMessage);
                }

                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.binance.us/api/v3/ping");
                httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var pingResult = await httpResponseMessage.Content.ReadAsStringAsync();
                    //_memoryCache.Remove("ping");
                    _memoryCache.Set("ping", pingResult);
                    //Debug.WriteLineLine("Ping result: " + pingResult);

                }
                else
                {
                    Debug.WriteLine("Error on calling ping endpoint: " + httpResponseMessage);
                }

                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.binance.us/api/v3/depth?symbol=BTCUSDT");
                httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var snapshotDepthResult = await httpResponseMessage.Content.ReadAsStringAsync();
                    //_memoryCache.Remove("depth");
                    _memoryCache.Set("depth", snapshotDepthResult);
                    //Debug.WriteLineLine("Depth result: " + snapshotDepthResult);

                }
                else
                {
                    Debug.WriteLine("Error on calling depth endpoint: " + httpResponseMessage);
                }

                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.binance.us/api/v3/time");
                httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var serverTimeResult = await httpResponseMessage.Content.ReadAsStringAsync();
                    //_memoryCache.Remove("depth");
                    _memoryCache.Set("depth", serverTimeResult);
                    //Debug.WriteLineLine("Time result: " + serverTimeResult);
                }
                else
                {
                    Debug.WriteLine("Error on calling time endpoint: " + httpResponseMessage);
                }
            }
        }

        protected async Task GetApiDataAsync(string memoryCacheKey, string url)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var serverTimeResult = await httpResponseMessage.Content.ReadAsStringAsync();
                _memoryCache.Set(memoryCacheKey, serverTimeResult);
            }
            else
            {
                Debug.WriteLine("Error on calling time endpoint: " + httpResponseMessage);
            }
        }
    }
}
