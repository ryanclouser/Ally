using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Input;
using MahApps.Metro.Controls;
using System.Diagnostics;
using MahApps.Metro.Controls.Dialogs;
using Windows.UI.Notifications;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using PolygonApi;

namespace Ally
{
    public partial class MainWindow : MetroWindow
    {
        Settings settings = null;
        Mutex m = null;

        Thread th = null;
        CancellationTokenSource stream_cts = null;

        List<AllyApi.Account> accounts = new List<AllyApi.Account>();
        Dictionary<string, List<AllyApi.Account.Holding>> holdings = new Dictionary<string, List<AllyApi.Account.Holding>>();
        Dictionary<string, List<AllyApi.Transaction>> history = new Dictionary<string, List<AllyApi.Transaction>>();
        Dictionary<string, List<AllyApi.ExecRpt>> orders = new Dictionary<string, List<AllyApi.ExecRpt>>();

        AllyApi api = null;
        Polygon polygon = null;

        string CurrentSymbol = string.Empty;

        class UITrade
        {
            public string Account { get; set; }
            public string ID { get; set; }
            public string Type { get; set; }
            public string Symbol { get; set; }
            public double Price { get; set; }
            public double Shares { get; set; }
            public DateTime Date { get; set; }
            public AllyApi.ExecRpt Trade { get; set; }
            public string State { get; set; }
        }

        class UITS
        {
            public DateTime Date { get; set; }
            public double Price { get; set; }
            public double Shares { get; set; }
        }

        class Settings
        {
            public string consumerKey { get; set; }
            public string consumerSecret { get; set; }
            public string OAuthToken { get; set; }
            public string OAuthSecret { get; set; }
            public string Polygon { get; set; }
            public string Symbol { get; set; }

            public Settings()
            {
                Symbol = "ALLY";
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            bool created = false;
            m = new Mutex(true, "Ally", out created);

            if (!created)
            {
                Environment.Exit(1);
                return;
            }

            try
            {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(@"settings.json"));
            }
            catch (Exception)
            {
                settings = new Settings();
                File.WriteAllText(@"settings.json", JsonConvert.SerializeObject(settings, Formatting.Indented));
                Environment.Exit(1);
                return;
            }

            if (string.IsNullOrEmpty(settings.consumerKey) || string.IsNullOrEmpty(settings.consumerSecret) || string.IsNullOrEmpty(settings.OAuthToken) || string.IsNullOrEmpty(settings.OAuthSecret))
            {
                Environment.Exit(1);
                return;
            }

            Symbol.Text = settings.Symbol;
            CurrentSymbol = settings.Symbol;

            api = new AllyApi(settings.consumerKey, settings.consumerSecret, settings.OAuthToken, settings.OAuthSecret);

            if (!string.IsNullOrEmpty(settings.Polygon))
            {
                polygon = new Polygon(settings.Polygon);
            }

            th = new Thread(async () => await RefreshThread());
            th.Start();

            if (!string.IsNullOrEmpty(CurrentSymbol))
            {
                RefreshQuotes();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (stream_cts != null)
            {
                stream_cts.Cancel();
            }

            if (th != null && th.IsAlive)
            {
                th.Interrupt();
                th.Join();
            }

            if (m != null)
            {
                m.Dispose();
                m = null;
            }
        }

        void Toast(string title, string message)
        {
            var template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);

            var textNodes = template.GetElementsByTagName("text");
            textNodes.Item(0).InnerText = message;

            var notifier = ToastNotificationManager.CreateToastNotifier(title);
            var notification = new ToastNotification(template);
            notifier.Show(notification);
        }

