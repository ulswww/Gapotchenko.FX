﻿# Gapotchenko.FX
`Gapotchenko.FX` is the main module of Gapotchenko.FX framework. Coincidently, it has the very same name.



The module was started by creating its first building block: `ArrayEqualityComparer`.

Sure enough, .NET provides a similar `Enumerable.SequenceEqual` method (`System.Linq`) that allows to check two sequences for equality, however it is very limited:
* It is slow, and puts a pressure on GC by allocating iterator objects
* It does not treat `null` arguments well
* It does not provide an implementation of `IEqualityComparer<T>` interface.
Good luck trying to make something like `Dictionary<byte[], string>`.

Many years had passed until it became clear that original platform maintainer is not going to solve that.

As you can imagine, this whole situation gave an initial spark to Gapotchenko.FX project.

What if we have an `ArrayEqualityComparer` that does the job out of the box?
What if it does the job in the fastest possible way by leveraging the properties of host CPU and platform?

No problem. Now we have it:

``` csharp
using Gapotchenko.FX;
using System;

var a1 = new byte[] { 1, 2, 3 };
var a2 = new byte[] { 1, 2, 3 };
bool f = ArrayEqualityComparer.Equals(a1, a2);
Console.WriteLine(f);
```

And what about `Dictionary<byte[], string>`? Here it is:

``` csharp
var map = new Dictionary<byte[], string>(ArrayEqualityComparer<byte>.Default);

var key1 = new byte[] { 1, 2, 3 };
var key2 = new byte[] { 110, 230, 36 };

map[key1] = "Easy";
map[key2] = "Complex";

Console.WriteLine(map[key1]);
```

## Functional Influence

Gapotchenko.FX has a strong influence from functional languages and paradigms,
so it's important to keep that in mind when we study its main `Gapotchenko.FX` module.

Some concepts may seem a bit odd at first look.
However, they allow to reap the **great** benefits. Let's see how and why that happens.

Please note that Gapotchenko.FX is not an idiomatic functional kernel like the one you might expect in some languages like Haskell.
Instead, Gapotchenko.FX is a mass-market product that uses the benefits of functional style up to the point where it remains beneficial.
It also does OOP and some other techniques as long as they are bringing the benefit at a particular scenario.

## Optional Values

.NET provides a notion of nullable values. For example, a nullable `int` value:

``` csharp
int? x = null;
if (!x.HasValue)
	x = 10;
Console.WriteLine(x);
```

But what if we want to do that with a reference type like `string`? Actually, we can:

``` csharp
string s = null;
if (s == null)
	s = "Test";
Console.WriteLine(s);
```

Unfortunately, the scheme breaks at the following example:

``` csharp
class Deployment
{
	string m_CachedHomeDir;

	public string HomeDir
	{
		get
		{
			if (m_CachedHomeDir == null)
				m_CachedHomeDir = Environment.GetEnvironmentVariable("PRODUCT_HOME");
			return m_CachedHomeDir;
		}
	}
}
```

If `PRODUCT_HOME` environment variable is not set (e.g. its value is `null`), then `GetEnvironmentVariable` method will be called again and again
diminishing the value of provided caching.

To make this scenario work as designed, we should use an `Optional<T>` value provided by Gapotchenko.FX. Like so:

``` csharp
using Gapotchenko.FX;

class Deployment
{
	Optional<string> m_CachedHomeDir;

	public string HomeDir
	{
		get
		{
			if (!m_CachedHomeDir.HasValue)
				m_CachedHomeDir = Environment.GetEnvironmentVariable("PRODUCT_HOME");
			return m_CachedHomeDir.Value;
		}
	}
}
```

Optional values are pretty common in functional languages. And they are simple enough to grasp.
But let's move to a more advanced topic - a notion of emptiness.

## Notion of Emptiness

Functional style is very similar to Unix philosophy.
There are tools, they do their job and they do it well.
Those Unix tools can be easily combined into more complex pipelines by redirecting inputs and outputs to form a chain.

Functional programming is no different.
There are primitives, and they can be combined to quickly achieve the goal.
Due to the fact that underlying primitives are well-written and have no side effects, the combined outcome also tends to be excellent.

Let's take a look at the notion of emptiness provided by the `Empty` class from `Gapotchenko.FX` assembly.

The basic thing it does is nullifying. Say, we have the following code:

``` csharp
using Gapotchenko.FX;

class Deployment
{
	Optional<string> m_CachedHomeDir;

	public string HomeDir
	{
		get
		{
			if (!m_CachedHomeDir.HasValue)
				m_CachedHomeDir = Environment.GetEnvironmentVariable("PRODUCT_HOME");
			return m_CachedHomeDir.Value;
		}
	}
}
```

It's all good, but in real world the `PRODUCT_HOME` environment variable may be set to an empty string `""`
on a machine of some customer.

Let's improve the code to handle that condition:

``` csharp
using Gapotchenko.FX;

class Deployment
{
	Optional<string> m_CachedHomeDir;

	public string HomeDir
	{
		get
		{
			if (!m_CachedHomeDir.HasValue)
			{
				string s = Environment.GetEnvironmentVariable("PRODUCT_HOME");
				if (string.IsNullOrEmpty(s))
				{
					// Treat an empty string as null. The value is absent.
					s = null;
				}
				m_CachedHomeDir = s;
			}
			return m_CachedHomeDir.Value;
		}
	}
}
```

It does the job but that's a lot of thought and code.

We can do better with `Empty.Nullify` primitive:

``` csharp
using Gapotchenko.FX;

class Deployment
{
	Optional<string> m_CachedHomeDir;

	public string HomeDir
	{
		get
		{
			if (!m_CachedHomeDir.HasValue)
				m_CachedHomeDir = Empty.Nullify(Environment.GetEnvironmentVariable("PRODUCT_HOME"));
			return m_CachedHomeDir.Value;
		}
	}
}
```

A simple one-liner.
We used the `Empty.Nullify` primitive, combined it with `Optional<T>` primitive and got a quick, excellent result.

## Lazy Evaluation

Most .NET languages employ eager evaluation model. But sometimes it may be beneficial to perform lazy evaluation.

.NET comes pre-equipped with `Lazy<T>` primitive that does a very good job.
However, during the years of extensive `Lazy<T>` usage it became evident that there are a few widespread usage scenarios where it becomes an overkill.

First of all, `Lazy<T>` is a class, even in cases where it might be a struct.
That means an additional pressure on GC.
Secondly, `Lazy<T>` employs a sophisticated concurrency model where you can select the desired thread-safety level.
That means an additional bookkeeping of state and storage, and thus fewer inlining opportunities for JIT.

Gapotchenko.FX extends the .NET lazy evaluation model by providing the new `LazyEvaluation<T>` primitive.
`LazyEvaluation<T>` is a struct, so it has no memory allocation burden.
It also uses a simple concurrency model which relies on consistency guarantees of .NET memory model.

The sample below demonstrates a typical usage scenario for `LazyEvaluation<T>`:

``` csharp
using Gapotchenko.FX;
using System;

class Program
{
	public static void Main()
	{
		var r = LazyEvaluation.Create(() => new Random().Next());
		// ...
		// Use 'r' value somewhere in the code.
	}
}
```