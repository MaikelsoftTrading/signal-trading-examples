# SignalTrading framework
This repository contains the developer documentation and C# examples for the SignalTrading framework, which can be found on [NuGet](https://www.nuget.org/packages/SignalTrading.Core/). You are welcome to share bugs and feature requests using the [Github issue tracker](https://github.com/MaikelsoftTrading/signal-trading-examples/issues). 

## About the framework
The framework generates trading signals from price data and custom data using a provided strategy. Signals can be used for backtesting and live trading, published to a web site (or other channel) or placing orders at a broker. It can generate candlestick charts from live prices, supports margin trading (also for long positions), and estimates trading performance in real-time. The framework does not provide the mechanisms for connecting to a market data source or placing orders.

* Not limited to a specific type of asset. Signals can be generated for every trading instrument.
* No need to implement interfaces or inherit from framework classes. Instead, a trading strategy is implemented as a single C# function. 
* Prices can be paired with custom data that is to be analyzed by your strategy.
* Easy to share C# code for backtesting and live trading.

Performance metrics that are collected for each signal:

* Amount of money invested (automatically calculated)
* Total profit
* Return on investment
* Number of trades closed
* Number of winning trades and win rate
* Maximum drawdown
* Estimated position value
* Position closing costs
* Buy & hold return
* Details such as fees and interest paid, rounding errors

## Framework design
The framework is built with .NET 5.0 on top of 64-bit native C libraries (Windows, Linux). It exposes only pure functions and immutable types. You will notice that most of these
types are implemented as structs because of C#/C interopability. 

## License key
Current version can be used without any costs. Starting at the first major version (1.x.x), a license key must be obtained if you want to generate signals for more than one trading symbol.

## Version history
#### 0.5.5 -> 0.6.0 
* Moved extensions for `IObservable<T>` (defined in `ExtendIObservable` class) from `SignalTrading.Reactive` to `SignalTrading` namespace.
* Renamed a few extension methods that are used for deriving data (`ExtendIObservable` and `ExtendIEnumerable` classes).
* New extension methods for generating candles from pricing, that can be used for generating signals directly from candles (without generating charts).
#### 0.6.1
* Added buy & hold return to `SignalPerformance` class.
* Added initial pricing to `Signal` class.
* Removed `SignalPerformance.StartTime` method. Timestamp of initial pricing can be used instead.
* Fixed a bug in chart generation where closed candles were incorrectly set to open (only occurred in `ExtendIEnumerable.GenerateCharts` method).
#### 0.6.2
* Removed method from API that should have been internal.
#### 0.7.0
* New feature: methods for detecting signal changes, which can be used for generating alerts or placing orders.

