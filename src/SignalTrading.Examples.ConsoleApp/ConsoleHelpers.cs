using System;
using System.Collections.Generic;

namespace SignalTrading.Examples.ConsoleApp
{
	public static class ConsoleHelpers
	{
		public static void ShowMenu(IReadOnlyList<(string, Action)> actions, string title)
		{
			if (Console.IsInputRedirected)
			{
				Console.WriteLine("This application cannot work if console input is redirected.");
				return;
			}

			do
			{
				Console.Clear();
				Console.WriteLine("===================================================");
				Console.WriteLine(title);
				Console.WriteLine("===================================================");
				for (int i = 0; i < actions.Count; i++)
				{
					Console.WriteLine($"{i + 1}.   {actions[i].Item1}");
				}

				Console.WriteLine("ESC. Quit");
				ConsoleKeyInfo key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Escape)
				{
					return;
				}

				int index = key.KeyChar - '0' - 1;
				if (index >= 0 && index < actions.Count)
				{
					Console.Clear();
					actions[index].Item2();
					Console.WriteLine("Press any key to return...");
					Console.ReadKey(true);
				}
				else
				{
					Console.Beep();
				}
			} while (true);
		}

		public static void WaitForAnyKeyToContinue()
		{
			if (Console.IsInputRedirected)
			{
				return;
			}

			Console.Write("Press any key to continue...");
			Console.ReadKey(true);
		}
	}
}