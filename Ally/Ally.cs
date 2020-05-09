using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Linq;
using System.Net;
using System.Threading;

namespace Ally
{
    public class AllyApi
    {
        readonly static string uri = "https://api.tradeking.com/v1";
        readonly static string stream_uri = "https://stream.tradeking.com/v1";
        string consumerKey, consumerSecret, OAuthToken, OAuthSecret;

        RestClient client = null;

        public enum OrderState : int
        {
            Working = 0,
            Executed = 2,
            Canceled = 4,
            Rejected = 8,
            Pending = 9
        }

        [XmlRoot(ElementName = "Instrmt", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
        public class Instrmt
        {
            [XmlAttribute(AttributeName = "Desc")]
            public string Desc { get; set; }
            [XmlAttribute(AttributeName = "SecTyp")]
            public string SecTyp { get; set; }
            [XmlAttribute(AttributeName = "Sym")]
            public string Sym { get; set; }
        }

        [XmlRoot(ElementName = "OrdQty", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
        public class OrdQty
        {
            [XmlAttribute(AttributeName = "Qty")]
            public double Qty { get; set; }
        }

        [XmlRoot(ElementName = "Comm", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
        public class Comm
        {
            [XmlAttribute(AttributeName = "Comm")]
            public double _Comm { get; set; }
        }

        [XmlRoot(ElementName = "ExecRpt", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
        public class ExecRpt
        {
            [XmlElement(ElementName = "Instrmt", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
            public Instrmt Instrmt { get; set; }
            [XmlElement(ElementName = "OrdQty", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
            public OrdQty OrdQty { get; set; }
            [XmlElement(ElementName = "Comm", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
            public Comm Comm { get; set; }
            [XmlAttribute(AttributeName = "TxnTm")]
            public DateTime TxnTm { get; set; }
            [XmlAttribute(AttributeName = "TrdDt")]
            public DateTime TrdDt { get; set; }
            [XmlAttribute(AttributeName = "LeavesQty")]
            public double LeavesQty { get; set; }
            [XmlAttribute(AttributeName = "TmInForce")]
            public string TmInForce { get; set; }
            [XmlAttribute(AttributeName = "Px")]
            public double Px { get; set; }
            [XmlAttribute(AttributeName = "Typ")]
            public int Typ { get; set; }
            [XmlAttribute(AttributeName = "Side")]
            public int Side { get; set; }
            [XmlAttribute(AttributeName = "AcctTyp")]
            public int AcctTyp { get; set; }
            [XmlAttribute(AttributeName = "Acct")]
            public string Acct { get; set; }
            [XmlAttribute(AttributeName = "Stat")]
            public int Stat { get; set; }
            [XmlAttribute(AttributeName = "ID")]
            public string ID { get; set; }
            [XmlAttribute(AttributeName = "OrdID")]
            public string OrdID { get; set; }
        }

        [XmlRoot(ElementName = "FIXML", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
        public class FIXML
        {
            [XmlElement(ElementName = "ExecRpt", Namespace = "http://www.fixprotocol.org/FIXML-5-0-SP2")]
            public ExecRpt ExecRpt { get; set; }
            [XmlAttribute(AttributeName = "xmlns")]
            public string Xmlns { get; set; }
        }

        public class Quote
        {
            public double adp_100 { get; set; }
            public double adp_200 { get; set; }
            public double adp_50 { get; set; }
            public UInt64 adv_21 { get; set; }
            public UInt64 adv_30 { get; set; }
            public UInt64 adv_90 { get; set; }
            public double ask { get; set; }
            public DateTime ask_time { get; set; }
            public int asksz { get; set; }
            public string basis { get; set; }
            public double beta { get; set; }
            public double bid { get; set; }
            public DateTime bid_time { get; set; }
            public int bidsz { get; set; }
            public string bidtick { get; set; }
            public double chg { get; set; }
            public string chg_sign { get; set; }
            public string chg_t { get; set; }
            public double cl { get; set; }
            public string contract_size { get; set; }
            public string cusip { get; set; }
            public DateTime date { get; set; }
            public DateTime datetime { get; set; }
            public string days_to_expiration { get; set; }
            public double div { get; set; }
            public string divexdate { get; set; }
            public string divfreq { get; set; }
            public string divpaydt { get; set; }
            public double dollar_value { get; set; }
            public double eps { get; set; }
            public string exch { get; set; }
            public string exch_desc { get; set; }
            public double hi { get; set; }
            public double iad { get; set; }
            public string idelta { get; set; }
            public string igamma { get; set; }
            public double imp_volatility { get; set; }
            public double incr_vl { get; set; }
            public string irho { get; set; }
            public string issue_desc { get; set; }
            public double itheta { get; set; }
            public double ivega { get; set; }
            public double last { get; set; }
            public double lo { get; set; }
            public string name { get; set; }
            public string op_delivery { get; set; }
            public int op_flag { get; set; }
            public string op_style { get; set; }
            public string op_subclass { get; set; }
            public string openinterest { get; set; }
            public double opn { get; set; }
            public string opt_val { get; set; }
            public double pchg { get; set; }
            public string pchg_sign { get; set; }
            public double pcls { get; set; }
            public double pe { get; set; }
            public double phi { get; set; }
            public double plo { get; set; }
            public double popn { get; set; }
            public double pr_adp_100 { get; set; }
            public double pr_adp_200 { get; set; }
            public double pr_adp_50 { get; set; }
            public DateTime pr_date { get; set; }
            public string pr_openinterest { get; set; }
            public double prbook { get; set; }
            public double prchg { get; set; }
            public string prem_mult { get; set; }
            public string put_call { get; set; }
            public UInt64 pvol { get; set; }
            public int qcond { get; set; }
            public string rootsymbol { get; set; }
            public string secclass { get; set; }
            public string sesn { get; set; }
            public string sho { get; set; }
            public string strikeprice { get; set; }
            public string symbol { get; set; }
            public string tcond { get; set; }
            public long timestamp { get; set; }
            public string tr_num { get; set; }
            public string tradetick { get; set; }
            public string trend { get; set; }
            public string under_cusip { get; set; }
            public string undersymbol { get; set; }
            public UInt64 vl { get; set; }
            public double volatility12 { get; set; }
            public double vwap { get; set; }
            public double wk52hi { get; set; }
            public DateTime wk52hidate { get; set; }
            public double wk52lo { get; set; }
            public DateTime wk52lodate { get; set; }
            public string xdate { get; set; }
            public string xday { get; set; }
            public string xmonth { get; set; }
            public string xyear { get; set; }
            public double yield { get; set; }
        }

        public class Security
        {
            public string cusip { get; set; }
            public string id { get; set; }
            public string sectyp { get; set; }
            public string sym { get; set; }
        }

        public class Account
        {
            public class BuyingPower
            {
                public double cashavailableforwithdrawal { get; set; }
                public int daytrading { get; set; }
                public int equitypercentage { get; set; }
                public int options { get; set; }
                public int soddaytrading { get; set; }
                public int sodoptions { get; set; }
                public int sodstock { get; set; }
                public int stock { get; set; }
            }

            public class Money
            {
                public double accruedinterest { get; set; }
                public double cash { get; set; }
                public double cashavailable { get; set; }
                public double marginbalance { get; set; }
                public double mmf { get; set; }
                public double total { get; set; }
                public double uncleareddeposits { get; set; }
                public double unsettledfunds { get; set; }
                public double yield { get; set; }
            }

            public class Securities
            {
                public int longoptions { get; set; }
                public int longstocks { get; set; }
                public int options { get; set; }
                public int shortoptions { get; set; }
                public int shortstocks { get; set; }
                public int stocks { get; set; }
                public int total { get; set; }
            }

            public class AccountBalance
            {
                public string account { get; set; }
                public double accountvalue { get; set; }
                public BuyingPower buyingpower { get; set; }
                public int fedcall { get; set; }
                public int housecall { get; set; }
                public Money money { get; set; }
                public Securities securities { get; set; }
            }

            public class DisplayData
            {
                public string totalsecurities { get; set; }
            }

            public class AccountHoldings
            {
                public DisplayData displaydata { get; set; }
                public List<Holding> holding { get; set; }
                public double totalsecurities { get; set; }
            }

            public class Instrument
            {
                public string cusip { get; set; }
                public string desc { get; set; }
                public string factor { get; set; }
                public string sectyp { get; set; }
                public string sym { get; set; }
            }

            public class Quote
            {
                public double change { get; set; }
                public double lastprice { get; set; }
            }

            public class HInstrument
            {
                public string cfi { get; set; }
                public string cusip { get; set; }
                public string desc { get; set; }
                public int factor { get; set; }
                public DateTime matdt { get; set; }
                public object mmy { get; set; }
                public double mult { get; set; }
                public double putcall { get; set; }
                public string sectyp { get; set; }
                public double strkpx { get; set; }
                public string sym { get; set; }
            }


            public class HExtendedQuote
            {
                public string dividenddata { get; set; }
            }

            public class HQuote
            {
                public string change { get; set; }
                public HExtendedQuote extendedquote { get; set; }
                public string format { get; set; }
                public double lastprice { get; set; }
            }

            public class Holding
            {
                public int accounttype { get; set; }
                public double costbasis { get; set; }
                public double gainloss { get; set; }
                public HInstrument instrument { get; set; }
                public double marketvalue { get; set; }
                public double marketvaluechange { get; set; }
                public double price { get; set; }
                public double purchaseprice { get; set; }
                public double qty { get; set; }
                public HQuote quote { get; set; }
                public int sodcostbasis { get; set; }
                public string underlying { get; set; }
            }

            public string account { get; set; }
            public AccountBalance accountbalance { get; set; }
            public AccountHoldings accountholdings { get; set; }
        }

        public class Transaction
        {
            public int accounttype { get; set; }
            public double commission { get; set; }
            public string description { get; set; }
            public double fee { get; set; }
            public double price { get; set; }
            public double quantity { get; set; }
            public double secfee { get; set; }
            public Security security { get; set; }
            public DateTime settlementdate { get; set; }
            public int side { get; set; }
            public string source { get; set; }
            public DateTime tradedate { get; set; }
            public string transactionid { get; set; }
            public string transactiontype { get; set; }
        }

        public class StreamQuote
        {
            public double ask { get; set; }
            public long asksz { get; set; }
            public double bid { get; set; }
            public long bidsz { get; set; }
            public DateTime datetime { get; set; }
            public string qcond { get; set; }
            public string symbol { get; set; }
            public long timestamp { get; set; }
        }

        public class StreamTrade
        {
            public string cvol { get; set; }
            public DateTime datetime { get; set; }
            public double last { get; set; }
            public string symbol { get; set; }
            public long timestamp { get; set; }
            public long vl { get; set; }
            public double vwap { get; set; }
        }

        public AllyApi(string consumerKey, string consumerSecret, string OAuthToken, string OAuthSecret)
        {
            this.consumerKey = consumerKey;
            this.consumerSecret = consumerSecret;
            this.OAuthToken = OAuthToken;
            this.OAuthSecret = OAuthSecret;

            client = new RestClient(uri) { Authenticator = OAuth1Authenticator.ForAccessToken(consumerKey, consumerSecret, OAuthToken, OAuthSecret) };
        }

        public async Task<List<Account>> GetAccounts()
        {
            var request = new RestRequest("/accounts.json");
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    var accounts = JsonConvert.DeserializeObject<List<Account>>(o["response"]["accounts"]["accountsummary"].ToString(), new JsonSerializerSettings
                    {
                        Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                    });

                    accounts.Sort((x, y) => x.account.CompareTo(y.account));
                    return accounts;
                }

                throw new Exception((string)o["response"]["error"]);
            }

            return null;
        }

        public async Task<List<Account.Holding>> GetHoldings(string id)
        {
            var request = new RestRequest("/accounts/{id}/holdings.json");
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;
            request.AddUrlSegment("id", id);

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    var result = new List<Account.Holding>();

                    if (o["response"]["accountholdings"] != null && o["response"]["accountholdings"]["holding"] != null)
                    {
                        try
                        {
                            result = JsonConvert.DeserializeObject<List<Account.Holding>>(o["response"]["accountholdings"]["holding"].ToString());
                        }
                        catch (Exception)
                        {
                            result.Add(JsonConvert.DeserializeObject<Account.Holding>(o["response"]["accountholdings"]["holding"].ToString(), new JsonSerializerSettings
                            {
                                Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                            }));
                        }
                    }

                    return result;
                }

                throw new Exception((string)o["response"]["error"]);
            }

            return null;
        }

        public async Task<List<Transaction>> GetHistory(string id)
        {
            var request = new RestRequest("accounts/{id}/history.json");
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;
            request.AddUrlSegment("id", id);

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    var result = new List<Transaction>();

                    try
                    {
                        foreach (var t in (JArray)o["response"]["transactions"]["transaction"])
                        {
                            if ((string)t["activity"] == "Trade")
                            {
                                result.Add(JsonConvert.DeserializeObject<Transaction>(t["transaction"].ToString(), new JsonSerializerSettings
                                {
                                    Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                                }));
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                    return result;
                }

                throw new Exception((string)o["response"]["error"]);
            }

            return null;
        }

        public async Task<bool> Stream(List<string> symbols, Action<StreamQuote> quote_callback, Action<StreamTrade> trade_callback, SynchronizationContext sc, CancellationToken ct)
        {
            var sclient = new RestClient(stream_uri) { Authenticator = OAuth1Authenticator.ForAccessToken(consumerKey, consumerSecret, OAuthToken, OAuthSecret) };

            var request = new RestRequest("/market/quotes.json");
            request.AddQueryParameter("symbols", string.Join(",", symbols));
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;

            request.ResponseWriter = (ms) =>
            {
                try
                {
                    using (var sr = new StreamReader(ms))
                    {
                        while (!sr.EndOfStream && (ct == null || !ct.IsCancellationRequested))
                        {
                            string data = string.Empty;

                            int c = 0;
                            int count = 0;

                            while ((c = sr.Read()) != -1)
                            {
                                data += (char)c;

                                if (c == '{')
                                    count++;
                                else if (c == '}')
                                    count--;

                                if (count == 0)
                                    break;
                            }

                            try
                            {
                                var o = JObject.Parse(data);

                                if (o.ContainsKey("quote"))
                                {
                                    var j = JsonConvert.DeserializeObject<StreamQuote>(o["quote"].ToString(), new JsonSerializerSettings
                                    {
                                        Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                                    });

                                    if (sc != null)
                                    {
                                        sc.Post((t) => quote_callback(j), null);
                                    }
                                    else
                                    {
                                        quote_callback(j);
                                    }
                                }
                                else if (o.ContainsKey("trade"))
                                {
                                    var j = JsonConvert.DeserializeObject<StreamTrade>(o["trade"].ToString(), new JsonSerializerSettings
                                    {
                                        Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                                    });

                                    if (sc != null)
                                    {
                                        sc.Post((t) => trade_callback(j), null);
                                    }
                                    else
                                    {
                                        trade_callback(j);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            };

            sclient.ConfigureWebRequest((r) =>
            {
                r.ServicePoint.Expect100Continue = true;
                r.KeepAlive = true;
            });

            var response = await sclient.ExecuteAsync(request, ct);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<List<Quote>> GetQuote(List<string> symbols)
        {
            if (symbols.Count == 1)
                return new List<Quote>() { await GetQuote(symbols.First()) };

            var request = new RestRequest("/market/ext/quotes.json");
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;

            request.AddQueryParameter("symbols", string.Join(",", symbols));
            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    return JsonConvert.DeserializeObject<List<Quote>>(o["response"]["quotes"]["quote"].ToString(), new JsonSerializerSettings
                    {
                        Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                    });
                }

                throw new Exception((string)o["response"]["error"]);
            }

            return null;
        }

        public async Task<Quote> GetQuote(string symbol)
        {
            var request = new RestRequest("/market/ext/quotes.json");
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;

            request.AddQueryParameter("symbols", symbol);
            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    return JsonConvert.DeserializeObject<Quote>(o["response"]["quotes"]["quote"].ToString(), new JsonSerializerSettings
                    {
                        Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                    });
                }

                throw new Exception((string)o["response"]["error"]);
            }

            return null;
        }

        public async Task<List<ExecRpt>> GetOrders(string id)
        {
            var request = new RestRequest("/accounts/{id}/orders.json");
            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;

            request.AddUrlSegment("id", id);
            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    var result = new List<ExecRpt>();

                    if (o["response"]["orderstatus"] != null && o["response"]["orderstatus"]["order"] != null)
                    {
                        try
                        {
                            foreach (JObject order in (JArray)o["response"]["orderstatus"]["order"])
                            {
                                string fixmlmessage = (string)order["fixmlmessage"];

                                var fixml = new FIXML();
                                XmlSerializer serializer = new XmlSerializer(typeof(FIXML));
                                fixml = (FIXML)serializer.Deserialize(new StringReader(fixmlmessage));

                                result.Add(fixml.ExecRpt);
                            }
                        }
                        catch (Exception)
                        {
                            JObject order = (JObject)o["response"]["orderstatus"]["order"];
                            string fixmlmessage = (string)order["fixmlmessage"];

                            var fixml = new FIXML();
                            XmlSerializer serializer = new XmlSerializer(typeof(FIXML));
                            fixml = (FIXML)serializer.Deserialize(new StringReader(fixmlmessage));

                            result.Add(fixml.ExecRpt);
                        }
                    }

                    var final = new List<ExecRpt>();

                    foreach (var ord in result)
                    {
                        ord.Acct = ord.Acct.Remove(ord.Acct.Length - 1, 1);

                        // Canceled
                        if (ord.Stat != 4)
                        {
                            final.Add(ord);
                        }
                    }

                    final.Sort((x, y) => y.TrdDt.CompareTo(x.TrdDt));

                    return final;
                }

                throw new Exception((string)o["response"]["error"]);
            }

            return null;
        }

        public class StringWriterWithEncoding : StringWriter
        {
            public StringWriterWithEncoding(StringBuilder sb, Encoding encoding) : base(sb)
            {
                this.m_Encoding = encoding;
            }
            private readonly Encoding m_Encoding;
            public override Encoding Encoding
            {
                get
                {
                    return this.m_Encoding;
                }
            }
        }

        string XmlToString(XmlDocument doc)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.Indent = true;

            var sb = new StringBuilder();
            using (var stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8))
            using (var xmlTextWriter = XmlWriter.Create(stringWriter, settings))
            {
                doc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }
        }

        public async Task<bool> CancelOrder(ExecRpt order)
        {
            var xml = new XmlDocument();
            var root = xml.CreateElement("FIXML", "http://www.fixprotocol.org/FIXML-5-0-SP2");
            xml.AppendChild(root);

            var cxl = xml.CreateElement("OrdCxlReq", xml.DocumentElement.NamespaceURI);
            var attr = xml.CreateAttribute("OrigID");
            attr.Value = order.ID;
            cxl.Attributes.Append(attr);

            attr = xml.CreateAttribute("Acct");
            attr.Value = order.Acct + "1";
            cxl.Attributes.Append(attr);

            attr = xml.CreateAttribute("Side");
            attr.Value = Convert.ToString(order.Side);
            cxl.Attributes.Append(attr);

            attr = xml.CreateAttribute("Typ");
            attr.Value = Convert.ToString(order.Typ);
            cxl.Attributes.Append(attr);

            attr = xml.CreateAttribute("TmInForce");
            attr.Value = order.TmInForce;
            cxl.Attributes.Append(attr);

            var instrmt = xml.CreateElement("Instrmt", xml.DocumentElement.NamespaceURI);
            attr = xml.CreateAttribute("SecTyp");
            attr.Value = order.Instrmt.SecTyp;
            instrmt.Attributes.Append(attr);

            attr = xml.CreateAttribute("Sym");
            attr.Value = order.Instrmt.Sym;
            instrmt.Attributes.Append(attr);

            var OrdQty = xml.CreateElement("OrdQty", xml.DocumentElement.NamespaceURI);
            attr = xml.CreateAttribute("Qty");
            attr.Value = Convert.ToString(order.OrdQty.Qty);
            OrdQty.Attributes.Append(attr);

            cxl.AppendChild(instrmt);
            cxl.AppendChild(OrdQty);
            root.AppendChild(cxl);

            var request = new RestRequest("/accounts/{id}/orders.json", Method.POST);
            request.AddHeader("TKI_OVERRIDE", "true");
            request.AddUrlSegment("id", order.Acct);
            request.AddParameter("application/xml", XmlToString(xml), ParameterType.RequestBody);
            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> CreateOrder(ExecRpt exec)
        {
            var xml = new XmlDocument();
            var root = xml.CreateElement("FIXML", "http://www.fixprotocol.org/FIXML-5-0-SP2");
            xml.AppendChild(root);

            var order = xml.CreateElement("Order", xml.DocumentElement.NamespaceURI);

            var attr = xml.CreateAttribute("Side");
            attr.Value = Convert.ToString(exec.Side);   // buy/sell
            order.Attributes.Append(attr);

            attr = xml.CreateAttribute("Acct");
            attr.Value = exec.Acct;
            order.Attributes.Append(attr);

            attr = xml.CreateAttribute("Typ");
            attr.Value = Convert.ToString(exec.Typ); // order type
            order.Attributes.Append(attr);

            if (exec.Typ == 2)    // limit
            {
                attr = xml.CreateAttribute("Px");
                attr.Value = Convert.ToString(exec.Px);
                order.Attributes.Append(attr);
            }

            attr = xml.CreateAttribute("TmInForce");
            attr.Value = exec.TmInForce;
            order.Attributes.Append(attr);

            var instrmt = xml.CreateElement("Instrmt", xml.DocumentElement.NamespaceURI);
            attr = xml.CreateAttribute("SecTyp");
            attr.Value = exec.Instrmt.SecTyp;
            instrmt.Attributes.Append(attr);

            attr = xml.CreateAttribute("Sym");
            attr.Value = exec.Instrmt.Sym;
            instrmt.Attributes.Append(attr);

            var OrdQty = xml.CreateElement("OrdQty", xml.DocumentElement.NamespaceURI);
            attr = xml.CreateAttribute("Qty");
            attr.Value = Convert.ToString(exec.OrdQty.Qty);
            OrdQty.Attributes.Append(attr);

            order.AppendChild(instrmt);
            order.AppendChild(OrdQty);
            root.AppendChild(order);

            var request = new RestRequest("/accounts/{id}/orders.json", Method.POST);
            request.AddHeader("TKI_OVERRIDE", "true");
            request.AddUrlSegment("id", exec.Acct);
            request.AddParameter("application/xml", XmlToString(xml), ParameterType.RequestBody);
            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<ExecRpt> CreateOrder(string account, string symbol, double quantity, double price, bool gtc = false)
        {
            var xml = new XmlDocument();
            var root = xml.CreateElement("FIXML", "http://www.fixprotocol.org/FIXML-5-0-SP2");
            xml.AppendChild(root);

            var order = xml.CreateElement("Order", xml.DocumentElement.NamespaceURI);

            var attr = xml.CreateAttribute("Side");
            attr.Value = quantity > 0 ? "1" : "2";   // buy/sell
            order.Attributes.Append(attr);

            attr = xml.CreateAttribute("Acct");
            attr.Value = account;
            order.Attributes.Append(attr);

            attr = xml.CreateAttribute("Typ");
            attr.Value = price > 0 ? "2" : "1"; // order type
            order.Attributes.Append(attr);

            if (price > 0)
            {
                attr = xml.CreateAttribute("Px");
                attr.Value = Convert.ToString(price);
                order.Attributes.Append(attr);
            }

            attr = xml.CreateAttribute("TmInForce");
            attr.Value = gtc ? "1" : "0";
            order.Attributes.Append(attr);

            var instrmt = xml.CreateElement("Instrmt", xml.DocumentElement.NamespaceURI);
            attr = xml.CreateAttribute("SecTyp");
            attr.Value = "CS";
            instrmt.Attributes.Append(attr);

            attr = xml.CreateAttribute("Sym");
            attr.Value = symbol.ToUpper();
            instrmt.Attributes.Append(attr);

            var OrdQty = xml.CreateElement("OrdQty", xml.DocumentElement.NamespaceURI);
            attr = xml.CreateAttribute("Qty");
            attr.Value = Convert.ToString(Math.Abs(quantity));
            OrdQty.Attributes.Append(attr);

            order.AppendChild(instrmt);
            order.AppendChild(OrdQty);
            root.AppendChild(order);

            var request = new RestRequest("/accounts/{id}/orders.json", Method.POST);
            request.AddHeader("TKI_OVERRIDE", "true");
            request.AddUrlSegment("id", account);
            request.AddParameter("application/xml", XmlToString(xml), ParameterType.RequestBody);
            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject o = JObject.Parse(response.Content);

                if ((string)o["response"]["error"] == "Success")
                {
                    string order_id = (string)o["response"]["clientorderid"];

                    var exec = new ExecRpt();
                    exec.TrdDt = DateTime.Now;
                    exec.OrdID = order_id;
                    exec.ID = order_id;
                    exec.Stat = (int)OrderState.Pending;
                    exec.Side = quantity > 0 ? 1 : 2;   // buy / sell
                    exec.Typ = price > 0 ? 1 : 2;       // limit
                    if (price > 0)
                        exec.Px = price;                // limit price
                    exec.TmInForce = gtc ? "1" : "0";   // duration
                    exec.Acct = account;

                    exec.Instrmt = new Instrmt();
                    exec.Instrmt.Sym = symbol;
                    exec.Instrmt.SecTyp = "CS";

                    exec.OrdQty = new OrdQty();
                    exec.OrdQty.Qty = Math.Abs(quantity);

                    return exec;
                }
            }

            return null;
        }
    }
}
