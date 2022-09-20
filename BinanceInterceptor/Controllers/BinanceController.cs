using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using RestSharp;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;


namespace BinanceInterceptor.Controllers
{
    [Route("api/v3")]
    [ApiController]
    public class BinanceController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;

        public BinanceController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        #region BINANCE.US ENDPOINTS
        //These are replicated API endpoints with data that is pulled from api.binance.us.
        //The RefreshBinanceValuesServices.cs polls the binance api endpoint and saves the data
        //to memory cache, which is then exposed here for clients to read

        [HttpGet]
        [Route("us/ticker/24hr")]
        public ActionResult Ticker([FromQuery] string symbol)
        {
            return GetResultFromMemoryCacheAsync("usTicker");
        }

        [HttpGet]
        [Route("us/exchangeInfo")]
        public ActionResult Exchange()
        {
            return GetResultFromMemoryCacheAsync("usExchangeInfo");
        }

        [HttpGet]
        [Route("us/ping")]
        public ActionResult Ping()
        {
            return GetResultFromMemoryCacheAsync("usPing");
        }

        [HttpGet]
        [Route("us/depth")]
        public ActionResult Depth([FromQuery] string symbol, int limit)
        {
            return GetResultFromMemoryCacheAsync("usDepthBtcUsdt");
        }

        [HttpGet]
        [Route("us/time")]
        public ActionResult TimeAsync()
        {
            return GetResultFromMemoryCacheAsync("usTime");
        }
        #endregion


        #region BINANCE.COM ENDPOINTS
        //These are replicated API endpoints with data that is pulled from api.binance.com.
        //The RefreshBinanceValuesServices.cs polls the binance api endpoint and saves the data
        //to memory cache, which is then exposed here for clients to read

        [HttpGet]
        [Route("com/ticker/24hr")]
        public ActionResult ComTicker([FromQuery] string symbol)
        {
            return GetResultFromMemoryCacheAsync("comTicker");
        }

        [HttpGet]
        [Route("com/exchangeInfo")]
        public ActionResult ComExchange()
        {
            return GetResultFromMemoryCacheAsync("comExchangeInfo");
        }

        [HttpGet]
        [Route("com/ping")]
        public ActionResult ComPing()
        {
            return GetResultFromMemoryCacheAsync("comPing");
        }

        [HttpGet]
        [Route("com/depth")]
        public ActionResult ComDepth([FromQuery] string symbol, int limit)
        {
            return GetResultFromMemoryCacheAsync("comDepthBtcUsdt");
        }

        [HttpGet]
        [Route("com/time")]
        public ActionResult ComTimeAsync()
        {
            return GetResultFromMemoryCacheAsync("comTime");
        }
        #endregion

        private ActionResult GetResultFromMemoryCacheAsync(string cacheKey)
        {
            _memoryCache.TryGetValue(cacheKey, out RestResponse cachedHttpResponse);
            if (cachedHttpResponse != null)
            {
                foreach (var header in cachedHttpResponse.Headers)
                {
                    Response.Headers.Add(header.Name, header.Value.ToString());
                }
                Response.ContentType = "application/json;charset=UTF-8";
                Response.StatusCode = 200;
                return Content(cachedHttpResponse.Content);
            }
            return BadRequest();
        }

        //API endpoint that the clients connect to and recieve the re-streamed websocket data from binance
        //the websocket data from binance is being added to the memory cache in 'BinanceWebSocketServices.cs'
        //the clients read the websocket data from memory cache

        [HttpGet("/ws")]
        public async Task Get()
        {
            //Accept websocket request
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext
                {
                    DangerousEnableCompression = true,
                    DisableServerContextTakeover = true,
                    ServerMaxWindowBits = 15 
                });
                Debug.WriteLine(LogLevel.Information, "WebSocket connection established");

                //Start websocket data transfer
                await Echo(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task Echo(WebSocket webSocket)
        {
            //This code waits for the newly connected client to send data subscribing to certain streams
            //we dont care what they subscribe because we are only sending them the stream data we already got from binance
            //where we already subscribed to the data streams

            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            //Sending the messages that the original binance would when subscribing to streams
            //this is just because the client wont start processing the data unless we send them the below
            //subscribe result messages
            if (result.EndOfMessage)
            {
                string subscribeResult1 = "{\"result\": null, \"id\": 1}";
                string subscribeResult2 = "{\"result\": null, \"id\": 2}";

                await webSocket.SendAsync(GetBytes(subscribeResult1), WebSocketMessageType.Text, true, CancellationToken.None);
                await webSocket.SendAsync(GetBytes(subscribeResult2), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            Debug.WriteLine(LogLevel.Information, "Message received from Client, starting Websocket conneciton");

            MemoryStream lastBinanceDataStream = new MemoryStream();

            //while websocket connection is open, keep checking the asp.net global memory cache to see if the 'binanceStreamData' 
            //has changed. If it did, then send it to the connected client
            while (!result.CloseStatus.HasValue)
            {

                _memoryCache.TryGetValue("binanceStreamData", out MemoryStream binanceStreamData);
                if(binanceStreamData != lastBinanceDataStream)
                {
                    await webSocket.SendAsync(binanceStreamData.GetBuffer(), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                lastBinanceDataStream = binanceStreamData;

            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

    }
}
