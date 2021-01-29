## SignalTrading framework
This repository contains the developer documentation and C# examples for the SignalTrading framework, which can be installed through [NuGet](https://www.nuget.org/packages/SignalTrading.Core/). Bugs can be reported through the GitHub issue tracker. 

### About the framework
The SignalTrading framework enables the rapid development of automated trading systems in .NET 5.0. It supports backtesting, live testing, and live trading by generating signals using pull or push mechanisms (IEnumerable or IObservable). Both long and short positions are supported and the leverage ratio can be set for each position. The framework estimates trading performance in real-time and reports unrealized profit, realized profit, return on investment, maximum drawdown, win rate, fees and interest paid, and more.

### Framework design
The framework is built on top of an optimized C library and applies principles from functional programming and stateless programming. It provides only pure functions and immutable objects. Most types are implemented as structs because of C#/C interopability. 

### Limitations
The framework can be used in x64 applications that run on Windows or Linux (x64 platform should be selected). Order execution is currently not part of the framework.

### License key
Current version can be used without any costs. The first major release (1.x.x) will require the purchase of a license key if you want to generate signals for more than one trading symbol.

### Prerequisites
* .NET 5.0 framework is installed
* A C# development environment with the .NET 5.0 SDK and NuGet installed
* Basic knowledge of functional programming in C# and reactive programming
* A .NET programming interface to a market data provider for live prices, price history, etc.
* For bot development: a .NET interface for retrieving balances and placing orders at the broker or exchange
