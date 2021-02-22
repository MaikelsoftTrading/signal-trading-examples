# SignalTrading framework
This repository contains the developer documentation and C# examples for the SignalTrading framework, which can be found on [NuGet](https://www.nuget.org/packages/SignalTrading.Core/). You are welcome to share bugs and feature requests using the [Github issue tracker](https://github.com/MaikelsoftTrading/signal-trading-examples/issues). 

## About the framework
This is a framework for building signal-based trading applications for backtesting, live testing and live trading. It builds candlestick charts from live prices, supports margin trading, leverage, and estimates trading performance in real-time.

* It can be used for trading commodities, forex, stocks, cryptocurrencies and more.
* No need to implement interfaces or inherit from framework classes. Instead, a trading strategy is implemented as a single C# function. 
* Prices can be paired with custom data that is to be analyzed by your strategy: e.g. Twitter mentions, number of crypto wallets, fundamentals.
* Easy to share C# code for backtesting and live trading.

Performance metrics that are collected for each signal:

* Total investment
* Total profit
* Return on investment
* Number of trades closed
* Number of winning trades and win rate
* Maximum drawdown
* Estimated position value
* Position closing costs
* Fees and interest paid

## Framework design
The framework is built with .NET 5.0 on top of 64-bit native C libraries (Windows, Linux). It exposes only pure functions and immutable types. You will notice that most of these
types are implemented as structs because of C#/C interopability. 

## License key
Current version can be used without any costs. Starting at the first major version (1.x.x), a license key must be obtained if you want to generate signals for more than one trading symbol.

## Version history
#### 0.5.5 -> 0.6.0 
Small (**possibly breaking**) changes: 
* Moved extensions for `IObservable<T>` (defined in `ExtendIObservable` class) from `SignalTrading.Reactive` to `SignalTrading` namespace.
* Renamed a few extension methods that are used for deriving data (`ExtendIObservable` and `ExtendIEnumerable` classes).
* New extension methods for generating candles from pricing, that can be used for generating signals directly from candles (without generating charts).

