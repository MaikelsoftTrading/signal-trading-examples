﻿using System;
using System.Collections.Generic;

namespace SignalTrading.Examples.ConsoleApp
{
	public static class ConsoleHelpers
	{
		public static void WaitForAnyKeyToQuit()
		{
			if (Console.IsInputRedirected)
			{
				return;
			}
			Console.Write("Press any key to quit...");
			Console.ReadKey(true);
		}

		public static void WriteToConsole(this SymbolInfo symbol)
		{
			Console.WriteLine($"{symbol.Symbol} info:");
			Console.WriteLine($"\tLot size: {symbol.LotSize:N10}");
			Console.WriteLine($"\tTick size: {symbol.TickSize:N10}");
			Console.WriteLine($"\tTrading fee: {symbol.TradingFeeRate:P4}");
			Console.WriteLine($"\tInterest rate: {symbol.InterestRate:P4}");
			Console.WriteLine($"\tInterest interval: {symbol.InterestInterval()}");
		}

		public static void WriteToConsole(this IEnumerable<Candle> candles)
		{
			foreach (Candle candle in candles)
			{
				candle.WriteToConsole();
			}
		}

		public static void WriteToConsole(this Candle candle)
		{
			Console.WriteLine($"{candle.OpenTime():u} O={candle.Open}, H={candle.High}, L={candle.Low}, C={candle.Close}");
		}

		public static void WriteToConsole(this Signal signal, bool showAccountInfo = false)
		{
			string baseNumberFormat = $"N{signal.SymbolInfo.BaseDecimals}";
			string quoteNumberFormat = $"N{signal.SymbolInfo.QuoteDecimals}";

			Console.WriteLine("==============================================================");
			Console.WriteLine($"{signal.Symbol} signal @ {signal.Timestamp():u}");
			Console.WriteLine("--------------------------------------------------------------");
			signal.Pricing.WriteToConsole(quoteNumberFormat);
			signal.LongEntryTarget.WriteToConsole("Long entry target:", quoteNumberFormat);
			signal.ShortEntryTarget.WriteToConsole("Short entry target:", quoteNumberFormat);
			signal.Position.WriteToConsole(baseNumberFormat, quoteNumberFormat);
			signal.Performance.WriteToConsole(quoteNumberFormat);
			if (showAccountInfo)
			{
				signal.Account.Base.WriteToConsole("Base currency balance:", baseNumberFormat);
				signal.Account.Quote.WriteToConsole("Quote currency balance:", quoteNumberFormat);
			}
		}
		
		public static void WriteToConsole(this EntryTarget entryTarget, string title, string numberFormat)
		{
			Console.WriteLine(title);
			if (!entryTarget.IsEnabled)
			{
				Console.WriteLine("\tDisabled");
				return;
			}
			Console.WriteLine($"\tPrice: {entryTarget.Price.ToString(numberFormat)} USD");
			Console.WriteLine($"\tProfit target: {entryTarget.ProfitTarget.ToString(numberFormat)} USD");
			Console.WriteLine($"\tLoss limit: {entryTarget.LossLimit.ToString(numberFormat)} USD");
			Console.WriteLine($"\tLeverage ratio: {entryTarget.LeverageRatio}");
		}

		public static void WriteToConsole(this SignalPosition position, string baseNumberFormat, string quoteNumberFormat)
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
			Console.WriteLine($"\tEquity: {performance.Equity.ToString(numberFormat)} USD (peak: {performance.EquityPeak.ToString(numberFormat)} USD)");
			Console.WriteLine($"\tProfit: {performance.Profit.ToString(numberFormat)}");
			Console.WriteLine($"\tROI: {performance.ROI:P2}");
			Console.WriteLine($"\tMaximum drawdown: {performance.MaximumDrawdown:P2}");
			Console.WriteLine($"\tRoMaD: {performance.RoMaD:N2}");
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
