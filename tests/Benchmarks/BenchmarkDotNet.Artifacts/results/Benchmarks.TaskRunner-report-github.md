``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.202
  [Host]     : .NET Core 3.1.4 (CoreCLR 4.700.20.20201, CoreFX 4.700.20.22101), X64 RyuJIT
  DefaultJob : .NET Core 3.1.4 (CoreCLR 4.700.20.20201, CoreFX 4.700.20.22101), X64 RyuJIT


```
|                Method |    Mean |    Error |   StdDev |     Gen 0 |     Gen 1 | Gen 2 | Allocated |
|---------------------- |--------:|---------:|---------:|----------:|----------:|------:|----------:|
|             JustRunIt | 1.571 s | 0.0160 s | 0.0134 s | 7000.0000 | 3000.0000 |     - |  39.04 MB |
|  RunWithReturnedInput | 1.590 s | 0.0302 s | 0.0297 s | 8000.0000 | 4000.0000 |     - |  42.79 MB |
| RunWithReturnedOutput | 1.585 s | 0.0227 s | 0.0212 s | 7000.0000 | 3000.0000 |     - |  42.81 MB |
