using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace BinanceInterceptor.BackgroundTasks
{
    public class BinanceWebSocketService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;

        public EventHandler WebsocketReceivedData;
        public BinanceWebSocketService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("BinanceWebSocketService ExecuteAsync: ");
            using (var socket = new ClientWebSocket())
            {
                string subscribeString1 = "{\"method\": \"SUBSCRIBE\", \"params\": [\"btcusdt@trade\"], \"id\": 1}";
                string subscribeString2 = "{\"method\": \"SUBSCRIBE\", \"params\": [\"btcusdt@depth@100ms\"], \"id\": 2}";

                Debug.WriteLine("Connecting Websocket client....");
                await socket.ConnectAsync(new Uri("wss://stream.binance.com:9443/ws"), CancellationToken.None);
                await Send(socket, subscribeString1);
                await Send(socket, subscribeString2);

                await Receive(socket, stoppingToken);

            }
        }

        static async Task Send(ClientWebSocket socket, string data) =>
            await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None);

        private async Task Receive(ClientWebSocket socket, CancellationToken stoppingToken)
        {
            Debug.WriteLine("In Receive Task of BinanceWebsocketService.cs");
            var buffer = new ArraySegment<byte>(new byte[2048]);
            while (!stoppingToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;

                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, stoppingToken);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);

                        //Debug.WriteLine(mes);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    ms.Seek(0, SeekOrigin.Begin);

                    _memoryCache.Set("binanceStreamData", ms);

                    //using (var reader = new StreamReader(ms, Encoding.UTF8))
                    //{
                    //    //var mes = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    //    //_memoryCache.Remove("binanceStreamData");
                    //    _memoryCache.Set("binanceStreamData", await reader.ReadToEndAsync());
                    //    //Debug.WriteLine(await reader.ReadToEndAsync());
                    //}
                }
            };
        }

        protected virtual void OnWebsocketReceivedData(EventArgs e)
        {
            EventHandler handler = WebsocketReceivedData;
            handler?.Invoke(this, e);
        }
    }
}
