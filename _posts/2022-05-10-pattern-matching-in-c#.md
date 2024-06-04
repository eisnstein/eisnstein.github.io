---
layout: post
tags: csharp
slug: pattern-matching-in-csharp
description: How to use pattern matching in C#. A closer look at what is possible in C# 11.
---

Because I'm programming a lot in Elixir, I am used to the concept of pattern matching. It has become so convenient, that I sometimes miss it in other languages. The more it makes me happy every time new pattern matching capabilities are coming to C#. Pattern matching is part of C# since version 7.0 and has evolved since then.

## What is pattern matching?

Pattern matching means, that we can test that something fits onto something. What? Ok, let's take a sheet of paper and write the number **2** on it. Now take a second sheet and write the number **1** on it. Cut the numbers out of the paper and stack the **1** onto the **2**. If you look from above, does that match/fit? No. It does not perfectly overlap. Take a third sheet and write again the number **2** on it and cut it out. Stack this **2** onto the first **2**. Does that match/fit? Yes. So this is basically, and in a very simplified way, how I imagine and describe pattern matching. I do have something, let's say a `string` value, and I want to test it against some other `string` values. Again, like before, we take that value and stack it onto the first one of those other given values. If there is no difference, if it overlaps perfectly, we have a match.

```csharp
string myStringValue = "something";
string message = myStringValue switch
{
  "nope"      => "This is not going to match",
  "something" => "But this will match",
  _           => "Not so much either"
};

Console.WriteLine(message)
//> But this will match
```

In the example above, the `myStringValue` first gets tested/matched against the value `"nope"`. This clearly does not match. However, the second value, `"something"`, does match. So the value that gets returned is `"But this will match"`.

## Syntax

But let's first take a step back and talk about the syntax. In the preceding code I used the `switch expression` syntax. Maybe you are more familiar with the `switch statement` syntax, which, with the same example as above, looks like this:

```csharp
string myStringValue = "something";

string message = string.Empty;
switch (myStringValue)
{
  case "nope":
    message = "This it not going to match";
    break;

  case "something":
    message = "But this will match";
    break;

  default:
    message = "Not so much either";
    break;
}

Console.WriteLine(message)
//> But this will match
```

Let's look at the differences. First, I think it is save to say, that the `switch expression` is less code, more compact and easier to read. No `case`, `break` or `default` keywords needed. One big difference to keep in mind, is, that in `switch expressions` you cannot write multiple lines of code in the `switch arm`. The code after the `=>` needs to be an expression. And the result of that expression gets returned, therefore no need for the `return` keyword. Last but not least, `switch expressions` have to return a value.

```csharp
// NOT POSSIBLE

string myStringValue = "something";
string message = myStringValue switch
{
  "nope"      => {
    DoSomething();
    return "This is not going to match"
  },
  "something" => "But this will match",
  _           => "Not so much either"
};

// POSSIBLE

string myStringValue = "something";
string message = myStringValue switch
{
  "nope"      => NopeArmFunc(),
  "something" => "But this will match",
  _           => "Not so much either"
};

string NopeArmFunc()
{
  DoSomething();
  return "This is not going to match"
}

Console.WriteLine(message)
//> But this will match
```

Another way to match an expression against a pattern is by using the `is` operator. The `is` operator is for checking if a given expression is of a specific type. But since C# 7.0 it can also be used in pattern matching.

```csharp
// Match against a constant pattern

string value = "test";
if (value is "test")
{
  Console.WriteLine("Content of 'value' is 'test'");
}

// Match against a property pattern (more on that later)

string value = "test";
if (value is { Length: > 3 })
{
  Console.WriteLine("The length of the value is larger than 3.");
}
```

## Which patterns can be matched against in C#?

A lot. There is a whole list of patterns we can match against. We will explore some with examples in the next sections.

### Constant patterns

In the examples above, I used `string` literals as patterns and those belong to the **constant patterns**. In this category we can also find `integer`, `floating-point`, `char`, `boolean`, `enum` and the `null` as possible patterns.

