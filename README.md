# SafeParallelForEach
This is a micro library that solves a single problem: How to run many async tasks in parallel without running too many at the same time - and without running out of memory.

The principles are simple and it's not many lines of code, but we found that every time we had to do this, it required too much thinking and it took too long, even with a code example in our development guidelines, hence this micro library. Feel free to use it from Nuget or just copy the relevant lines of code into your project.

The original inspiration for this came from a [tweet](https://twitter.com/clemensv/status/831462231808339971) by Clemens Vasters. 

# Why
One of the very nice things in C# Async is that it makes it easy to do things in parallel. For example, if you need to write 100 messages to a queue, you can do it in parallel instead of one by one, effectively speeding up your code by 100x. 
But;
- If you do too many network writes in parallel, you will exhaust your sockets and effectively take your server down.
- If you do too many operations in parallel, you may overload the backend you are talking to.
- If you need to run a very large number of operations, you may use too much memory storing all the Task objects and may exhaust the maxium size of lists.

This micro library:
- Provides extension methods for `IEnumerable` and `IAsyncEnumerable` to run an async `Func` over each item in the list. 
- It will run the `Func` in parallel up to a maximum you specify. 
- It will not try to load all of the items from the enumerable into memory or do any aggregations on it so this can be safely used to "stream" data, for example reading millions of records from one database and writing them to another.
- It will make sure the `Task` objects go out of scope on a regular basis so you don't consume too much memory, nor maintain references to values from the enumerable.

# Background information
I have given a talk about the how async in C# *actually* works (it is probably different to what you think) a few times. A recording is available [on my blog](https://www.lytzen.name/2019/04/29/Everything-I-thought-I-knew-about-async-was-wrong.html)

## Installation
TO BE DONE!
```cmd
dotnet add SafeParallelForEach
```

## Usage
If you wanted to just write all the items in an `IEnumerable` to a queue, you could do something like this:
```csharp
    await enumerable.SafeParallel(async msg => await queueWriter.Write(msg));
```
By default, this will write 100 messages in parallel. It will not wait for all 100 to finish, but will keep writing so that at any one time there are 100 writes in operation.

At the other end of the scale, you can chain things and handle cancellations like this:




See the [Wiki](https://github.com/NewOrbit/SafeParallelForEach/wiki) for more scenarions, including more details on cancellation and potential threading issues.

*Example usage and code snippets here.*
