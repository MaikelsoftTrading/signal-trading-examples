using System;

namespace SignalTrading.Examples.ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			ConsoleHelpers.ShowMenu(new  (string, Action)[]
			{
				("Generate signals from charts (Reactive)", () =>
				{
					Tutorial.GenerateSignals();
					Console.WriteLine("Press any key to return...");
					Console.ReadKey(true);
				}),
			}, "Main menu");
		}
	}
}
