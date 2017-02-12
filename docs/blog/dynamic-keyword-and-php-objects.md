> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [the Peachpie repository](https://github.com/iolevel/peachpie)

We’ve already demonstrated, how to use PHP scripts from within a .NET application using Phalanger. For reference you can take a look at PHP as a scripting language for C# article or Standard mode interoperability tutorial. In this way we can take an existing PHP web or a library, load them into C# context and reuse their functions, classes, constants or global variables. In addition we can even define new functions and classes in C# and inject them into PHP, so the code in PHP seamlessly uses these declarations as they would be declared in PHP too.

EDIT: Phalanger 3.0 removes all the limitations mentioned below.

In this post I would like to show you, how to easily reuse objects defined in PHP code from C# environment – by using “dynamic” keyword introduced in C# 4. Since Phalanger translates PHP classes into general .NET types, the usage is very intuitive. However there is one catch …

For this demo, I have prepared ASP.NET application, that loads PHP scripts. First it loads a PHP script into C# context:

```php
context.Include("script.php", false);
```

This “runs” script.php, and as a result context contains all the PHP declarations. At this point, you can e.g. take a look on context.DeclaredTypes, and list declared PHP types.

Now we can instantiate a PHP type, and take advantage of the dynamic keyword. Following statement instantiates a PHP class “X”, and passes its reference into a C# dynamic variable:

```php
dynamic x = context.NewObject("X");
```

Then we can call methods on X class as we are used to. C# compiler automatically implements polymorphic inline cache (the same as we used in Phalanger compiler earlier) in run time, to determine what method should be called and to cache the call, so repetitious calls are executed faster.

```php
var result = x.bar(context, 11);
```

You can notice the additional first argument here which is the only catch you should keep in mind. Phalanger requires the current context to be passed as the first argument in every method call. It is used later internally, mostly to enhance runtime performance.

EDIT: Phalanger 3.0 does not require the context to be passed.

# Limitations (Phalanger 2.1)

Since this is the fastest way how to call PHP methods from C# context, several internal conversions between PHP and .NET world are skipped. It means you should only pass PHP/Phalanger compatible types as an argument: int, long, bool, string, PHP.Core.PhpBytes, PHP.Core.PhpArray and PHP.Core.Reflection.DObject (base class for all PHP objects, so any other PHP class instance is allowed).

# Conclusion

In this post I have demonstrated how to take advantage of the “dynamic” keyword in C# so you can intuitively use your PHP objects in C# application. Please download attached ASP.NET demo to see it working. In addition to that, Phalanger allows many other ways of interoperability, e.g. the strongly typed Duck Typing.