        private async Task RefreshThread()
        {
            var sw = new Stopwatch();

            while (true)
            {
                try
                {
                    if (!sw.IsRunning || sw.ElapsedMilliseconds > 60000 || accounts == null || accounts.Count == 0)
                    {
                        await RefreshAccounts();
                        sw.Restart();
                    }

                    var t1 = RefreshHoldings();
                    var t2 = RefreshHistory();
                    var t3 = RefreshOrders();

                    await t1;
                    await t2;
                    await t3;

                    this.Invoke(() =>
                    {
                        RefreshUI();
                    });
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (Exception)
                {
                }

                await Task.Delay(2000);
            }
        }

        void QuoteA(AllyApi.StreamQuote q)
        {
            this.Invoke(() =>
            {
                if (q.symbol == CurrentSymbol)
                {
                    Buy.Content = string.Format("A {0:N2} x {1}\nBuy", q.ask, q.asksz);
                    Sell.Content = string.Format("B {0:N2} x {1}\nSell", q.bid, q.bidsz);

                    Bid.Content = string.Format("{0:N2}", q.bid);
                    Ask.Content = string.Format("{0:N2}", q.ask);
                }
            });
        }

        void TradeA(AllyApi.StreamTrade t)
        {
            this.Invoke(() =>
            {
                if (t.symbol == CurrentSymbol)
                {
                    TS.Items.Insert(0, new UITS() { Date = t.datetime, Price = t.last, Shares = t.vl });

                    if (TS.Items.Count > 1000)
                        TS.Items.RemoveAt(TS.Items.Count - 1);
                }
            });
        }

        static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // https://stackoverflow.com/questions/249760/how-can-i-convert-a-unix-timestamp-to-datetime-and-vice-versa
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        void QuoteP(Polygon.PolygonQuote q)
        {
            this.Invoke(() =>
            {
                if (q.sym == CurrentSymbol)
                {
                    Buy.Content = string.Format("A {0:N2} x {1}\nBuy", q.ap, q.as_);
                    Sell.Content = string.Format("B {0:N2} x {1}\nSell", q.bp, q.bs);

                    Bid.Content = string.Format("{0:N2}", q.bp);
                    Ask.Content = string.Format("{0:N2}", q.ap);
                }
            });

        }

        void TradeP(Polygon.PolygonTrade t)
        {
            this.Invoke(() =>
            {
                if (t.sym == CurrentSymbol)
                {
                    TS.Items.Insert(0, new UITS() { Date = UnixTimeStampToDateTime(t.t), Price = t.p, Shares = t.s });

                    if (TS.Items.Count > 1000)
                        TS.Items.RemoveAt(TS.Items.Count - 1);
                }
            });
        }

        async void RefreshQuotes()
        {
            if (stream_cts != null)
            {
                stream_cts.Cancel();
            }

            string symbol = string.Empty;
            this.Invoke(() =>
            {
                symbol = CurrentSymbol;
            });

            if (symbol.Length == 0 || symbol.Length > 5)
            {
                return;
            }

            stream_cts = new CancellationTokenSource();

            try
            {
                if (polygon != null)
                {
                    var q = await polygon.GetQuote(symbol);
                    this.BeginInvoke(() =>
                    {
                        if (q != null)
                        {
                            Buy.Content = string.Format("A {0:N2} x {1}\nBuy", q.askprice, q.asksize);
                            Sell.Content = string.Format("B {0:N2} x {1}\nSell", q.bidprice, q.bidsize);

                            Bid.Content = string.Format("{0:N2}", q.bidprice);
                            Ask.Content = string.Format("{0:N2}", q.askprice);
                        }

                        TS.Items.Clear();
                    });
                }
                else
                {
                    var q = await api.GetQuote(symbol);
                    this.BeginInvoke(() =>
                    {
                        if (q != null && q.symbol == CurrentSymbol)
                        {
                            Buy.Content = string.Format("A {0:N2} x {1}\nBuy", q.ask, q.asksz);
                            Sell.Content = string.Format("B {0:N2} x {1}\nSell", q.bid, q.bidsz);

                            Bid.Content = string.Format("{0:N2}", q.bid);
                            Ask.Content = string.Format("{0:N2}", q.ask);
                        }

                        TS.Items.Clear();
                    });
                }
            }
            catch (Exception)
            {
            }

            var token = stream_cts.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (polygon != null)
                    {
                        await polygon.Subscribe(new List<string>() { symbol }, new List<string>() { "T", "Q" }, QuoteP, TradeP, SynchronizationContext.Current, token);
                    }
                    else
                    {
                        await api.Stream(new List<string>() { symbol }, QuoteA, TradeA, SynchronizationContext.Current, token);
                    }
                }
                catch (Exception)
                {
                }

                await Task.Delay(5000);
            }
        }

        async Task RefreshAccounts()
        {
            var accounts = await api.GetAccounts();

            if (accounts != null)
            {
                this.Invoke(() =>
                {
                    this.accounts = accounts;

                    int idx = Account.SelectedIndex;
                    if (idx < 0) idx = 0;

                    if (Account.Items.Count == accounts.Count)
                    {
                        for (int x = 0; x < accounts.Count; ++x)
                        {
                            Account.Items[x] = accounts[x].account;
                        }
                    }
                    else
                    {
                        Account.Items.Clear();
                        foreach (var a in accounts)
                        {
                            Account.Items.Add(a.account);
                        }
                    }

                    Account.SelectedIndex = idx;
                });
            }
        }

        private AllyApi.Account GetSelectedAccount()
        {
            string id = Account.SelectedItem as string;

            if (accounts != null)
            {
                return accounts.Find(T => T.account == id);
            }

            return null;
        }

