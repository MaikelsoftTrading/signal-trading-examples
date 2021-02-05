# SignalTrading framework
This repository contains the developer documentation and C# examples for the SignalTrading framework, which can be found on [NuGet](https://www.nuget.org/packages/SignalTrading.Core/). Bugs can be reported using the GitHub issue tracker. 

## About the framework
This is a framework for building signal-based trading applications for backtesting, live testing and live trading. The framework can generate 
trading signals from any data source that emits timestamped price data. It can build candlestick charts from live prices, supports margin trading, leverage, and measures
signal performance in real-time.

## Framework design
The framework is built with .NET 5.0 on top of 64-bit native C libraries (Windows, Linux). It exposes only pure functions and immutable types. You will notice that most of these
types are implemented as structs because of C#/C interopability. 

## Limitations
The framework can be used in x64 applications that run on Windows or Linux (x64 platform should be selected). 

## License key
Current version can be used without any costs. Starting at the first major version (1.x.x), a license key must be obtained if you want to generate signals for more than one trading symbol.

## Prerequisites
* Basic understanding of functional programming and reactive programming in C#
* .NET 5.0 framework and the .NET 5.0 SDK are installed
* C# development environment with NuGet package manager
* A .NET programming interface to a market data provider (for retrieving live prices, candles history, etc.).
* For bot development: .NET interface for retrieving balances and placing orders at the broker or exchange

