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
				("Backtest", Tutorial.Backtest),
				("Live trading simulation", Tutorial.SimulateLiveTrading)
			}, "Main menu");
		}
	}
}
