using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(EasySpan.Benchmarks.SpanListVsListBenchmarks).Assembly).Run(args);
