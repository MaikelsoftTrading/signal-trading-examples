using System;

namespace SignalTrading.Examples.ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			ConsoleHelpers.ShowMenu(new  (string, Action)[]
			{
				("Generate signals from candlestick charts", () =>
				{
					Tutorial.GenerateSignals();
					Console.WriteLine("Press any key to return...");
					Console.ReadKey(true);
				}),
				("Generate observable signals from candlestick charts", () =>
				{
					Tutorial.GenerateSignals();
					Console.WriteLine("Press any key to return...");
					Console.ReadKey(true);
				}),
				(ExamplesWithIEnumerable.Title, ExamplesWithIEnumerable.ShowMenu),
				(ExamplesWithIObservable.Title, ExamplesWithIObservable.ShowMenu)
			}, "Main menu");
		}
	}
}
