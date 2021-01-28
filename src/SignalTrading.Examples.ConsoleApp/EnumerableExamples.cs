using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SignalTrading.Examples.ConsoleApp
{
	public static class EnumerableExamples
	{
		public static void HelloTrader()
		{
			// Create a trading symbol for which signals will be generated. Lot size and tick size are required
			// and will be set to 1.
			// In a realistic scenario the required info will come from an API.
			SymbolInfo symbolInfo = SymbolInfo.Create("TEST-USD", 1, 1);
			
			// Create some price data. Timestamps must UTC times.
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

			// In a most scenarios, the second value of the signal input tuple will contain additional data such
			// as a candlestick chart or other data that can be derived from the price stream.
			IEnumerable<(Pricing, Pricing)> signalInput = prices.AsSignalInput();

			// Define a trading strategy. This is a callback function that returns the next entry targets.
			static (EntryTarget longTarget, EntryTarget shortTarget) Strategy(Signal signal, Pricing data)
			{
				if (signal.Position.IsOpen)
				{
					// The position of the signal opens and closes automatically and since entry targets cannot be
					// set while a position is open, we will have to returned disabled targets.
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

				// This validation is recommended because the signal will throw an exception if an invalid entry
				// target is returned.
				Debug.Assert(longTarget.IsValid(signal.Pricing));

				return (longTarget, EntryTarget.Disabled);
			}

			// Generate signals
			IEnumerable<Signal> signals = signalInput.GenerateSignals(symbolInfo, Strategy);

			// The last signal is the most recent
			signals.Last().WriteToConsole();

			ConsoleHelpers.WaitForAnyKeyToQuit();
		}
	}
}