# EasySpan

[![.NET Quality Check](https://github.com/Ataraxia-Hare/EasySpan/actions/workflows/dotnet-quality.yml/badge.svg)](https://github.com/Ataraxia-Hare/EasySpan/actions/workflows/dotnet-quality.yml)
[![Target: .NET Standard 2.1](https://img.shields.io/badge/target-.NET%20Standard%202.1-512BD4)](https://learn.microsoft.com/en-us/dotnet/standard/net-standard)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

EasySpan is a lightweight C# library for fixed-capacity collections backed by `Span<T>`.
It provides list and set operations over caller-owned memory, helping reduce heap allocations
and garbage collection pressure in short-lived, allocation-sensitive code paths.

## Features

- **`SpanList<T>`**: append, search, writable reference indexing, and constant-time unordered removal by index.
- **`SpanSet<T>`**: unique elements, non-throwing `TryAdd`, set operations, relationship checks, and allocation-free enumeration.
- **Caller-owned storage**: use stack-allocated memory, arrays, or slices of existing buffers.
- **Fixed capacity**: no automatic resizing or internal backing-array allocation.
- **Span interoperability**: expose current elements as `ReadOnlySpan<T>`, or as writable `Span<T>` for lists.
- **No external runtime package dependencies**.

## Requirements

| Purpose | Requirement |
| --- | --- |
| Use the library | A runtime supporting .NET Standard 2.1 |
| Compile the library and examples | C# 8.0 or later |
| Build the full solution, run tests or benchmarks | .NET 9 SDK, as used by CI |
| Element types | `T : unmanaged, IEquatable<T>` |

Both collections are `ref struct` types. They cannot be boxed, stored in class fields, or
captured by lambdas, and must obey span lifetime rules. They are not drop-in replacements
for `List<T>` or `HashSet<T>` when dynamically growing storage or heap-stored collections are required.

## Installation

Clone the repository and add a project reference from your application:

```powershell
git clone https://github.com/Ataraxia-Hare/EasySpan.git
dotnet add .\YourApp\YourApp.csproj reference .\EasySpan\EasySpan\EasySpan.csproj
```

Replace `.\YourApp\YourApp.csproj` with your application's project path. The reference above
assumes the cloned `EasySpan` directory and `YourApp` directory share a parent directory.

## Quick Start

```csharp
using System;
using EasySpan;

public static class Program
{
    public static void Main()
    {
        Span<int> listBuffer = stackalloc int[8];
        var list = new SpanList<int>(listBuffer);
        list.Add(10);
        list.Add(20);
        list.Add(30);
        list.RemoveAt(0); // Moves the last element into the removed slot.

        ReadOnlySpan<int> items = list;
        foreach (var item in items)
        {
            Console.WriteLine(item); // 30, then 20.
        }

        Span<int> setBuffer = stackalloc int[8];
        var set = new SpanSet<int>(setBuffer);
        Console.WriteLine(set.Add(10)); // True.
        Console.WriteLine(set.Add(10)); // False: already present.
        Console.WriteLine(set.TryAdd(20)); // True.

        foreach (var item in set)
        {
            Console.WriteLine(item); // 10, then 20.
        }
    }
}
```

Keep stack allocations small; use a suitably sized array or an existing buffer for larger collections.

## Usage

### Storage and initialization

The single-argument constructor starts an empty collection without clearing its backing memory.
To use existing elements, supply their count:

```csharp
int[] buffer = { 10, 20, 30, 0, 0, 0 };
var list = new SpanList<int>(buffer, 3);
list.Add(40);

Span<int> elements = list;
elements[0] = 99; // Also updates buffer[0].
```

Supply a count between zero and the buffer length. `SpanSet<T>(buffer, count)` additionally
requires the initial elements to be unique and throws `ArgumentException` for duplicates.

### Collection behavior

| Operation | `SpanList<T>` | `SpanSet<T>` |
| --- | --- | --- |
| Size and capacity | `Count`, `Capacity`, `IsFull()` | `Count`, `Capacity`, `IsFull()` |
| Add | `Add(item)` | `Add(item)` returns whether inserted |
| Non-throwing insertion | Not provided | `TryAdd(item)` returns `false` for duplicates or full capacity |
| Search | `Contains(item)`, `IndexOf(item)` | `Contains(item)` |
| Remove | `Remove(item)`, `RemoveAt(index)` | `Remove(item)` |
| Reset | `Clear()` | `Clear()` |
| Access current elements | Implicit `Span<T>` or `ReadOnlySpan<T>` conversion | Implicit `ReadOnlySpan<T>` conversion; direct `foreach` |

Adding to a full list throws `InvalidOperationException`. Adding a new element to a full set
also throws, but adding an existing element returns `false`.

Removal replaces the removed element with the last element, so **element order is not preserved**.
`Clear()` resets the count without wiping memory; removal also leaves data outside the active range.
Neither operation should be used to erase sensitive data.

The list indexer checks the backing span's bounds, not `Count`. Use indices below `Count`
when accessing active elements, or convert to a span to obtain an active-range view.

Collections and converted spans share their backing storage. Copying a collection copies its
count but not its buffer, so avoid independently mutating copies. Do not mutate a set's backing
buffer externally while it is in use, as this can break uniqueness.

### Set operations

```csharp
Span<int> buffer = stackalloc int[8];
var set = new SpanSet<int>(buffer);
ReadOnlySpan<int> initial = stackalloc int[] { 1, 2, 3 };
ReadOnlySpan<int> retained = stackalloc int[] { 2, 3, 4 };

set.UnionWith(initial);
set.IntersectWith(retained);

Console.WriteLine(set.SetEquals(stackalloc int[] { 3, 2 })); // True.
```

`SpanSet<T>` also provides `ExceptWith`, `Overlaps`, `IsSubsetOf`, and `IsSupersetOf`.
These methods accept `ReadOnlySpan<T>`. `SetEquals` ignores input order and duplicates.
`UnionWith` may partially modify the set before throwing if capacity is exhausted.
Use separate input storage for mutating set operations rather than an overlapping view of the set's buffer.

## Performance

EasySpan avoids allocating collection storage internally; allocating an array for the backing
buffer still allocates on the heap. Reusing an existing buffer or using `stackalloc` avoids
that additional backing-buffer allocation.

`SpanList<T>.Add`, `RemoveAt`, and `Clear` are O(1). Searches and removal by value are O(n).
`SpanSet<T>` uses linear equality searches rather than hashing: membership tests and insertion
are O(n), and inserting n distinct values takes O(n²) overall. It is intended for small,
allocation-sensitive sets, not as a universal replacement for `HashSet<T>`.

The BenchmarkDotNet project compares `SpanList<T>` with `List<T>` and `SpanSet<T>` with
`HashSet<T>`, using 16, 256, and 4096 elements and reporting memory allocations.
Run from the repository root:

```powershell
dotnet run -c Release --project .\EasySpan.Benchmarks -- --filter '*'
```

Run only the set comparisons:

```powershell
dotnet run -c Release --project .\EasySpan.Benchmarks -- --filter '*SpanSetVsHashSet*'
```

Interpret results per scenario: some span benchmarks include filling the collection before
querying it, while their standard-collection counterparts query pre-filled collections.
List removal also has different semantics: `List<T>.RemoveAt` preserves order, while
`SpanList<T>.RemoveAt` does not. Results are workload- and environment-dependent; benchmark
your actual use case rather than assuming a general speedup.

## Documentation

API documentation is included as XML comments in the source and generated during library builds:

- [`SpanList<T>` API](EasySpan/SpanList.cs)
- [`SpanSet<T>` API](EasySpan/SpanSet.cs)
- [Tests and behavioral examples](EasySpan.Tests)
- [Benchmark scenarios](EasySpan.Benchmarks)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the development workflow, pull request guidelines,
and Conventional Commits requirements. Open an issue before starting significant changes.

From the repository root, use the same checks as CI:

```powershell
dotnet restore EasySpan.sln
dotnet format EasySpan.sln --no-restore --verify-no-changes
dotnet build EasySpan.sln --configuration Release --no-restore -warnaserror
dotnet test EasySpan.sln --configuration Release --no-build
```

## License

EasySpan is licensed under the [MIT License](LICENSE).
