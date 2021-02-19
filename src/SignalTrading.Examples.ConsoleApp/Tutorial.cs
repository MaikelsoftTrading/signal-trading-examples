using System;
using System.Linq;
using System.Reactive.Linq;

namespace SignalTrading.Examples.ConsoleApp
{
	public static class Tutorial
	{
		#region Create a trading symbol

		private static readonly Symbol Amazon = Symbol
			.Create("AMZN", lotSize: 0.1, tickSize: 0.01)
			.SetBaseAssetName("AMZN")
			.SetQuoteCurrencyName("USD");

		#endregion

		#region Create a factory function for our strategy

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

		#endregion

		#region Create price data

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

		#endregion

		#region Generate signals from charts

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

		#endregion
	}
}