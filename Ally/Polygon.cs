using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace PolygonApi
{
    class Polygon
    {
        readonly string api_uri = "https://api.polygon.io/v1";
        readonly string wss_uri = "wss://socket.polygon.io/stocks";
        string apiKey = string.Empty;

        RestClient client = null;

        public class PolygonTrade
        {
            public string ev { get; set; }
            public string sym { get; set; }
            public int x { get; set; }
            public string i { get; set; }
            public int z { get; set; }
            public double p { get; set; }
            public int s { get; set; }
            public List<int> c { get; set; }
            public long t { get; set; }
        }

        public class PolygonQuote
        {
            public string ev { get; set; }
            public string sym { get; set; }
            public string bx { get; set; }
            public double bp { get; set; }
            public int bs { get; set; }
            public string ax { get; set; }
            public double ap { get; set; }
            [JsonProperty("as")]
            public int as_ { get; set; }
            public int c { get; set; }
            public long t { get; set; }
        }

        public class LastQuote
        {
            public double askprice { get; set; }
            public int asksize { get; set; }
            public int askexchange { get; set; }
            public double bidprice { get; set; }
            public int bidsize { get; set; }
            public int bidexchange { get; set; }
            public long timestamp { get; set; }
        }

        public Polygon(string apiKey)
        {
            this.apiKey = apiKey;
            client = new RestClient(api_uri);
        }

        public async Task<LastQuote> GetQuote(string symbol)
        {
            var request = new RestRequest("/last_quote/stocks/{symbol}");
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;

            request.AddUrlSegment("symbol", symbol);
            request.AddQueryParameter("apiKey", apiKey);

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["status"] == "success")
                {
                    return JsonConvert.DeserializeObject<LastQuote>(o["last"].ToString());
                }

                throw new Exception((string)o["status"]);
            }

            return null;
        }

        // https://polygon.io/sockets
        public async Task Subscribe(List<string> symbols, List<string> channels, Action<PolygonQuote> quotes, Action<PolygonTrade> trades, SynchronizationContext sc, CancellationToken ct)
        {
            try
            {
                using (var ws = new ClientWebSocket())
                {
                    await ws.ConnectAsync(new Uri(wss_uri), ct);

                    JObject o = new JObject();
                    o["action"] = "auth";
                    o["params"] = apiKey;

                    byte[] bytes = Encoding.UTF8.GetBytes(o.ToString(Formatting.None));
                    await ws.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, ct);

                    o = new JObject();
                    o["action"] = "subscribe";

                    var arr = new List<string>(); 
                    foreach (var s in symbols)
                    {
                        foreach (var c in channels)
                        {
                            arr.Add(string.Format("{0}.{1}", c, s));
                        }
                    }
                    o["params"] = string.Join(',', arr);

                    bytes = Encoding.UTF8.GetBytes(o.ToString(Formatting.None));
                    await ws.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, ct);

                    byte[] buffer = new byte[8192];

                    while (ws.State == WebSocketState.Open)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            WebSocketReceiveResult result;

                            do
                            {
                                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                                ms.Write(buffer, 0, result.Count);
                            } while (!result.EndOfMessage);

                            if (result.MessageType == WebSocketMessageType.Text)
                            {
                                try
                                {
                                    JArray obj = JArray.Parse(Encoding.UTF8.GetString(ms.ToArray()));

                                    for (int x = 0; x < obj.Count; ++x)
                                    {
                                        if (obj[x]["ev"] != null)
                                        {
                                            if ((string)obj[x]["ev"] == "T")
                                            {
                                                var t = JsonConvert.DeserializeObject<PolygonTrade>(obj[x].ToString());

                                                if (sc != null)
                                                {
                                                    sc.Post((x) => trades(t), null);
                                                }
                                                else
                                                {
                                                    trades(t);
                                                }
                                            }
                                            else if ((string)obj[x]["ev"] == "Q")
                                            {
                                                var q = JsonConvert.DeserializeObject<PolygonQuote>(obj[x].ToString());

                                                if (sc != null)
                                                {
                                                    sc.Post((x) => quotes(q), null);
                                                }
                                                else
                                                {
                                                    quotes(q);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                            else
                            {
                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, ct);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

}