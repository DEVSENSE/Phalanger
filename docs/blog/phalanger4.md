> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [here](https://github.com/iolevel/peachpie)

After the long break we are happy to announce progress on Phalanger and the new version 4.0. There are some major changes in functionality and planned changes in Phalanger API. Following blog post should summarize them and describe Phalanger 4.0 advantages and improvements.

In previous releases most of the issues were caused by complexity of the compilation process and the need of low level configuration of ASP.NET and Phalanger to achieve working Phalanger application. With Phalanger it was possible e.g. to build WinForms, ASP.NET Forms, console apps, class libraries; in two compilation modes with different parser settings.

Phalanger 4.0 will simplify the process of compiling and running PHP applications. Not used features will be deprecated. New PHP 5.5 features may be added and the compilation process will be improved to achieve better performance in run-time.

# Major changes

- **Support for native PHP 4 extensions was removed.** It is no longer possible to wrap native C extension for PHP 4 into a .NET assembly and use it is an extension for Phalanger. PHP 4 extensions caused lagging and memory leaks. Its configuration and use is confusing, with the need to run in 32 bit mode.

- **Precompiled Web Sites are automatically loaded from /Bin folder.** It is possible to compile a bunch of PHP scripts into a .NET assembly. It is no longer needed to name such assembly “WebPages.dll” and it is no longer needed to list such assemblies within Web.Config file.

- **Distributed with Phalanger Tools for Visual Studio.** Since debugging, building new Web Projects and configuring go together, Phalanger will be distributed with up-to-date add-in for Visual Studio. This allows users to update Phalanger through Visual Studio extensions manager, keep both products in sync and work better with their Phalanger projects.

- **Up-to-date Tools for Visual Studio.** New Phalanger Tools are built on top of PHP Tools for Visual Studio. Thanks to this integration Phalanger Tools will take advantage of continuously improved features of PHP Tools. Based on huge users feedback, the integration gets faster and more intuitive.

- **New API for parsing and compiling.** Phalanger is about running PHP projects under .NET, integrating with .NET and improving performance of PHP code. Integrating with .NET projects is very important and unique feature. This release of Phalanger introduces new API that will be subsequently improved and documented. This allows developers to do frequent tasks with PHP code from .NET languages like C# or VB.

# Future changes

- **Improved performance.** New API allows to bring better code analysis and to produce much faster compiled assemblies. Also thanks to the missing support for native PHP 4 extensions, it will be possible to improve internals of Phalanger, including application startup and more.

- **Built-in obfuscation.** Phalanger is being frequently used as a PHP obfuscation tool. With this release of Phalanger, Phalanger Tools will come with Obfuscate option to protect compiled code, string literals, functions body and names of function arguments and name.

- **Easier development of extensions.** Since this release, it will be easier to implement missing extensions for Phalanger in .NET or PHP itself. This will be achieved by new project templates for Visual Studio and simplified interface for a Phalanger extension.

The list above gives quick look on major changes and planned improvements. Any new progress will be announced on Facebook, Twitter or here. The release will be available soon at CodePlex or newly at Visual Studio Gallery.

# Resources

Download: http://www.devsense.com/products/phalanger-tools/download/preview  
Getting Started: http://www.devsense.com/products/phalanger-tools/getting-started  
CodePlex: http://phalanger.codeplex.com/  
Twitter: https://twitter.com/phpcompiler
