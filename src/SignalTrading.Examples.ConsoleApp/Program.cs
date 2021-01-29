using System;

namespace SignalTrading.Examples.ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			ConsoleHelpers.ShowMenu(new  (string, Action)[]
			{
				(ExamplesWithIEnumerable.Title, ExamplesWithIEnumerable.ShowMenu),
				(ExamplesWithIObservable.Title, ExamplesWithIObservable.ShowMenu)
			}, "Main menu");
		}
	}
}
