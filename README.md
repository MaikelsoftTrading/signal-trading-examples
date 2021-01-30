## SignalTrading framework
This repository contains the developer documentation and C# examples for the SignalTrading framework, which can be found on [NuGet](https://www.nuget.org/packages/SignalTrading.Core/). Bugs can be reported using the GitHub issue tracker. 

### About the framework
This framework supports the rapid development of automated trading systems in .NET 5.0. It enables backtesting, live testing, and live trading by generating streams of signals from input data, according to a provided trading strategy. Streams of input data and signals can be implemented as pull or push mechanisms (IEnumerable or IObservable). Both long and short trading is supported and the leverage ratio can be set for each position. The framework estimates trading performance in real-time and reports unrealized profit, realized profit, return on investment, maximum drawdown, win rate, fees and interest paid, and more.

### Framework design
The framework is built on top of an optimized 64-bit C library and applies principles from functional programming and stateless programming. It provides only pure functions and immutable objects. Most types are implemented as structs because of C#/C interopability. 

### Limitations
The framework can be used in x64 applications that run on Windows or Linux (x64 platform should be selected). Order execution is currently not part of the framework.

### License key
Current version can be used without any costs. Starting at the first major version (1.x.x), a license key must be obtained if you want to generate signals for more than one trading symbol.

### Prerequisites
* .NET 5.0 framework is installed
* A C# development environment with the .NET 5.0 SDK and NuGet installed
* Basic knowledge of functional programming in C# and reactive programming
* A .NET programming interface to a market data provider for live prices, price history, etc.
* For bot development: a .NET interface for retrieving balances and placing orders at the broker or exchange
