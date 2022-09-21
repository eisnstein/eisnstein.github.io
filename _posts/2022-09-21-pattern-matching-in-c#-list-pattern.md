---
layout: post
tags: csharp
slug: pattern-matching-in-csharp-list-pattern
title: Pattern Matching In C# - List Patterns
description: How to use list pattern matching in C#.
---

In my last post I wrote about [pattern matching in C#]({% post_url 2022-05-10-pattern-matching-in-c# %}). One type of pattern matching I left out was the _list pattern matching_. I think this is a very handy and powerful one.

```csharp
var emptyList = Array.Empty<int>();
string msg = SwitchNumberList(emptyList);
Console.WriteLine(msg);
//> empty list

int[] twoNumbers = { 1, 2 };
msg = SwitchNumberList(twoNumbers);
Console.WriteLine(msg);
//> a list with two numbers: 1, 2

int[] numbers = { 1, 2, 3, 4, 5, 6, 7 };
msg = SwitchNumberList(numbers);
Console.WriteLine(msg);
//> first item discarded, 6 numbers left

string SwitchNumberList(int[] numbers)
  => numbers switch
  {
    [] => "empty list",
    [var first, var second] => $"list with 2 numbers: {first}, {second}",
    [_, ..int[] tail] => $"first ignored, {tail.Length} numbers left",
    _ => "fallback arm if nothing else matched"
  };
```

In the preceding code we can see, that we can match against various list patterns. First there is the empty list `[]`. The second arm matches, if the list has exactly 2 numbers in it. Those 2 numbers will be captured by the `first` and `second` variables.
The next pattern is more interesting. First, it _ignores_ the first element with the _discard_ pattern `_`. This is a nice way to avoid _unused_ variables. Second, we capture the rest of the list in a new array `tail`. If you take a look at the [generated C# code](https://sharplab.io/#v2:EYLgtghglgdgNAExAagD4AECMAGABAZQHcoAXAYwAsA5AVzGAFMAnAGSgGcSAKWEgbQC6uGHUZN2ASgCwAKFzzcAXgB8w0c3a52xchVkLcAb30GFgpaoBEDMAAcSAT1wAbDiUtwTp3HwBuEJlwAMyhxEjhcf0D2BjIAexgEIRVcABJLV05cHQpcACZcUht2ECMQsIBfCMMY+MSKjy9TPgB9CIA6dt5zEmhnZNV08qyoAHMYOKYGBGreqGd2lgYYUZIKCrV6DRcGIPdPOW9cFotcSyCIZ2dgCDIAa1wAsEKg4Ti12FHcSF1pyyaKgBuIA){:target="\_blank" rel="noopener noreferrer"} of the `SwitchNumberList` function you will find a line which starts with `int[] subArray = ...`. This is the place, where the `tail` gets created (the remaining numbers get copied into the new array).

```csharp
List<string> words = new() { "this", "is", "an", "example", "text" };
string msg = SwitchStringList(words);
Console.WriteLine(msg);
//> this text

string SwitchStringList(IReadOnlyList<string> words)
  => words switch
  {
    [var first, .., var last] => $"{first} {last}",
    _ => "the list has less than two elements"
  };
```

List pattern matching also works on lists. In the example above we match a list of strings against this pattern `[var first, .., var last]`. Interesting here is the `..`. This is the _range pattern_ and matches zero or more elements. That means, that, if the list would look like that `{ "one", "two" }`, the returned message would be `"one two"`.

Ok, so here is the thing. In Elixir you can match on a string like that:

```elixir
auth_header = "Bearer this-is-the-auth-token"

result =
  case auth_header do
    "Bearer " <> token -> {:ok, token}
    _ -> {:error, "invalid auth header"}
  end
```

The `<>` operator in Elixir is basically the `+` from C#. I like this way of checking for a pattern in a string (just keep in mind, that this only works when the _static_ string is at the begining of the pattern). Good, enough of Elixir. What I was wondering, could we do something like that in C#? Yes, more or less. I came up with the following solution:

```csharp
string authHeaderValue = "Bearer this-is-the-auth-token";
string token = GetToken(authHeaderValue);
Console.WriteLine(token);
//> this text

string GetToken(string authHeader)
  => authHeader.Split(" ") switch
  {
    ["Bearer", var token] => token.Trim(),
    _ => throw new Exception("Invalid authorization header")
  };
```
