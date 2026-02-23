# `LuaStyleCoroutines` - Lua-Style Coroutines for .NET

A lightweight, pure .NET implementation of coroutines inspired by Lua's cooperative multitasking model, written in VB.NET but fully compatible with C#.

## Requirements
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later
- Works with VB.NET, C#, F#, and any .NET language

## Features
- ✅ **Lightweight** - No external dependencies, minimal memory footprint
- ✅ **Data-focused** - Not tied to time or game loops, works with any data type
- ✅ **Two-way communication** - Pass data back into coroutines with `ResumeWith(value)`
- ✅ **Type-safe** - Generic implementation with strong typing
- ✅ **LINQ Support** - Full LINQ integration for data manipulation
- ✅ **Lua-friendly** - Includes `coroutine.wrap` functionality via `AsDelegate`
- ✅ **Resource-aware** - Implements `IDisposable` for proper resource management

## Quick Example
- **VB.NET**:
```vb
' Create a coroutine that yields Fibonacci numbers
Dim fib As New Coroutine(Of Integer)(
    Function()
        Return Iterator Function()
            Dim prev = 0, curr = 1
            Do
                Yield curr
                Dim temp = prev
                prev = curr
                curr += temp
            Loop
        End Function()
    End Function)

fib.Start()
Console.Write("[Fibonacci sequence] ")
For i = 1 To 10
    fib.Continue()
    Console.Write("{0}, ", fib.Current)
Next i

' Create a coroutine that yields numbers from 1 to 100;
' Print all numbers that are divisible by 3 with a remainder of 1.
Dim nums As New Coroutine(Of Integer)(Enumerable.Range(1, 100))
Dim query = From num In nums Where num Mod 3 = 1
Console.WriteLine("--- Numbers divisible by 3 with a remainder of 1 ---")
query.Start()
While query.Continue()
    Console.Write("{0}, ", query.Current)
End While
```

- **C#**:
```csharp
// Create a coroutine that yields Fibonacci numbers
Coroutine<int> fib = new(() =>
{
    static IEnumerable<int> Iterator()
    {
        (int prev, int curr) = (0, 1);
        while (true)
        {
            yield return curr;
            int temp = prev;
            prev = curr;
            curr += temp;
        }
    }
    return Iterator();
});

fib.Start();
Console.Write("[Fibonacci sequence] ");
for (int i = 1; i <= 10; i++)
{
    fib.Continue();
    Console.Write("{0}, ", fib.Current);
}
Console.WriteLine('\n');

// Create a coroutine that yields numbers from 1 to 100;
// Print all numbers that are divisible by 3 with a remainder of 1.
Coroutine<int> nums = new(Enumerable.Range(1, 100));
var query = from num in nums where num % 3 == 1 select num;
Console.WriteLine("--- Numbers divisible by 3 with a remainder of 1 ---");
query.Start();
while (query.Continue())
    Console.Write("{0}, ", query.Current);
```

Both VB.NET and C# examples produce the same output:
```
[Fibonacci sequence] 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 

--- Numbers divisible by 3 with a remainder of 1 ---
1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34, 37, 40, 43, 46, 49, 52, 55, 58, 61, 64, 67, 70, 73, 76, 79, 82, 85, 88, 91, 94, 97, 100,
```

## Setting the Cleanup Action

Use the `Cleanup` property to release resources when the coroutine completes:

```vb
Dim fs As New FileStream("test.txt", FileMode.Create)

Dim coro As New Coroutine(Of Char)(Iterator Function()
    Do
        For Each c In "Hello world!".ToCharArray()
            fs.Write(c)
            Yield c
        Next c
    Loop
End Function)

coro.Cleanup = Sub()
    fs.Dispose()
    Console.WriteLine("Resources released.")
End Sub
```

## Comparison with Lua
Suppose `coro` is an instance of `Coroutine(Of T)` (or `Coroutine<T>` in C#):

| Lua | This Package | Effect |
|-----|---------------|-------|
| `coroutine.create(f)` | `Dim coro As New Coroutine(Of T)(f)` | Creates a new coroutine |
| `coroutine.resume(co)` | `coro.Continue()` | Resumes the coroutine |
| `coroutine.resume(co, val)` | `coro.ResumeWith(val)` | Resumes with a value |
| `coroutine.status(co)` | `coro.IsRunning`/`coro.IsCompleted` | Checks the coroutine status |
| `coroutine.wrap(f)` | `Coroutine.AsDelegate(f)` | Wraps as a delegate |
| `coroutine.yield(val)` | `Yield val` | Yields a value |

**Additional API**:
- `coro.Current` - The current yielded value.
- `coro.Start()` - Starts the coroutine.
- `coro.ForceStop()` - Forces the coroutine to stop.
- `coro.Terminate()` - Terminates the coroutine.
- `coro.TryReset()` - Attempts to reset to initial state.
- `coro.Dispose()` - Releases resources.

## Installation
Install via NuGet:
```
Install-Package LuaStyleCoroutines
```
Or via .NET CLI:
```
dotnet add package LuaStyleCoroutines
```

## License
BSD-3-Clause License