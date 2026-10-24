# EasySpan
The Span extension library.

## Benchmarks

Compare `SpanList<T>` with `List<T>` and `SpanSet<T>` with `HashSet<T>`:

```sh
dotnet run -c Release --project EasySpan.Benchmarks -- --filter *
```

Run a single comparison:

```sh
dotnet run -c Release --project EasySpan.Benchmarks -- --filter *SpanSetVsHashSet*
```