        private async Task RefreshHoldings()
        {
            if (accounts == null)
                return;

            foreach (var account in accounts)
            {
                try
                {
                    var holdings = await api.GetHoldings(account.account);

                    if (holdings != null)
                    {
                        this.Invoke(() =>
                        {
                            lock (this.holdings)
                            {
                                this.holdings[account.account] = holdings;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private async Task RefreshHistory()
        {
            if (accounts == null)
                return;

            foreach (var account in accounts)
            {
                try
                {
                    var history = await api.GetHistory(account.account);

                    if (history != null)
                    {
                        history.Sort((x, y) => y.tradedate.CompareTo(x.tradedate));

                        this.Invoke(() =>
                        {
                            lock (this.history)
                            {
                                this.history[account.account] = history;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private void RefreshUI()
        {
            var account = GetSelectedAccount();
            if (account != null)
            {
                lock (this.history)
                {
                    List<AllyApi.Transaction> history;
                    if (this.history.TryGetValue(account.account, out history))
                    {
                        if (history.Count != Trades.Items.Count)
                        {
                            Trades.Items.Clear();
                            foreach (var h in history)
                            {
                                var t = new UITrade() { Price = h.price, Shares = h.quantity, Symbol = h.security.sym, Date = h.tradedate, Type = (h.side == 1 ? "BUY" : "SELL") };
                                Trades.Items.Add(t);
                            }
                        }
                    }
                    else
                    {
                        Trades.Items.Clear();
                    }
                }

                lock (this.orders)
                {
                    List<AllyApi.ExecRpt> orders;
                    if (this.orders.TryGetValue(account.account, out orders))
                    {
                        if (orders.Count != Orders.Items.Count)
                        {
                            Orders.Items.Clear();
                            foreach (var o in orders)
                            {
                                var t = new UITrade() { Account = o.Acct, ID = o.OrdID, Price = o.Px, Shares = o.OrdQty.Qty, Symbol = o.Instrmt.Sym, Date = o.TrdDt, Type = (o.Side == 1 ? "BUY" : "SELL"), Trade = o };

                                if (o.Stat == 0)
                                {
                                    t.State = "Working";
                                }
                                else if (o.Stat == 2)
                                {
                                    t.State = "Executed";
                                }
                                else if (o.Stat == 4)
                                {
                                    t.State = "Canceled";
                                }
                                else if (o.Stat == 8)
                                {
                                    t.State = "Rejected";
                                }
                                else if (o.Stat == 9)
                                {
                                    t.State = "Pending";
                                }

                                Orders.Items.Add(t);
                            }
                        }
                        else
                        {
                            // Refresh state
                            for (int x = 0; x < orders.Count; ++x)
                            {
                                var o = orders[x];
                                var t = (Orders.Items[x] as UITrade);

                                if (o.Stat != t.Trade.Stat && (AllyApi.OrderState)o.Stat == AllyApi.OrderState.Executed)
                                {
                                    Toast("Ally", string.Format("Filled {0} for {1} @ {2:C2} on {3}", o.OrdQty.Qty, o.Instrmt.Sym, o.Px, o.Acct));
                                }

                                t.Trade = o;

                                if (o.Stat == 0)
                                {
                                    t.State = "Working";
                                }
                                else if (o.Stat == 2)
                                {
                                    t.State = "Executed";
                                }
                                else if (o.Stat == 4)
                                {
                                    t.State = "Canceled";
                                }
                                else if (o.Stat == 8)
                                {
                                    t.State = "Rejected";
                                }
                                else if (o.Stat == 9)
                                {
                                    t.State = "Pending";
                                }

                                Orders.Items[x] = t;
                            }

                            Orders.Items.Refresh();
                        }
                    }
                    else
                    {
                        Orders.Items.Clear();
                    }
                }

                lock (this.holdings)
                {
                    Position.Content = string.Empty;
                    List<AllyApi.Account.Holding> holdings;
                    if (this.holdings.TryGetValue(account.account, out holdings) && holdings != null)
                    {
                        foreach (var h in holdings)
                        {
                            if (h.instrument.sym == CurrentSymbol)
                            {
                                Position.Content = string.Format("{0} @ {1:C2} - {2:C2}", h.qty, h.purchaseprice, h.gainloss);

                                if (h.gainloss > 0)
                                    Position.Foreground = Brushes.Green;
                                else if (h.gainloss < 0)
                                    Position.Foreground = Brushes.Red;
                                else
                                    Position.Foreground = Brushes.Black;

                                break;
                            }
                        }
                    }
                }
            }
        }

        private async Task RefreshOrders()
        {
            if (accounts == null)
                return;

            foreach (var account in accounts)
            {
                try
                {
                    var orders = await api.GetOrders(account.account);

                    if (orders != null)
                    {
                        orders.Sort((x, y) => y.TrdDt.CompareTo(x.TrdDt));

                        this.Invoke(() =>
                        {
                            lock (this.orders)
                            {
                                this.orders[account.account] = orders;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private void Account_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var account = GetSelectedAccount();
            if (account != null)
            {
                AccountBalance.Content = string.Format("Value: {0:C2}", account.accountbalance.accountvalue);
                AccountUnsettled.Content = string.Format("Unsettled: {0:C2}", account.accountbalance.money.unsettledfunds);
                AccountAvailable.Content = string.Format("Available: {0:C2}", account.accountbalance.money.cashavailable - account.accountbalance.money.unsettledfunds);

                RefreshUI();
            }
            else
            {
                AccountBalance.Content = string.Empty;
                AccountUnsettled.Content = string.Empty;
                AccountAvailable.Content = string.Empty;
            }
        }

        private async void OrderCancel_Click(object sender, RoutedEventArgs e)
        {
            var account = GetSelectedAccount();

            if (account != null)
            {
                var o = Orders.SelectedItem as UITrade;

                if (o != null && o.Trade.Stat != (int)AllyApi.OrderState.Executed) // not executed
                {
                    if (await api.CancelOrder(o.Trade))
                    {
                        if (orders.ContainsKey(account.account))
                            orders[account.account].Remove(o.Trade);

                        RefreshUI();
                    }
                }
            }
        }

        private async void Buy_Click(object sender, RoutedEventArgs e)
        {
            var account = GetSelectedAccount();

            if (account != null)
            {
                string symbol = CurrentSymbol;
                double quantity = Convert.ToDouble(Quantity.Text);
                double price = Convert.ToDouble(Price.Text);
                bool gtc = (bool)GTC.IsChecked;
                bool limit = (bool)Limit.IsChecked;

                if (symbol.Length > 0 && symbol.Length < 8 && quantity > 0 && quantity < 100 && (price > 0 || !limit))
                {
                    if ((bool)Confirm.IsChecked)
                    {
                        string str = string.Format("Buy {0} of {1} at {2} for {3} on account {4}", quantity, symbol, limit ? string.Format("{0:C2}", price) : "market", gtc ? "GTC" : "Day", account.account);

                        var settings = new MetroDialogSettings();
                        settings.AffirmativeButtonText = "Confirm";

                        var result = await this.ShowMessageAsync(string.Format("Confirm {0} Trade", symbol), str, MessageDialogStyle.AffirmativeAndNegative, settings);
                        if (result != MessageDialogResult.Affirmative)
                        {
                            return;
                        }
                    }

                    var o = await api.CreateOrder(account.account, symbol, quantity, limit ? price : 0, gtc);

                    if (o != null)
                    {
                        lock (orders)
                        {
                            if (orders[account.account].Find(T => T.OrdID == o.OrdID) == null)
                                orders[account.account].Insert(0, o);
                        }

                        RefreshUI();
                    }
                    else
                    {
                        await this.ShowMessageAsync("Error", "Unable to place trade", MessageDialogStyle.Affirmative);
                    }
                }
            }
        }

        private async void Sell_Click(object sender, RoutedEventArgs e)
        {
            var account = GetSelectedAccount();

            if (account != null)
            {
                string symbol = CurrentSymbol;
                double quantity = Convert.ToDouble(Quantity.Text);
                double price = Convert.ToDouble(Price.Text);
                bool gtc = (bool)GTC.IsChecked;
                bool limit = (bool)Limit.IsChecked;

                if (symbol.Length > 0 && symbol.Length < 8 && quantity > 0 && quantity < 100 && (price > 0 || !limit))
                {
                    if ((bool)Confirm.IsChecked)
                    {
                        string str = string.Format("Sell {0} of {1} at {2} for {3} on account {4}", quantity, symbol, limit ? string.Format("{0:C2}", price) : "market", gtc ? "GTC" : "Day", account.account);

                        var settings = new MetroDialogSettings();
                        settings.AffirmativeButtonText = "Confirm";

                        var result = await this.ShowMessageAsync(string.Format("Confirm {0} Trade", symbol), str, MessageDialogStyle.AffirmativeAndNegative, settings);
                        if (result != MessageDialogResult.Affirmative)
                        {
                            return;
                        }
                    }

                    var o = await api.CreateOrder(account.account, symbol, -quantity, limit ? price : 0, gtc);

                    if (o != null)
                    {
                        lock (orders)
                        {
                            if (orders[account.account].Find(T => T.OrdID == o.OrdID) == null)
                                orders[account.account].Insert(0, o);
                        }

                        RefreshUI();
                    }
                    else
                    {
                        await this.ShowMessageAsync("Error", "Unable to place trade", MessageDialogStyle.Affirmative);
                    }
                }
            }
        }

        private void Symbol_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CurrentSymbol = Symbol.Text;
                RefreshQuotes();
            }
        }

        private void Bid_Click(object sender, RoutedEventArgs e)
        {
            Price.Text = ((string)Bid.Content).Replace("$", "");
        }

        private void Ask_Click(object sender, RoutedEventArgs e)
        {
            Price.Text = ((string)Ask.Content).Replace("$", "");
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
