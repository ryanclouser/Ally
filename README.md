Ally
===

[![Donate with Bitcoin](https://en.cryptobadges.io/badge/micro/12BMo7nBeBhDGDGagwqSRPAv3fkQi8nCfq)](https://en.cryptobadges.io/donate/12BMo7nBeBhDGDGagwqSRPAv3fkQi8nCfq)
[![Donate with Ethereum](https://en.cryptobadges.io/badge/micro/0xd163fdde358f9000A4E9290f23B84DFb6E9190D3)](https://en.cryptobadges.io/donate/0xd163fdde358f9000A4E9290f23B84DFb6E9190D3)
[![Donate with Litecoin](https://en.cryptobadges.io/badge/micro/LVSmZByqa6Cp1BFwgqeUyMjKmpfHP23ApR)](https://en.cryptobadges.io/donate/LVSmZByqa6Cp1BFwgqeUyMjKmpfHP23ApR)

Simple C# WPF application for placing trades at [Ally Invest](https://www.ally.com/api/invest/documentation/getting-started/). Their web platform for trading is a bit slow and doesn't always update, so I decided to create this to make things a bit faster.

**Supported**

1. Multiple accounts
1. Stock trading (limit/market orders, day/GTC orders)
1. Previous trades
1. Balance display
1. Order fill notifications
1. Real-time time and sales
1. Bid/ask quotes
1. Position status and profit/loss
1. Quotes through Ally or [Polygon.io](https://polygon.io/)

**Not supported**

1. Options
1. Short selling
1. Not tested with margin accounts

**Issues**

1. API timeout issues near the market open (including real-time quotes)

Requirements
---

1. You will need an Ally Invest account and API keys in order to place trades.
1. .NET Core 3.1

Configuration
---

Upon first start a `settings.json` file will be created. This file allows you to set your Ally Invest API keys.

Disclaimer
---

Do not use this to trade with a live account without rigorous testing and do expect to lose all your money if something goes wrong. I take no responsibility for how you use this code.

This application is in no way associated with Ally Bank or Ally Invest.

License
---

MIT

---

Powered by [Ally Invest](http://www.ally.com/invest)