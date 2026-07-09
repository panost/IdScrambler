using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(IdScrambler.Benchmarks.Transform32Benchmarks).Assembly).Run(args);
