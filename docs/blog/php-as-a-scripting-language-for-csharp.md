> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [here](www.github.com/iolevel/peachpie)

When creating .NET applications (including desktop and web applications), it may be useful to allow extending the application using some scripting language. The users of the application can write simple scripts to configure the application, modify how data is presented or write simple add-ins. In this article, we look how to use PHP as a scripting language. This has numerous benefits:

- Many people have some basic knowledge of PHP, so even less experienced developers can write simple PHP scripts for your application.
- PHP is very easy to use and there is a large number of ready-to-use PHP snippets on the internet that can be copied and used as a starting point.
- Thanks to Phalanger, PHP scripts can easily access any .NET libraries and call services provided by the rest of the .NET application.

The scenario described above is just one example of what can be done when using Phalanger from C# (or other .NET language) can i buy viagra at a store in pa to kamagra tablets evaluate PHP code at runtime. For example, you can imagine a web framework that uses C# to write the domain model and PHP to build the user interface. This article shows how to evaluate PHP code from C#, how to pass arguments to the PHP code using global variables and how to read result as a standard .NET stream.

Phalanger is PHP compiler into .NET byte code. It is designed to allow seamless interoperability with other .NET languages in both directions. This means you can call .NET methods and use .NET classes in PHP code, and you can call PHP functions and use PHP classes in C# (or F# :-) ). This article shows another possible usage of Phalanger: evaluating PHP code from a .NET application. This is useful when the code is obtained dynamically or cannot be precompiled into an assembly (e.g. when the code is written as script by users). When using PHP code that does not change, you should always use precompiled script libraries, which is more efficient as it doesn’t involve compilation at runtime.

# Configuration

I’ve tested this technique as a part of ASP.NET 4.0 C# web site. Of course, it will also work from a console or WinForms application too. Your program must use .NET 4.0 (full profile) and reference at least one Phalanger’s assembly: “PhpNetCore, Version=2.1.0.0, Culture=neutral, PublicKeyToken=0A8E8C4C76728C71″ . Phalanger must be properly configured from within context of your application. The easiest way to achieve that is to use the installer, but it can be also configured manually.

# The Code

The heart of the evaluation itself is, surprisingly, method PHP.Core.DynamicCode.Eval, that can be found in PhpNetCore.dll assembly. The only problem should be its enormous number of various parameters. First we will need valid PHP.Core.ScriptContext. It is Phalanger’s PHP code execution context. You can get one assigned to the current thread. Note PHP is not multi-threaded so the ScriptContext is associated with a single thread.

```php
var context = PHP.Core.ScriptContext.CurrentContext;
```

Than we can set the output, so the script will write to the stream we want. We are going to setup two outputs – byte stream and text stream. Note you should dispose the streams at the end, so all the data are flushed properly.

```php
context.OutputStream = output;
using (context.Output = new System.IO.StreamWriter(output)) {
```

We can also set some global variables in the context. So we can easily pass some parameters into the evaluated code.

```php
Operators.SetVariable(context, null, "X", "Hello World!");
```

Finally we can evaluate the PHP code using our Eval method. This method is actually used internally by Phalanger to process PHP eval() expression. That’s why it has so many various parameters.

```php
// evaluate our code:
return DynamicCode.Eval(
    code,
    false,/*phalanger internal stuff*/
    context,
    null,/*local variables*/
    null,/*reference to "$this"*/
    null,/*current class context*/
    "Default.aspx.cs",/*file name, used for debug and cache key*/
    1,1,/*position in the file used for debug and cache key*/
    -1,/*something internal*/
    null/*current namespace, used in CLR mode*/
);
```

Most of the parameters are not interesting if the evaluated code should behave as global PHP code. The most important parameter is the code. It is a string containing your PHP code. Phalanger will parse and compile the code. The resulting byte code is stored in in-memory assembly (called Transient assembly). The parsing and compilation itself are little bit slow so the resulting assembly is also cached to speed up repetitious evaluations of the same code. As you can see, you can also provide the file name and position in the file; so when you debug the code and you step into this expression, it will jump right there.

Note the cached assembly may depend on code previously evaluated on the same script context (e.g. declared classes and functions). That is why the cache is reused only if provided code, file name and position match. When you are evaluating more code fragments subsequently you should take it into account.

Eventually if you are using Phalanger from within the web application, you should initialize the PHP.Core.RequestContext first, and Dispose it when you are done with PHP.

```php
using (var request_context = RequestContext.Initialize(
                 ApplicationContext.Default,
                 HttpContext.Current))
{ /* all the stuff above */ }
```
The RequestContext finalizes all the PHP objects in its Dispose() method and also current ScriptContext. It is recommended to dispose it as soon as you can, and definitely on the same thread.

# That’s all

No more stuff is needed. After the evaluation the context also contains defined PHP functions, variables and classes so you can use them from your buy lopressor no script .NET code. Mixing the PHP language with the .NET ecosystem has numerous possible usages. PHP is an easy to use language that many developers know (or can easily learn), so it is a great language for extending existing .NET applications with plugins or user-defined scripts. You can also use this technique for creating web applications that use C# to create the domain model and PHP to build the user-interface. For more information, you can also check the Standard_mode_interoperability article which discusses the include statement.
