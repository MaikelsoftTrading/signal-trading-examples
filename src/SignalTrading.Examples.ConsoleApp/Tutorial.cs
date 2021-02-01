using System;
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
			.SetBaseAssetName("AMZN")
			.SetQuoteCurrencyName("USD");

		#endregion

		#region Step 2: Create a trading strategy

		public static Strategy<Chart> CreateStrategy()
		{
			const int movingAverageLength = 4;

			return (Signal signal, Chart chart) =>
			{
				if (signal.Position.IsOpen)
				{
					// It is not allowed to set entry targets while the position is open. The position of the
					// signal is closed automatically when the profit target or loss limit price is hit.
					return signal; 
				}

				if (signal.LongTradeSetup.IsEnabled)
				{
					// For simplicity of the tutorial, we will compute the entry target once and just wait for a
					// position to be opened by the signal.
					return signal;
				}

				// We're interested in closed candles only when working with moving averages.
				chart = chart.TakeClosedCandles();
				if (chart.Count < movingAverageLength)
				{
					// Not enough candles for computing the moving average
					return signal;
				}

				// Compute the moving average from the chart
				double ma = chart.Values
					.TakeLast(movingAverageLength)
					.Select(candle => candle.Close)
					.Average();

				double profitTarget = signal.Symbol.RoundToTickSize(ma);
				double entryPrice = profitTarget - 5;
				double lossLimit = entryPrice - 10;

				TradeSetup setup = TradeSetup.Long(entryPrice, 1, profitTarget, lossLimit);
				if (signal.IsTradeSetupValid(setup))
				{
					signal = signal.SetLongTradeSetup(setup);
				}
				
				return signal;
			};
		}

		#endregion

		#region Step 3: Create price data

		public static IObservable<Pricing> CreatePriceData()
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
				})
				.ToObservable();
		}

		#endregion

		#region Step 4: Build charts from prices

		public static IObservable<(Pricing, Chart)> BuildCharts()
		{
			IObservable<Pricing> prices = CreatePriceData();
			TimeSpan interval = TimeSpan.FromHours(1);
			return prices.BuildCharts(interval);
		}

		#endregion

		#region Step 5: Generate signals from charts

		public static void GenerateSignals()
		{
			// Use the charts as input for the signals
			IObservable<(Pricing, Chart)> signalInput = BuildCharts();
			
			// Get the strategy that was created earlier
			Strategy<Chart> strategy = CreateStrategy();
			
			// Generate the signals from the input
			IObservable<(Signal, Chart)> tuples = signalInput.GenerateSignals(Amazon, strategy);

			// The result also contains the input data (Chart) but we're only interested in the signals
			IObservable<Signal> signals = tuples.SelectSignals();

			// Show some info about the last signal
			Signal signal = signals.Wait();
			Console.WriteLine($"{signal.Symbol.Name} @ {signal.Timestamp():u}:");
			Console.WriteLine($"\tLast price: {signal.Pricing.Last} {signal.Symbol.QuoteCurrencyName}");
			Console.WriteLine($"\tCurrent position size: {signal.Position.Size} {signal.Symbol.BaseAssetName}");
		}

		#endregion
	}
}