# Development guide
## Prerequisites
* Basic understanding of functional programming and reactive programming in C#
* .NET 5.0 framework and the .NET 5.0 SDK are installed
* Latest [Visual C++ Redistributable (x64)](https://support.microsoft.com/en-us/topic/the-latest-supported-visual-c-downloads-2647da03-1eea-4433-9aff-95f26a218cc0) is installed if running on Windows
* C# development environment with NuGet package manager
* A .NET programming interface to a market data provider (for retrieving symbols, prices and candles).
* For bot development: .NET interface for managing orders at your broker or exchange
* 
## Reference documentation
The [C# reference documentation](https://maikelsofttrading.github.io/signal-trading-examples/api/index.html) here on Github is a detailed description of all data types, methods and functions of the framework.

## How signals are generated
Trading signals are generated from a stream of tuples with a pricing object and additional data. The framework calls a provided strategy function for each tuple. It uses the pricing (timestamped buy, sell and last price) for determining if an enty or exit price is hit and for estimating unrealized profits. Additional data that is paired with pricing can be of a custom data type or a built-in type such as a candlestick chart (derived from pricing elements).
Source data can be provided using either push or pull mechanism: `IObservable<(Pricing, TData)>` versus `IEnumerable<(Pricing, TData)>`.

The basic flow for generating signals from an observable sequence can be described as:

1. Framework subscribes to provided source
2. Framework receives (next) tuple from source
3. If this is the first tuple, framework initializes signal for provided symbol
4. Framework updates signal with latest prices and subsequently:
	1. If signal position is closed and trades have been set up, position is opened if an entry price was triggered
	2. If signal position is open, closes position if its profit target or loss limit price was triggered
5. Framework calls provided stategy function with signal and custom data as arguments
6. Provided strategy function checks/modifies the signal as follows:
	* If signal position is closed, sets up trades for the next long and/or short position
	* If signal postion is open, changes the profit target or loss limit if required
7. Framework provides observers with signal (paired with the source data)
8. Process is repeated from step 2

## Tutorial
This tutorial explains how to generate trading signals from last trade prices, and how to to accomplish this with automatically generated candlestick charts. The trading strategy in this tutorial will be fairly simple. 

### Create the trading symbol
First, we need to create the trading symbol for which we're going to generate signals. Name, lot size and tick size are mandatory. In a real trading scenario this information is retrieved from a broker or exchange using an API.
```C#
private static readonly Symbol Amazon = Symbol
	.Create("AMZN", lotSize: 0.1, tickSize: 0.01)
	.SetBaseAssetName("AMZN")
	.SetQuoteCurrencyName("USD");
```

### Create a factory for our strategy
Strategies are implemented as callback functions (that conform to the Strategy<TData> delegate) and the method below will create the function for our strategy. The function computes a moving average from the charts, sets up a trade that enters below this average and takes profit at the average.	
```C#
public static Strategy<Chart> CreateMovingAverageStrategy(int movingAverageLength)
{
	return (Signal signal, Chart chart) =>
	{
		// At this point, our signal is up-to-date with the latest prices and the position of the
		// signal is opened or closed according to these prices and the trade setups that
		// were set (see below).

		if (signal.Position.IsOpen)
		{
			// If a position is open, we just wait for the position to close automatically
			return signal;
		}

		if (signal.LongTradeSetup.IsSet)
		{
			// No position is open and we've already set up the long trade that we're interested in.
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

### Create test data
In order to demonstrate our strategy, we need some test data in the form of an observable Pricing sequence. The function below creates this observable which returns prices with arbitrary timestamps. These will be converted into candlesticks by the framework in the next step.
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
		Pricing.FromLastPrice(h4.AddMinutes(14), 3300.77)
	}.ToObservable();
}
```

### Build charts and generate signals
```C#
public static void GenerateSignals()
{
	// Create a strategy function that uses a moving average length of 3
	Strategy<Chart> strategy = CreateMovingAverageStrategy(3);

	// Get the prices
	IObservable<Pricing> prices = GetPricing();

	// Build charts from prices
	IObservable<(Pricing, Chart)> pricingWithChart = prices.GenerateCharts(TimeSpan.FromHours(1));

	// Generate signals from the charts
	IObservable<(Signal, Chart)> signalsWithChart = pricingWithChart.GenerateSignals(Amazon, strategy);

	// We're interested in the signals only
	IObservable<Signal> signals = signalsWithChart.SelectSignals();

	// In a real trading scenario we would subscribe to the observable. Here, we wait for the last
	// signal.
	Signal signal = signals.Wait();

	// Show some information from the signal
	Console.WriteLine($"{signal.Symbol.Name} signal @ {signal.Timestamp():u}:");
	string baseFormat = $"N{signal.Symbol.BaseDecimals}";
	string quoteFormat = $"N{signal.Symbol.QuoteDecimals}";
	Console.WriteLine(
		$"\tLast price: {signal.Pricing.Last.ToString(quoteFormat)} {signal.Symbol.QuoteCurrencyName}");
	Console.WriteLine(
		$"\tCurrent position size: {signal.Position.Size.ToString(baseFormat)} {signal.Symbol.BaseAssetName}");
	Console.WriteLine(
		$"\tInvestment: {signal.Performance.Investment.ToString(quoteFormat)} {signal.Symbol.QuoteCurrencyName}");
	Console.WriteLine(
		$"\tProfit: {signal.Performance.Profit.ToString(quoteFormat)} {signal.Symbol.QuoteCurrencyName}");
	Console.WriteLine($"\tReturn on investment: {signal.Performance.ROI:P2}");
}
```
## Troubleshooting
### System.DllNotFoundException for 'libsignaltrading'
If libsignaltrading.dll exists in the program folder, make sure that the latest Visual C++ Redistributable (x64) is installed.
If libsignaltrading.dll (Windows) or libsignaltrading.so (Linux) does not exist, add the SignalTrading.Core package to the executing assembly.