```csharp
int grade = 2;
string message = grade switch
{
  1 => "very good",
  2 => "good",
  3 => "satisfying",
  4 => "enough",
  5 => "not enough",
  _ => throw new ArgumentException(nameof(grade))
};

Console.WriteLine(message);
//> good

bool result = true;
string value = MatchBoolean(result);
Console.WriteLine(value);
//> It's true

string MatchBoolean(bool arg)
  => arg switch
  {
    true  => "It's true",
    false => "It's false"
    // Here no 'default' arm is necessary, because `bool` can only be
    // `true` and `false`, and both cases are covered.
  };
```

### Relational patterns

Also very handy are **relational patterns**. There it is possible to use the `>`, `<`, `>=` and `<=` operators as well as the `and`, `or` and `not` pattern combinators to match a specific range or values.

```csharp
// An example with the `and` combinator

string msg = Temperature(18);
Console.WriteLine(msg);
//> warm

string Temperature(int temp)
  => temp switch
  {
    < -20           => "very cold",
    >= -20 and < 0  => "cold",
    > 0 and <= 10   => "a little cold",
    > 10 and <= 20  => "warm",
    > 20            => "hot",
    _ => throw new ArgumentOutOfRangeException(nameof(temp), $"{temp}")
  };

// An example with the `or` combinator

int value = GetCharScrabbleValue('a');
Console.WriteLine(value);
//> 1

int GetCharScrabbleValue(char c)
  => char.ToUpper(c) switch
  {
    'A' or 'B' or 'C' => 1,
    'D'               => 2,
    'E' or 'F'        => 3,
    _                 => 4
  };
```

### Type patterns

We can also match against types.

```csharp
Cat cat = new();
string value = MatchType(cat);
Console.WriteLine(value);
//> Hello Cat

string MatchType(Animal? animal)
  => animal switch
  {
    Cat  => "Hello Cat",
    Dog  => "Hello Dog",
    Bird => "Hello Bird",
    null => throw new ArgumentNullException(nameof(animal)),
    _ => throw new ArgumentException()
  };

interface Animal {}
record Cat : Animal;
record Dog : Animal;
record Bird : Animal;
```

### Property patterns

With this type of pattern matching we can check/match against properties or fields of objects. Even nested property matching is possible. And again we can use relational operators.

```csharp
string fullTitle = "Title: This is the title";
string title = GetTitle(fullTitle);
Console.WriteLine(title);
//> This is the title

string GetTitle(string value)
  => value switch
  {
    string { Length: >= 7 } s => s.Substring(7),
    _ => throw new ArgumentException("too short")
  };

// Object with nested properties
Address address = new("Streetname", "Vienna");
Person me = new("Me", address);
bool isFromVienna = IsFromVienna(me);
Console.WriteLine(isFromVienna ? "Yes, from Vienna", "No, not from Vienna");
//> Yes, from Vienna

bool IsFromVienna(Person p)
  => p switch
  {
    { Address.City: "Vienna" } => true,
    _ => false
  };

record Adress(string Street, string City);
record Person(string Name, Address Address);
```

A few things to mention regarding the preceding code examples. In the first example in the first **switch expression arm**, the input is matched against `string { Length: >= 7 } s => ...`. Two things are happening here. First, the incoming string value is checked to be at least 7 characters long and second, is then assigned to a new local variable `s`. This is actually the **var pattern**, which is useful if you need to create a temporary var for future use. In that case it is not really needed, but I showed it here for demonstration purposes.

In the second example with the **Person** record, we can see how nested property matching works. I think important to mention is, that when matching an object against a pattern, not the whole object needs to match, only those parts of the pattern. In that case `Address.City: "Vienna"`. Imaging the **me** object to look like that:

```
{
  Name: "Me",
  Address: {
    City: "Vienna,
    Street: "Streetname"
  }
}
```

Ok, that is a lot of pattern matching. But there is even more, which I have not covered here. For more details and more in-depth explanations I recommend you check out the documentation over at [docs.microsoft.com](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns){:target="\_blank" rel="noopener noreferrer"}.
