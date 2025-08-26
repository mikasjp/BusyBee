# BusyBee 🐝💨

Fast and observable background job processing for .NET built on native channels. Simple configuration, rich job context, and built-in OpenTelemetry metrics and tracing.

[![GitHub License](https://img.shields.io/github/license/mikasjp/BusyBee)](https://github.com/mikasjp/BusyBee)
[![GitHub Build Status](https://github.com/mikasjp/BusyBee/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/mikasjp/BusyBee)
[![GitHub Repo stars](https://img.shields.io/github/stars/mikasjp/BusyBee)](https://github.com/mikasjp/BusyBee)

## Installation

Available on NuGet: https://www.nuget.org/packages/BusyBee

Install via .NET CLI:

```bash
dotnet add package BusyBee
```

## Features

### 🚀 High Performance

* In-memory queues built on .NET channels for speed and efficiency

### ⚙️ Highly Configurable

* Unbounded or bounded queues with multiple overflow strategies
* Configurable job timeouts both globally and per job
* Parallel job processing with configurable slots pool size

### 📊 Built-in Observability

* Job execution flow logging
* Tracing ready for OpenTelemetry
* Detailed OpenTelemetry ready metrics (jobs count, execution times, wait times, and more...)

### 🔧 Developer Friendly

* Fluent configuration API
* Full dependency injection support
* Comprehensive cancellation token support
* Rich job context information


## Links

Source code, docs and examples: https://github.com/mikasjp/BusyBee