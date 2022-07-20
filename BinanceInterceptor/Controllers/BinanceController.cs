using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BinanceInterceptor.Controllers
{
    [Route("api/v3")]
    [ApiController]
    public class BinanceController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;

        //public string tickerResult = "";
        //public string exchangeInfoResult = "";
        //public string pingResult = "";
        //public string snapshotDepthResult = "";
        //public string serverTimeResult = "";

        public BinanceController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }


        [HttpGet]
        [Route("ticker/24hr")]
        public string Ticker()
        {
            _memoryCache.TryGetValue("ticker", out string tickerResult);
            return tickerResult;
        }

        [HttpGet]
        [Route("exchangeInfo")]
        public string Exchange()
        {
            _memoryCache.TryGetValue("exchangeInfo", out string exchangeInfoResult);
            return exchangeInfoResult;
        }

        [HttpGet]
        [Route("ping")]
        public string Ping()
        {
            _memoryCache.TryGetValue("ping", out string pingResult);
            return pingResult;
        }

        [HttpGet]
        [Route("depth")]
        public string Depth([FromRoute] string symbol)
        {
            _memoryCache.TryGetValue("depth", out string snapshotDepthResult);
            return snapshotDepthResult;
        }

        [HttpGet]
        [Route("time")]
        public string Time()
        {
            _memoryCache.TryGetValue("time", out string serverTimeResult);
            return serverTimeResult;
        }

        [HttpGet("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                Debug.WriteLine(LogLevel.Information, "WebSocket connection established");
                await Echo(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.EndOfMessage)
            {
                string subscribeResult1 = "{\"result\": null, \"id\": 1}";
                string subscribeResult2 = "{\"result\": null, \"id\": 2}";

                await webSocket.SendAsync(GetBytes(subscribeResult1), WebSocketMessageType.Text, true, CancellationToken.None);
                await webSocket.SendAsync(GetBytes(subscribeResult2), WebSocketMessageType.Text, true, CancellationToken.None);

            }

            Debug.WriteLine(LogLevel.Information, "Message received from Client");
            //var timer = new PeriodicTimer(TimeSpan.FromSeconds(0.05));
            MemoryStream lastBinanceDataStream = new MemoryStream();

            while (!result.CloseStatus.HasValue)
            {
                //await timer.WaitForNextTickAsync();
                //_memoryCache.TryGetValue("binanceStreamData", out string binanceStreamData);
                //if(binanceStreamData != lastBinanceDataStream)
                //    await webSocket.SendAsync(GetBytes(binanceStreamData), WebSocketMessageType.Text, true, CancellationToken.None);

                _memoryCache.TryGetValue("binanceStreamData", out MemoryStream binanceStreamData);
                if(binanceStreamData != lastBinanceDataStream)
                {
                    await webSocket.SendAsync(binanceStreamData.GetBuffer(), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                lastBinanceDataStream = binanceStreamData;

                //var serverMsg = Encoding.UTF8.GetBytes($"Server: Hello. You said: {Encoding.UTF8.GetString(buffer)}");
                //await webSocket.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
                //_logger.Log(LogLevel.Information, "Message sent to Client");

                //result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                //_logger.Log(LogLevel.Information, "Message received from Client");

            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            //_logger.Log(LogLevel.Information, "WebSocket connection closed");
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

    }
}
