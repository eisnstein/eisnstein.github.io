---
layout: post
tags: csharp
slug: pattern-matching-in-csharp-list-pattern
title: Pattern Matching In C# - List Patterns
description: How to use list pattern matching in C#.
---

In my last post I wrote about [pattern matching in C#]({% post_url 2022-05-10-pattern-matching-in-c# %}). One type of pattern matching I left out was the _list pattern matching_. I think this is a very handy and powerful one.

```csharp
int[] numbers = {1, 2, 3, 4, 5, 6, 7};
string msg = ExploreNumberList(numbers);
Console.WriteLine(msg);
//> first item discarded, 6 numbers left

string ExploreNumberList(int[] numbers)
  => numbers switch
  {
    [] => "empty list",
    [var first, var second] => $"a list with two numbers: {first}, {second}",
    [_head, ..int[] tail] => $"first item discarded, {tail.Length} numbers left",
    _ => "fallback arm if nothing else matched"
  };
```
