using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using SignalTrading.Reactive;

namespace SignalTrading.Examples.ConsoleApp
{
	public static class Tutorial
	{
		#region Step 1: Create a trading symbol

		private static readonly Symbol Amazon = Symbol
			.Create("AMZN", lotSize: 1, tickSize: 0.01)
			.SetBaseAsset("AMZN")
			.SetQuoteCurrency("USD");

		#endregion

		#region Step 2: Create a trading strategy

		public static Strategy<Chart> CreateStrategy()
		{
			const int fastMaLength = 3;
			const int slowMaLength = 6;

			return (Signal signal, Chart chart) =>
			{
				if (signal.Position.IsOpen)
				{
					// It is not allowed to set entry targets while the position is open. The position of the
					// signal is closed automatically when the profit target or loss limit price is hit.
					return signal; 
				}

				// We're interested in closed candles only for our moving average crossover strategy. The last
				// candle is 'open' as long as the end of the candle period has not been reached.
				chart = chart.TakeClosedCandles();
				if (chart.Count < slowMaLength)
				{
					return signal;
				}

				// Compute the two moving averages from the chart
				double fastMA = chart.Values.TakeLast(fastMaLength).Select(c => c.Close).Average();
				double slowMA = chart.Values.TakeLast(slowMaLength).Select(c => c.Close).Average();
				if (fastMA > slowMA)
				{
					// TODO: Replace with round method of Symbol
					double entryPrice = TradingMath.RoundToTickSize(slowMA, signal.Symbol.TickSize);
					double profitTarget = entryPrice + 400 * signal.Symbol.TickSize;
					double lossLimit = entryPrice - 200 * signal.Symbol.TickSize;

					EntryTarget entryTarget = EntryTarget.Long(entryPrice, 1, profitTarget, lossLimit);
					if (signal.IsEntryTargetValid(entryTarget))
					{
						signal = signal.SetLongEntryTarget(entryTarget);
					}
				}
				else
				{
					signal = signal.SetLongEntryTarget(EntryTarget.Disabled);
				}
				
				return signal;
			};
		}

		#endregion

		#region Step 3: Create price data

		public static IEnumerable<Pricing> CreatePriceData()
		{
			DateTimeOffset now = DateTimeOffset.UtcNow;
			DateTimeOffset start = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 10, 0, 0, TimeSpan.Zero);

			return new[]
				{
					3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 
					3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000,
					3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000,
					3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000,
				}
				.Select((price, i) =>
				{
					DateTimeOffset timestamp = start.AddMinutes(i * 30);
					return Pricing.FromLastPrice(timestamp, price);
				});
		}

		#endregion

		#region Step 4: Build charts from prices

		public static IEnumerable<(Pricing, Chart)> BuildCharts()
		{
			IEnumerable<Pricing> prices = CreatePriceData();
			TimeSpan interval = TimeSpan.FromHours(1);
			return prices.BuildCharts(interval);
		}

		public static IObservable<(Pricing, Chart)> BuildReactiveCharts()
		{
			IObservable<Pricing> prices = CreatePriceData().ToObservable();
			TimeSpan interval = TimeSpan.FromHours(1);
			return prices.BuildCharts(interval);
		}

		#endregion

		#region Step 5: Generate signals from charts

		public static void GenerateSignals()
		{
			// Use the charts as input for the signals
			IEnumerable<(Pricing, Chart)> signalInput = BuildCharts();
			
			// Get the strategy that was created earlier
			Strategy<Chart> strategy = CreateStrategy();
			
			// Generate the signals from the input
			IEnumerable<(Signal, Chart)> tuples = signalInput.GenerateSignals(Amazon, strategy);

			// The result also contains the input data (Chart) but we're only interested in the signals
			IEnumerable<Signal> signals = tuples.SelectSignals();

			// Show some info about the last signal
			Signal signal = signals.Last();
			Console.WriteLine($"{signal.Symbol.Name} @ {signal.Timestamp():u}:");
			Console.WriteLine($"\tLast price: {signal.Pricing.Last} {signal.Symbol.QuoteCurrency}");
			Console.WriteLine($"\tCurrent position size: {signal.Position.Size} {signal.Symbol.BaseAsset}");
		}

		public static void GenerateReactiveSignals()
		{
			// Use the charts as input for the signals
			IObservable<(Pricing, Chart)> signalInput = BuildReactiveCharts();
			
			// Get the strategy that was created earlier
			Strategy<Chart> strategy = CreateStrategy();
			
			// Generate the signals from the input
			IObservable<(Signal, Chart)> tuples = signalInput.GenerateSignals(Amazon, strategy);

			// The result also contains the input data (Chart) but we're only interested in the signals
			IObservable<Signal> signals = tuples.SelectSignals();

			// Show some info about the last signal
			Signal signal = signals.Wait();
			Console.WriteLine($"{signal.Symbol.Name} @ {signal.Timestamp():u}:");
			Console.WriteLine($"\tLast price: {signal.Pricing.Last} {signal.Symbol.QuoteCurrency}");
			Console.WriteLine($"\tCurrent position size: {signal.Position.Size} {signal.Symbol.BaseAsset}");
		}

		#endregion
	}
}
