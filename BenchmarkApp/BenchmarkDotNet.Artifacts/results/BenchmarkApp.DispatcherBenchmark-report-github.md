```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley)
Intel Core i7-9700 CPU 3.00GHz, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2


```
| Method              | Mean     | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|-------------------- |---------:|----------:|----------:|-------:|-------:|----------:|
| MevoraDispatchAsync | 1.308 μs | 0.0204 μs | 0.0201 μs | 0.1202 | 0.0305 |     760 B |
| MediatrSendAsync    | 1.325 μs | 0.0258 μs | 0.0439 μs | 0.1202 | 0.0305 |     760 B |
