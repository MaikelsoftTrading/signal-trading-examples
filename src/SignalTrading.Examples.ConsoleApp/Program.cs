using System;
using System.Globalization;

namespace SignalTrading.Examples.ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
			CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

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