# Development guide
## Prerequisites
* Basic understanding of functional programming and reactive programming in C#
* .NET 5.0 framework and the .NET 5.0 SDK are installed
* Latest [Visual C++ Redistributable (x64)](https://support.microsoft.com/en-us/topic/the-latest-supported-visual-c-downloads-2647da03-1eea-4433-9aff-95f26a218cc0) is installed if running on Windows
* C# development environment with NuGet package manager
* A .NET programming interface to a market data provider (for retrieving symbols, prices and candles).
* For bot development: .NET interface for managing orders at your broker or exchange

## Reference documentation
The [C# reference documentation](https://maikelsofttrading.github.io/signal-trading-examples/api/index.html) here on Github is a detailed description of all data types, methods and functions of the framework.

## How signals are generated
Signals are generated from a stream of tuples with a pricing object and additional data. The framework calls a provided strategy function for each tuple. It uses the pricing (timestamped buy, sell and last price) for determining if an entry or exit price is hit and for estimating profit. Additional data that is paired with pricing can be of a custom data type or a built-in type such as a candlestick chart (derived from pricing).
Source data can be provided using either push or pull mechanism: `IObservable<(Pricing, TData)>` versus `IEnumerable<(Pricing, TData)>`.

The basic flow for generating signals is:

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
8. Flow continues at step 2

## Tutorial
This tutorial explains how to generate trading signals from trade prices, and how to to accomplish this with automatically generated candlestick charts. The trading strategy in this tutorial will be fairly simple. Source code and a runnable console application can be found in this repository.

### 1. Create the symbol and define time frame
First, we need to create the trading symbol for which we're going to generate signals. Name, lot size and tick size are mandatory. In a real trading scenario this information is retrieved from a broker or exchange using an API. We also define the time frame for candlestick charts that is needed later on when generating charts.
```C#
// Create a symbol with lot size of 0.1 and tick size of 0.01
private static readonly Symbol Amazon = Symbol
	.Create("AMZN", 0.1, 0.01)
	.SetBaseAssetName("AMZN")
	.SetQuoteCurrencyName("USD");

// Define the candles time frame
private static readonly TimeSpan TimeFrame = TimeSpan.FromHours(1);
```

### 2. Create a method that displays a signal
```C#
public static void ShowSignal(Signal signal)
{
	// Define two helper functions for formatting amounts
	string FormatBase(double value) => $"{value.ToString($"N{signal.Symbol.BaseDecimals}")} " +
					   $"{signal.Symbol.BaseAssetName}";

	string FormatQuote(double value) => $"{value.ToString($"N{signal.Symbol.QuoteDecimals}")} " +
					    $"{signal.Symbol.QuoteCurrencyName}";

	// Show some basic info (see reference documentation or IntelliSense for more Signal properties).
	Console.WriteLine($"{signal.Symbol.Name} signal @ {signal.Timestamp():u}:");
	Console.WriteLine($"\tLast price: {FormatQuote(signal.Pricing.Last)}");
	Console.WriteLine($"\tCurrent position size: {FormatBase(signal.Position.Size)}");
	Console.WriteLine($"\tInvestment: {FormatQuote(signal.Performance.Investment)}");
	Console.WriteLine($"\tProfit: {FormatQuote(signal.Performance.Profit)}");
	Console.WriteLine($"\tReturn on investment: {signal.Performance.ROI:p2}");
	Console.WriteLine($"\tMaximum drawdown: {signal.Performance.MaximumDrawdown:p2}");
	Console.WriteLine($"\tTrades closed: {signal.Performance.TradesClosed}");
	Console.WriteLine($"\tTrades won: {signal.Performance.TradesWon} ({signal.Performance.WinRate:p2})");
}
```

### 3. Create the trading strategy
Strategies are implemented as callback functions that conform to the `Strategy<TData>` delegate. Our function computes a moving average from a candlestick chart, sets up a trade that enters below this average and takes profit at the average. The factory function below will create a strategy function for a specific moving average length.
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
		double average = chart.Values.TakeLast(movingAverageLength).Average(candle => candle.Close);

		// Create a long trade setup that enters below current moving average and takes profit
		// at current moving average.
		double profitTarget = signal.Symbol.RoundToTickSize(average); // Price should be rounded to tick size
		double entryPrice = profitTarget - 5;
		double lossLimit = entryPrice - 10;
		TradeSetup setup = TradeSetup.Long(entryPrice, 1, profitTarget, lossLimit);

		// A setup for long trading can only be set if its entry price is below the last trade price and
		// below current buy price. This can easily be validated before setting the new setup so an 
		// exception will be avoided.
		return signal.IsTradeSetupAllowed(setup) 
			? signal.SetLongTradeSetup(setup) 
			: signal;
	};
}
```
### 4. Backtest the strategy
#### 1. Get historical price data
Since backtesting uses historical data, candles must be retrieved from a market data source. In this tutorial we will not connect to a real data source and
just return some mock data. 
```C#
public static IEnumerable<Candle> GetHistoricalPrices()
{
	DateTimeOffset p0 = new DateTimeOffset(2021, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
	DateTimeOffset p1 = p0.Add(TimeFrame);
	DateTimeOffset p2 = p1.Add(TimeFrame);
	DateTimeOffset p3 = p2.Add(TimeFrame);
	DateTimeOffset p4 = p3.Add(TimeFrame);

	return new[]
	{
		Candle.Create(p0, 3000.00, 3101.14, 3000.00, 3101.14),
		Candle.Create(p1, 3101.14, 3230.65, 3004.89, 3230.65),
		Candle.Create(p2, 3230.65, 3410.81, 3010.34, 3410.81),
		Candle.Create(p3, 3410.81, 3420.20, 3005.23, 3240.16),
		Candle.Create(p4, 3240.16, 3300.77, 3005.71, 3300.77)
	};
}
```

#### 2. Run the test
Backtesting is usually done using Linq extensions (Reactive extensions can be used as an alternative). After the strategy is instantiated, candles are retrieved, candlestick chartsare generated and signals are generated from these charts.
```C#
public static void Backtest()
{
	// Create a strategy function. We will use a moving average length of 3 candles.
	Strategy<Chart> strategy = CreateMovingAverageStrategy(3);

	// Get the historical prices in the form of candles
	IEnumerable<Candle> candles = GetHistoricalPrices();

	// Generate charts from the candles. Specified time frame must match the time frame of the candles.
	IEnumerable<(Pricing, Chart)> pricesWithChart = candles.GenerateCharts(TimeFrame);

	// Generate signals from the charts
	IEnumerable<(Signal, Chart)> signalsWithChart = pricesWithChart.GenerateSignals(Amazon, strategy);

	// We're interested in the signals only
	IEnumerable<Signal> signals = signalsWithChart.SelectSignals();

	// Show info of the most recent signal
	Signal lastSignal = signals.Last();
	ShowSignal(lastSignal);
}
```

### 5. Simulate live trading
#### 1. Get live prices
In order to demonstrate live trading, we need some test data in the form of an observable Pricing sequence. The function below creates this observable which returns prices with arbitrary timestamps. These will be converted into candlesticks by the framework in the next step.
```C#
public static IObservable<Pricing> GetLivePrices()
{
	DateTimeOffset p0 = new DateTimeOffset(2021, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
	DateTimeOffset p1 = p0.Add(TimeFrame);
	DateTimeOffset p2 = p1.Add(TimeFrame);
	DateTimeOffset p3 = p2.Add(TimeFrame);
	DateTimeOffset p4 = p3.Add(TimeFrame);

	// Emit some prices at irregular intervals
	return new[]
	{
		Pricing.FromLastPrice(p0.AddMinutes(10), 3000.34),
		Pricing.FromLastPrice(p0.AddMinutes(24), 3101.14),
		Pricing.FromLastPrice(p1.AddMinutes(21), 3000.97),
		Pricing.FromLastPrice(p1.AddMinutes(56), 3230.65),
		Pricing.FromLastPrice(p2.AddMinutes(13), 3000.33),
		Pricing.FromLastPrice(p2.AddMinutes(50), 3410.81),
		Pricing.FromLastPrice(p3.AddMinutes(42), 3308.11),
		Pricing.FromLastPrice(p3.AddMinutes(49), 3240.16),
		Pricing.FromLastPrice(p4.AddMinutes(10), 3312.67),
		Pricing.FromLastPrice(p4.AddMinutes(14), 3300.77)
	}.ToObservable();
}
```

#### 2. Run the simulation
```C#
public static void SimulateLiveTrading()
{
	// Create a strategy function. We will use a moving average length of 3 candles.
	Strategy<Chart> strategy = CreateMovingAverageStrategy(3);

	// Get the prices
	IObservable<Pricing> livePrices = GetLivePrices();

	// Build charts from prices
	IObservable<(Pricing, Chart)> pricesWithChart = livePrices.GenerateCharts(TimeFrame);

	// Generate signals from the charts
	IObservable<(Signal, Chart)> signalsWithChart = pricesWithChart.GenerateSignals(Amazon, strategy);

	// We're interested in the signals only
	IObservable<Signal> signals = signalsWithChart.SelectSignals();

	// Show information from each signal
	IDisposable subscription = signals.Subscribe(ShowSignal);
	subscription.Dispose();
}
```
## Troubleshooting
### System.DllNotFoundException for 'libsignaltrading'
If **libsignaltrading.dll** exists in the program folder, make sure that the latest Visual C++ Redistributable (x64) is installed.
If **libsignaltrading.dll** (Windows) or **libsignaltrading.so** (Linux) does not exist, add the SignalTrading.Core package to the executing assembly.
