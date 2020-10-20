# SafeParallelForEach
This is a micro library that solves a single problem: How to run many async tasks in parallel without running too many at the same time - and without running out of memory.

The principles are simple and it's not many lines of code, but we found that every time we had to do this, it required too much thinking and it took too long, even with a code example in our development guidelines, hence this micro library. Feel free to use it from Nuget or just copy the relevant lines of code into your project.

The original inspiration for this came from a [tweet](https://twitter.com/clemensv/status/831462231808339971) by Clemens Vasters. We have used that code many times, but each time we have to stop and think for too long - so it felt sensible to do a micro library that, effectively, just provides that code without lots of other stuff.   
There are also scenarios like very large enumerables, return values and the fancy new `IAsyncEnumerable` that I wanted to support.

# Installation
TO BE DONE!
```cmd
dotnet add SafeParallelForEach
```

# Usage
If you wanted to just write all the items in an `IEnumerable` to a queue, you could do something like this:
```csharp
    await enumerable.SafeParallel(async msg => await queueWriter.Write(msg));
```
By default, this will write 100 messages in parallel. It will not wait for all 100 to finish, but will keep writing so that at any one time there are 100 writes in operation.

At the other end of the scale, you can chain things and handle cancellations like this:
```csharp
    var cancellationToken = new CancellationToken();

    var process = idList.SafeParallelWithResult(id => dbReader.ReadData(id), 30)
                    .SafeParallelWithResult(readResult => dbWriter.WriteData(readResult.Output), 100)
                    .SafeParallel(writeResult => queueWriter.Write(writeResult.Output.SomeDescription), 50, cancellationToken);

    await process;
```
The extension methods work for both `IEnumerable` and `IAsyncEnumerable`.

The `SafeParrallelWithResult` methods return a result object you can query - or you can call this extension again to process the results. WithResult returns an `IAsyncEnumerable` so you can use the `.WithCancellationToken` syntax, even when your input is a normal `IEnumerable`. 
They have error handling and will catch exceptions; the exception will be returned on the result object. 

The `SafeParallel` methods return a plain `Task` that you can await. This method will not catch errors - if anything throws an exception, it will propagate to your code and interrupt the processing. 

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
I have given a talk about the how async in C# really works (it is probably different to what you think) a few times. A recording is available [on my blog](https://www.lytzen.name/2019/04/29/Everything-I-thought-I-knew-about-async-was-wrong.html)







See the [Wiki](https://github.com/NewOrbit/SafeParallelForEach/wiki) for more scenarions, including more details on cancellation and potential threading issues.

# What about...
`Task.Parallels.ForEach`? To my understanding, that method is more about threads (do correct me if I am wrong) whereas this micro library is exclusively about async. 

