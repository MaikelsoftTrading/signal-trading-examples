using System;
using System.Linq;
using System.Reactive.Linq;
using SignalTrading.Reactive;

namespace SignalTrading.Examples.ConsoleApp
{
	public static class Tutorial
	{
		#region Step 1: Create a trading symbol

		/// <summary>
		/// Create the trading symbol for which signals will be generated. Name, lot size and tick size
		/// are mandatory. Most market data APIs provide an end point for retrieving this information.
		/// </summary>
		private static readonly Symbol Amazon = Symbol.Create("AMZN", lotSize:1, tickSize: 0.01);

		#endregion

		#region Step 2: Create a trading strategy

		public static Strategy<Chart> CreateStrategy()
		{
			// Define the number of candles from which the average closing price is calculated
			const int movingAverageLength = 4;

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
