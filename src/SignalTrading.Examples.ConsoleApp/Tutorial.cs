using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace SignalTrading.Examples.ConsoleApp
{
	public static class Tutorial
	{
		// Create a symbol with lot size of 0.1 and tick size of 0.01
		private static readonly Symbol Amazon = Symbol
			.Create("AMZN", 0.1, 0.01)
			.SetBaseAssetName("AMZN")
			.SetQuoteCurrencyName("USD");

		// Define the candles time frame
		private static readonly TimeSpan TimeFrame = TimeSpan.FromHours(1);

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
	}
}