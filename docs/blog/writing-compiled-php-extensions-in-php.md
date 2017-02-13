> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [the Peachpie repository](https://github.com/iolevel/peachpie)

By Jakub Misek, 02/07/2012

PHP offers a lot of various extensions which add additional library functions, classes, constants and other constructs. Common extensions include for example php_mysql and php_iconv. capoten on line no presciption Since extensions are implemented in C language, the performance is great. It also allows programmers to use other native libraries that are not available in PHP. However there is one big disadvantage; writing such extensions is not easy. C code is harder to maintain, it requires learning lower-level language and it is easier to make mistakes that lead to program failures that are hard to handle.

Writing custom extensions is mostly done by companies requiring high performance code. Most of the normal libraries are written in PHP as a bunch of standard PHP scripts containing functions and classes. Users then include these libraries as normal, and the result is very similar. Writing libraries in PHP is much easier then writing extensions in C language, but it has significant performance costs. Also the author has to expose the source code of their library, which is not always desirable (I know, there is a obfuscation; but also deobfuscation).

# Phalanger approach

With Phalanger (the PHP compiler for .NET) developers can simply compile their PHP libraries as they are into a DLL file. Taking advantage of pure mode, the resulting compiled assembly can be used both from PHP scripts running on Phalanger or from any other .NET language.

# Extension advantages

So there are three approaches of extending PHP scripts with your library functionality. Every approach has some advantages. As you can see, Phalanger offers best of both worlds. You can:

- Write a bunch of PHP scripts as a “library”. Any PHP programmer is able to write such library. However library has to be processed and loaded every time it is used. Also you don’t have an access to native libraries written in other languages.
- Implement PHP extension in C language. This requires better knowledge of PHP internals and C language. The prize for that effort is maximum performance. Also you can take advantage of other libraries written in C/C++.
- Implement Phalanger extension in PHP langage. When you take your PHP library and compile it using Phalanger, the result is DLL working as any other extension. You are using simple PHP syntax, but you have access to all the .NET libraries including your own. The extension is fast, compiled, and loaded only once. Moreover Phalanger compiler optimizes usage of such extension more than usage of a bunch of PHP scripts.

# An example

The extension has to be compiled in pure mode. It has only one (logical) limitation – you are not allowed to write a global code. You can only declare global functions, global classes or global constants. Also there are no inclusions; all the source files are processed together, as one big script containing everything (the order of scripts is not important since there is no global code).

Imagine you need simple extension with one function. Something you cannot do efficiently in PHP :) , such as calling a .NET method. Sure you can call it directly from Phalanger-compiled PHP code without using an extension, but this is just for the demonstration:

```php
<?php
function getextension($fname) {
return System\IO\Path::GetExtension($fname);
}
// Expose domperidone without script constant or a class as follows:
// const MY_CONST = 123;
// class MY_CLASS{};
?>
```

The code can be compiled by the following command or from Visual Studio:

`phpc.exe /target:dll /pure+ /out:myex.dll /r:mscorlib ex.php`

The command references .NET library “mscorlib” where the System.IO.Path class is defined and compiles the “ex.php” script. Resulting assembly “myex.dll” can be used as any other Phalanger extension by adding following into the configuration:

`<add url='myex.dll' />`

Thats all. Functions, classes and constants defined in the extension will be available in the PHP code. The extension can be distributed compiled, so the source code is safe. Since it is loaded as an extension, the performance is not degraded; and it does not matter how big your extension is.

# Summary

This article is, of course, just a simple demonstration, but it shows a very powerful approach for extending PHP with extensions when using Phalanger. The approach described in this article has a number of applications – when porting applications from PHP to Phalanger, you can use it to reimplement a C extension that you’re using, so that you don’t have to change a single line of your program. You can also use it to wrap .NET functionality in a PHP-friendly way. Finally, the technique can be also used to re-implement standard PHP extensions that are not yet available in Phalanger (although, all major extensions are already available).

Complete example with sample usages is available at discussion forum. See PHP extension in PHP. Note you can use Phalanger Tools for Visual Studio if you don’t like command line :-)
