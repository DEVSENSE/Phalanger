> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [the Peachpie repository](https://github.com/iolevel/peachpie)

By Miloslav Beno, 12/10/2012

The main goal of Phalanger is to provide fast and easy means for PHP and .NET languages to interoperate with each other. When using Phalanger, PHP becomes a .NET language, but it’s still a dynamic language. That means that, in order to communicate with strongly typed languages as C#, we need more sofisticated architecture. This comes with DLR (Dynamic Language Runtime) and dynamic keyword in C#.

So far we had only few choices how to allow C# and PHP to communicate:

- Pure mode – PHP gets compiled as standard .NET assembly http://wiki.phpcompiler.net/Pure_mode. Therefore it is easy to use PHP code from C#, but PHP code can’t use global code and inclusions.
- Duck typing – PHP code can be standard with global code and inclusions. But you have to prepare special strongly-typed interfaces in C#. More details can be found here: http://tomasp.net/blog/ducktyping-in-phalaner.aspx. It is also possible to generate these interfaces from documentation comments.

With Phalanger 3.0 (January 2012) we have one more choice and so far it looks like most convinient one for most of the cases. This is a benefit of incorporating the .NET interoperability model provided by DLR. Using the DLR, it is much easier to call PHP functionality from C# using the dynamic keyword. In one of the previous posts my collegue explains how to use dynamic keyword in C# in combination with Phalanger 2.1. Some of it has changed since, so I want to show how it looks with the current release.

# Globals

In Phalanger 3.0 we have implemented convenience dynamic object for simple access to global variables, functions etc. Motivation for this object is that .NET world is fully object-oriented and there is no way to invoke global functions or access global variables from C#.

You can get the instance of the object representing PHP globals from ScriptContext:

`dynamic globals = ScriptContext.CurrentContext.Globals;`

Following operations are currently available in this object:

- globals.x - Global variable access.
This construct assign or read global variable. If variable doesn’t exist it gets created.
PHP: $x
- globals.foo(arg1,arg2,...argn) - Global function invocation.
Global PHP function foo gets invoked with the given arguments and variable is returned (if supplied in PHP function). All the necessary conversions are taken cared of.
PHP: foo(arg1,arg2,...argn)
- globals.@const.c – Global constant access.
Global constants can be assigned or readed like this or created if the constant doesn’t exist. The @const syntax is necessary to avoid collisions with variables.
PHP: c
- globals.@class.PhpClass(arg1,arg2,...argn) - how much does viagra cost at walgreens Creates a new instance.
The constructor of a class named “PhpClass” is called and new PHP instance is returned which has to be typed as dynamic in C#. Again, the @class syntax is necessary to avoid collisions with global function.
PHP: new PhpClass(arg1,arg2,...argn)
- globals.@class.PhpClass.x – Static variable access.
This construct assigns or reads static class variables of a PHP class named PhpClass.
PHP: PhpClass::$x
- globals.@class.PhpClass.foo(arg1,arg2,...argn) - Static method invocation.
Invokes a static method foo of a PHP class named PhpClass
PHP: PhpClass::foo(arg1,arg2,...argn)
- globals.@class.PhpClass.@const.x - Class constant access.
Class constant can be assigned or read or created if doesn’t exist.
PHP: PhpClass::x

All the constructs mentioned above work well also if PHP namespaces are used, it’s just necessary to specify the namespace using a special @namespace syntax: globals.@namespace.ns1.ns2.*

For example, globals.@namespace.ns1.ns2.@class.MyClass() – creates an instance of MyClass which is in ns1.ns2 namespace. PHP: new ns1/ns2/PhpClass().

Note: If your PHP is code compiled 
in pure mode using Phalanger(http://wiki.phpcompiler.net/Pure_mode). You don’t need the globals class at all, as you can access compiled PHP classes directly from C#.

# Object Operations

In .NET, an object is the king and although some PHP applications still use procedural approach, there is a plenty of applications built using objects. In this part of the post I’ll explain basic operation on PHP objects that can be used from C# (or any other .NET language that supports DLR).

# Create Instance

With globals object explained above you can create a new instance of class X with this construct:

`dynamic x = globals.@class.X(arg);`
In a previous version of Phalanger, this was also possible, but using a more complex syntax. You could create a new instance of PHP class by calling New method on ScriptContext.

# Method Invoke

When we have dynamic object x, we call method just as we are used to in C#.

`result = x.Hello(arg1,arg2,...argN);`

Arguments can be any value type, reference type or strongly typed delegate which allows you callbacks from PHP code to C#. If PHP object is expected to be returned it has to be assigned to dynamic type.

You can also expect PHP magic method __call to work. If PHP object doesn’t contain method which is called and defined __call method, the method gets invoked with suplied arguments.

The current version of Phalanger does not yet implement the interoperability in the other direction (calling DLR objects from PHP code in Phalanger). This means that dynamic objects defined in C#, IronRuby or IronPython won’t work as expected from PHP code now, only standard CLR objects. This functionality is planed in a future release.

# Get and Set Property

Getting or setting properties on PHP objects from C# works as you’d expect.

```php
x.Field = 7;  
int i = x.Field;
```

This works, but you have to be sure that x.Field contains int (or any other type you assign this field to), otherwise runtime exception will be thrown. We are considering implicit conversions with PHP semantics to avoid this in the future. But for the reference types it’s usually sufficient just to use C# operator as.

If you assign to the field that doesn’t exist runtime field is created or if PHP object defines special method __set(), the method will be called with two arguments, name of the assigned field and value to be assigned. Same principle applies

Few friends and stamper large others singles dances battle creek I is much a my antique sofa dating marketed I up says. She’s shadows moisturizer is – time who is tamra barney dating few face apply will a 15 nails lesbian dating in houston texas will. Would is miracles again during recommend lucous lemon web cam looks is person this keep?
when reading a value from field and object defines __get() method.

```php
function __set($name,$value){ }  
function __get($name){ }
```
# Invoke on the object

In Phalanger 3.0 it’s possible to invoke PHP object that implements __invoke method or PHP delegate, but how? :-) . The code is surprisingly simple:

```php
dynamic phpObj = globals.@class.PhpClass();
phpObj(arg1,arg2,...,argn);
```
The method __invoke gets called and any number of arguments can be supplied.

```php
function __invoke($arg1,$arg2,...,argn){}
```

There’s however one limitation. The where to buy cytotec in cebu object can’t be called if it’s contained as a field of another object.

```php
phpObj(); //this works
y.Field = phpObj;
y.Field(); // this won't work
```
The reason for this not to work is that we can’t be sure if you want to
invoke a method or invokable field.

# Sample

I’ve created small PHP web site as a sample. The web site consits of three important files:

default.php – contains definition of PHP class PhpFriend  
CsFriend.cs – This file is located in App_code folder and contains class CsFriend written in C#. This class gets automatically loaded when the AppPool starts.  
web.config – Configuration of web site which in  
system.web/compilation/assemblies configration section adds PhpNetCore assembly, so it can be used from C# code  

Default.php also contains global code which creates an instance of CsFriend C# class which is in namespace CsLib and calls its method called run.

```php
$cs = new \CsLib\CsFriend();
$cs->run();
```

The run method just contains few demonstration of the operations I’ve explained here.

First we want to get PHP output stream, which is necessary to use for outputs. Because we want C# to output everything to the same stream as PHP, in order to PHP output control functions to work properly http://www.php.net/manual/en/ref.outcontrol.php . Then we’ll get the Globals object.

```php
TextWriter output = ScriptContext.CurrentContext.Output;
dynamic global = ScriptContext.CurrentContext.Globals;
```

All the other parts of the sample are with comments so I won’t be explaining them here.

# Conclusion

When I say PHP and .NET interoperability, there are two sides of the coin. First one solves how to use .NET in PHP (PHP -> .NET) and the other one solves how to use PHP world from .NET world (.NET -> PHP) which I was explaining in this post. Phalanger 3.0 and .NET with its dynamic type gives us an oportunity to use PHP code from .NET world in a very convinient way.

