# SignalTrading framework
This repository contains the developer documentation and C# examples for the SignalTrading framework, which can be found on [NuGet](https://www.nuget.org/packages/SignalTrading.Core/). Bugs can be reported using the GitHub issue tracker. 

## About the framework
This is a framework for building signal-based trading applications for backtesting, live testing and live trading. The framework can generate 
trading signals from any type of data that contains price info. It can build candlestick charts from live prices, supports margin trading, leverage, and measures
signal performance in real-time.

## Framework design
The framework is built with .NET 5.0 on top of 64-bit native C libraries. It exposes only pure functions and immutable types. You will notice that most of these
types are implemented as structs because of C#/C interopability. 

## Limitations
The framework can be used in x64 applications that run on Windows or Linux (x64 platform should be selected). 

## License key
Current version can be used without any costs. Starting at the first major version (1.x.x), a license key must be obtained if you want to generate signals for more than one trading symbol.

## Prerequisites
* Basic knowledge of functional and reactive programming in C#
* .NET 5.0 framework is installed
* C# development environment with the .NET 5.0 SDK and NuGet installed
* .NET programming interface to a market data provider for retrieving live prices, price history, etc.
* For bot development: .NET interface for retrieving account balance and placing orders at the broker or exchange

# Development guide

## Tutorial

### Step 1: Create a trading symbol
Create the trading symbol for which signals will be generated. Name, lot size and tick size are mandatory. Most market data APIs provide an end point for retrieving this information.
```C#
private static readonly Symbol Amazon = Symbol.Create("AMZN", lotSize:1, tickSize: 0.01);
```

### Step 2: Create a trading strategy
```C#
public static Strategy<Chart> CreateStrategy()
{
	// Define the number of candles from which the average closing price is calculated
	const int movingAverageLength = 4;

	return (Signal signal, Chart chart) =>
	{
		// At this point, the signal is up-to-date with the latest prices and the position of the
		// signal is opened or closed according to these prices and the trade setups that
		// were set (see below).

		if (signal.Position.IsOpen)
		{
			// If a position is open, we just wait for the position to close automatically
			// when the profit target or loss limit is triggered.
			return signal; 
		}

		if (signal.LongTradeSetup.IsEnabled)
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

		if (signal.IsTradeSetupAllowed(setup))
		{
			signal = signal.SetLongTradeSetup(setup);
		}

		return signal;
	};
}
```
