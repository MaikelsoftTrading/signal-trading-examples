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

		public static void WriteToConsole(this Signal signal, bool showAccountInfo = false)
		{
			string baseNumberFormat = $"N{signal.Symbol.BaseDecimals}";
			string quoteNumberFormat = $"N{signal.Symbol.QuoteDecimals}";

			Console.WriteLine("==============================================================");
			Console.WriteLine($"{signal.Symbol.Name} signal @ {signal.Timestamp():u}");
			Console.WriteLine("--------------------------------------------------------------");
			signal.Pricing.WriteToConsole(quoteNumberFormat);
			signal.LongTradeSetup.WriteToConsole("Long trade setup:", quoteNumberFormat);
			signal.ShortTradeSetup.WriteToConsole("Short trade setup:", quoteNumberFormat);
			signal.Position.WriteToConsole(baseNumberFormat, quoteNumberFormat);
			signal.Performance.WriteToConsole(quoteNumberFormat);
			if (showAccountInfo)
			{
				signal.Account.Base.WriteToConsole("Base currency balance:", baseNumberFormat);
				signal.Account.Quote.WriteToConsole("Quote currency balance:", quoteNumberFormat);
			}
		}

		public static void WriteToConsole(this TradeSetup setup, string title, string numberFormat)
		{
			Console.WriteLine(title);
			if (!setup.IsSet)
			{
				Console.WriteLine("\tDisabled");
				return;
			}

			Console.WriteLine($"\tEntry price: {setup.EntryPrice.ToString(numberFormat)} USD");
			Console.WriteLine($"\tProfit target: {setup.ProfitTarget.ToString(numberFormat)} USD");
			Console.WriteLine($"\tLoss limit: {setup.LossLimit.ToString(numberFormat)} USD");
			Console.WriteLine($"\tLeverage ratio: {setup.LeverageRatio}");
		}

		public static void WriteToConsole(this SignalPosition position, string baseNumberFormat,
			string quoteNumberFormat)
		{
			if (!position.IsOpen)
			{
				Console.WriteLine("No open position");
				return;
			}

			Console.WriteLine("Current position:");
			Console.WriteLine($"\tSize: {position.Size.ToString(baseNumberFormat)}");
			Console.WriteLine($"\tEntry price: {position.EntryPrice.ToString(quoteNumberFormat)} USD");
			Console.WriteLine($"\tEntry time: {position.EntryTime():u}");
			Console.WriteLine($"\tOpening value: {position.OpeningValue.ToString(quoteNumberFormat)} USD");
			Console.WriteLine($"\tCurrent value: {position.Value.ToString(quoteNumberFormat)} USD");
			Console.WriteLine($"\tUnrealized profit: {position.Profit.ToString(quoteNumberFormat)} USD");
			Console.WriteLine($"\tClosing costs: {position.ClosingCosts.ToString(quoteNumberFormat)} USD");
		}

		public static void WriteToConsole(this SignalPerformance performance, string numberFormat)
		{
			Console.WriteLine("Performance:");
			Console.WriteLine($"\tStart time: {performance.StartTime():u}");
			Console.WriteLine($"\tInvestment: {performance.Investment.ToString(numberFormat)} USD");
			Console.WriteLine(
				$"\tEquity: {performance.Equity.ToString(numberFormat)} USD (peak: {performance.EquityPeak.ToString(numberFormat)} USD)");
			Console.WriteLine($"\tProfit: {performance.Profit.ToString(numberFormat)}");
			Console.WriteLine($"\tReturn on investment (ROI): {performance.ROI:P2}");
			Console.WriteLine($"\tMaximum drawdown: {performance.MaximumDrawdown:P2}");
			Console.WriteLine($"\tReturn over maximum drawdown (RoMaD): {performance.RoMaD:N2}");
			Console.WriteLine($"\tTrades closed: {performance.TradesClosed}");
			Console.WriteLine($"\tTrades won: {performance.TradesWon} ({performance.WinRate:P2})");
		}

		public static void WriteToConsole(this Pricing pricing, string numberFormat)
		{
			Console.WriteLine("Latest prices:");
			Console.WriteLine($"\tLast: {pricing.Last.ToString(numberFormat)} USD");
			Console.WriteLine($"\tBuy: {pricing.Buy.ToString(numberFormat)} USD");
			Console.WriteLine($"\tSell: {pricing.Sell.ToString(numberFormat)} USD");
		}

		public static void WriteToConsole(this Ledger ledger, string title, string numberFormat)
		{
			Console.WriteLine(title);
			Console.WriteLine($"\tCash: {ledger.Cash.ToString(numberFormat)}");
			Console.WriteLine($"\tDeposits: {ledger.Deposits.ToString(numberFormat)}");
			Console.WriteLine($"\tDebt: {ledger.Debt.ToString(numberFormat)}");
			Console.WriteLine($"\tFees: {ledger.PaidFees.ToString(numberFormat)}");
			Console.WriteLine($"\tInterest: {ledger.PaidInterest.ToString(numberFormat)}");
			Console.WriteLine($"\tRounding errors: {ledger.RoundingErrors.ToString(numberFormat)}");
		}
	}
}