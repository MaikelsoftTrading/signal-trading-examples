using System;

namespace SignalTrading.Examples.ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			ConsoleHelpers.ShowMenu(new  (string, Action)[]
			{
				(ExamplesIEnumerable.Title, ExamplesIEnumerable.ShowMenu),
				(ExamplesIObservable.Title, ExamplesIObservable.ShowMenu)
			}, "Main menu");
		}
	}
}
