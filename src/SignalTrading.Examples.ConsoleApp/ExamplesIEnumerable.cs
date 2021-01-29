using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SignalTrading.Examples.ConsoleApp
{
	public static class ExamplesIEnumerable
	{
		public static string Title => "Examples using IEnumerable";

		public static void ShowMenu()
		{
			ConsoleHelpers.ShowMenu(new  (string, Action)[]
			{
				("Generate signals from prices", GenerateSignalsFromPrices)
			}, Title);
		}

		private static void GenerateSignalsFromPrices()
		{
			// Create a dummy trading symbol for which signals will be generated. Lot size and tick size are required
			// and will be set to 1. For a real trading instrument this info is usually retrieved using an API.
			SymbolInfo symbolInfo = SymbolInfo.Create("TEST-USD", 1, 1);
			
			// Create some price data. The framework expects UTC timestamps.
			DateTimeOffset now = DateTimeOffset.UtcNow;
			DateTimeOffset startTime = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, 0, TimeSpan.Zero);
			IEnumerable<Pricing> prices = new[]
			{
				Pricing.FromLastPrice(startTime, 100),
				Pricing.FromLastPrice(startTime.AddMinutes(1), 98), 
				Pricing.FromLastPrice(startTime.AddMinutes(2), 95), 
				Pricing.FromLastPrice(startTime.AddMinutes(4), 101), 
				Pricing.FromLastPrice(startTime.AddMinutes(5), 100), 
				Pricing.FromLastPrice(startTime.AddMinutes(6), 103),
				Pricing.FromLastPrice(startTime.AddMinutes(7), 104)
			};

			// For simplicity of this example, the signal will be generated from price data only. In real trading
			// scenarios, the second value of the tuple is used to pass additional data to the trading strategy
			// such as a candlestick chart.
			IEnumerable<(Pricing, Pricing)> signalInput = prices.AsSignalInput();

			// Define a trading strategy. This is a callback function that returns the next entry targets.
			static (EntryTarget longTarget, EntryTarget shortTarget) Strategy(Signal signal, Pricing data)
			{
				if (signal.Position.IsOpen)
				{
					// The position of the signal opens and closes automatically. Since entry targets cannot be
					// set while a position is open, we will have to return disabled entry targets.
					return (EntryTarget.Disabled, EntryTarget.Disabled);
				}

				if (signal.LongEntryTarget.IsEnabled)
				{
					// We will not change the entry target if already set. It is allowed however to
					// replace current targets with new values.
					return (signal.LongEntryTarget, EntryTarget.Disabled);
				}

				// Set the entry target price, profit target and loss limit. Entry price must be set below current price.
				double targetPrice = signal.Pricing.Last - 2;
				EntryTarget longTarget = EntryTarget.Long(targetPrice, 10, targetPrice + 5, targetPrice - 5);

				// The validation below is recommended during development of a trading strategy. It validates that the profit
				// target and loss limit are valid according to the entry price, and that the entry target is valid according
				// to current prices.
				Debug.Assert(longTarget.IsValid(signal.Pricing));

				return (longTarget, EntryTarget.Disabled);
			}

			// Generate signals
			IEnumerable<Signal> signals = signalInput.GenerateSignals(symbolInfo, Strategy);

			// Show information about the latest signal
			signals.Last().WriteToConsole();

			ConsoleHelpers.WaitForAnyKeyToContinue();
		}

	}
}