# Development guide
## Reference documentation
The [C# reference documentation](https://maikelsofttrading.github.io/signal-trading-examples/api/index.html) here on Github is a detailed description of all data types, methods and functions of the framework.

## Signal generation process
The basic flow for generating signals from live price data, using candlestick charts, is described below. It requires an observable sequence of pricing objects, which can easily be created from any price data source (a Pricing object is timestamped Buy, Sell and Last prices). A time frame for the charts needs to be chosen and a strategy callback function must be provided.

1. Framework subscribes to provided pricing source
2. Framework receives (next) pricing object from source
3. If this is the first pricing, framework creates candlestick chart for provided time frame
4. Framework updates chart from pricing
5. If this is the first pricing/chart, framework creates signal for provided symbol
6. Framework updates signal with latest prices and subsequently:
	1. If signal position is closed and trades have been set up, position is opened if an entry price was triggered
	2. If signal position is open, closes position if its profit target or loss limit price was triggered
7. Framework calls provided stategy function with signal and chart as arguments
8. Strategy function checks/modifies the signal as follows:
	* If signal position is closed, sets trade setups for the next long and/or short position
	* If signal postion is open, changes the profit target or loss limit if required (e.g. trailing stop loss)
9. Framework provides observers with signal
10. Flow continues at step 2

## Tutorial
(The development of this tutorial is in progress)

### Create the trading symbol
Create the trading symbol for which signals will be generated. Name, lot size and tick size are mandatory. Most market data APIs provide an end point for retrieving this information.
```C#
private static readonly Symbol Amazon = Symbol
	.Create("AMZN", lotSize:0.1, tickSize: 0.01)
	.SetBaseAssetName("AMZN")
	.SetQuoteCurrencyName("USD");
```

### Create a factory function for our strategy
```C#
public static Strategy<Chart> CreateStrategy()
{
	// Define the number of candles from which the average closing price is calculated
	const int movingAverageLength = 3;

	return (Signal signal, Chart chart) =>
	{
		// At this point, our signal is up-to-date with the latest prices and the position of the
		// signal is opened or closed according to these prices and the trade setups that
		// were set (see below).

		if (signal.Position.IsOpen)
		{
			// If a position is open, we just wait for the position to close automatically
			// when its profit target or loss limit is triggered.
			return signal; 
		}

		if (signal.LongTradeSetup.IsSet)
		{
			// No position is open and we've already set up the long trade that we're interested in.
			// It is however allowed to change the trade setups as long as no position is open.
			return signal;
		}

		// No position is open, there is no trade setup and we're gonna try to set up our trade.
		// We're interested in closed candles only when working with moving averages.
		chart = chart.TakeClosedCandles();
		if (chart.Count < movingAverageLength)
		{
			return signal; // Not enough candles for computing the moving average
		}

		// Compute the average of the last closing prices
		double ma = chart.Values.TakeLast(movingAverageLength).Average(candle => candle.Close);

		// Create a long trade setup that enters below current moving average and takes profit
		// at current moving average.
		double profitTarget = signal.Symbol.RoundToTickSize(ma); // Price should be rounded to tick size
		double entryPrice = profitTarget - 5;
		double lossLimit = entryPrice - 10;
		TradeSetup setup = TradeSetup.Long(entryPrice, 1, profitTarget, lossLimit);

		// A setup for long trading can only be set if its entry price is below the last trade price and
		// below current buy price. This can easily be validated before setting the new setup so an 
		// exception will be avoided.
		if (signal.IsTradeSetupAllowed(setup))
		{
			signal = signal.SetLongTradeSetup(setup);
		}

		return signal;
	};
}
```

### Create an observable Pricing sequence
```C#
public static IObservable<Pricing> GetPricing()
{
	DateTimeOffset h0 = DateTimeOffset.UtcNow.Date;
	DateTimeOffset h1 = h0.AddHours(1);
	DateTimeOffset h2 = h1.AddHours(1);
	DateTimeOffset h3 = h2.AddHours(1);
	DateTimeOffset h4 = h3.AddHours(1);

	// Emit some prices at irregular intervals
	return new[]
	{
		Pricing.FromLastPrice(h0.AddMinutes(10), 3000.34),
		Pricing.FromLastPrice(h0.AddMinutes(24), 3101.14),

		Pricing.FromLastPrice(h1.AddMinutes(21), 3000.97),
		Pricing.FromLastPrice(h1.AddMinutes(56), 3230.65),

		Pricing.FromLastPrice(h2.AddMinutes(13), 3000.33),
		Pricing.FromLastPrice(h2.AddMinutes(50), 3410.81),

		Pricing.FromLastPrice(h3.AddMinutes(42), 3308.11),
		Pricing.FromLastPrice(h3.AddMinutes(49), 3240.16),

		Pricing.FromLastPrice(h4.AddMinutes(10), 3312.67),
		Pricing.FromLastPrice(h4.AddMinutes(10), 3300.77),

	}.ToObservable();
}
```

### Generate signals from charts
```C#
public static void GenerateSignals()
{
	// Create a strategy function that uses a moving average length of 3
	Strategy<Chart> strategy = CreateMovingAverageStrategy(3);

	IObservable<Pricing> prices = GetPricing();

	// Use the charts as input for the signals
	IObservable<(Pricing, Chart)> signalInput = prices.BuildCharts(TimeSpan.FromHours(1));

	// Generate the signals from the input
	IObservable<(Signal, Chart)> tuples = signalInput.GenerateSignals(Amazon, strategy);

	// The result also contains the input data (Chart) but we're only interested in the signals
	IObservable<Signal> signals = tuples.SelectSignals();

	// Show some info from the last signal
	Signal signal = signals.Wait();
	Console.WriteLine($"{signal.Symbol.Name} signal @ {signal.Timestamp():u}:");
	string baseFormat = $"N{signal.Symbol.BaseDecimals}";
	string quoteFormat = $"N{signal.Symbol.QuoteDecimals}";
	Console.WriteLine($"\tLast price: {signal.Pricing.Last.ToString(quoteFormat)} {signal.Symbol.QuoteCurrencyName}");
	Console.WriteLine($"\tCurrent position size: {signal.Position.Size.ToString(baseFormat)} {signal.Symbol.BaseAssetName}");
	Console.WriteLine($"\tInvestment: {signal.Performance.Investment.ToString(quoteFormat)} {signal.Symbol.QuoteCurrencyName}");
	Console.WriteLine($"\tProfit: {signal.Performance.Profit.ToString(quoteFormat)} {signal.Symbol.QuoteCurrencyName}");
	Console.WriteLine($"\tReturn on investment: {signal.Performance.ROI:P2}");
}
```
