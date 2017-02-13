> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [the Peachpie repository](https://github.com/iolevel/peachpie)

By Jakub Misek, 03/06/2013

After several months of development, contributions from opensource community and collaboration with big commercial users, Phalanger is getting bigger. Today we’ve released package of Phalanger, containing many new extensions and latest integration for Visual Studio.

# New goodies in Phalanger

Mainly Phalanger is getting more managed extensions.

Biggest advantage of Phalanger is its platform; Phalanger is built completely on .NET, using safe code. No native unsafe C/C++ code is being used, and so Phalanger does not suffer from common bugs like memory leaks, buffer overruns and others. However to achieve compatibility with existing PHP applications, it needs to support a lot of PHP extensions. Until now the only way was to use mechanisms of remoting and DLL hijacking, so old native PHP4 extensions might be used in otherwise clean .NET environment.

Since this release, Phalanger contains more extensions implemented directly in .NET, so the whole process can run managed, eyeglasses without prescription 64bit (AnyCPU if you want) fully taking advantage of .NET framework. See following section for new extensions.

Beside new extensions, Phalanger gets more PDO and SPL functions/classes and more PHP 5.4 and 5.5 (short array syntax, function array dereferencing, indirect static method call, binary number format, new callback options, boolval() function).

# Complete package for development

Phalanger Setup contains everything developer may need for running common PHP web or application under .NET with all its advantages.

Phalanger supports more extensions now, so it allows many more popular PHP projects running natively on .NET, without any unsafe native code.

- Class Library (PhpNetClasslibrary.dll) is basic part of Phalanger containing basic set of functionality (standard,Core,session,ctype,tokenizer,date,pcre,ereg,json,hash,SPL,filter).
- cURL (new) – for most common tasks, Phalanger now comes with cURL extension suporting HTTP/HTTPS protocols. Community is now free to extends its functionality as they need.
- GD2, exif and image (new) are well known PHP extensions allowing to read/manipulate with images.
- Iconv (new) for string encoding conversions built on .NET Encoding.
- MSSQL is Microsoft SQL extension using SqlConnection internally to increase performance. It also ensures compatibility with latest Microsoft SQL servers.
- PDO (new) is an abstraction over PHP database connections. Support for PDO was added, containing several DB drivers like SQLite or MySQL. Developers are free to extend PDO support with additional DB drivers now.
- SoapClient (new) is managed reimplementation of PHP SOAP taking advantage of .NET built-in SOAP support.
- SQLite (new) is another DB extension for Phalanger.
- MySQL extension for Phalanger takes advantage of latest managed Oracle/.NET connector. This makes DB operations faster and safer, allowing to configure additional options and security options in standard .NET-way.
- XML (new) extension is now contained in Phalanger too. Must-have extensions commonly used for its utf8 functions.
- XMLDom extension contains support for PHP SimpleXML, dom, xsl and libxml extensions. Its feature set was extended by libxml functions and improved HTML parsing functions. The extension takes advantage of .NET XML built-in support which offers great performance and security.
- Zip (new) extension was added thanks to community contributions. Anyway still needs some work to be finished.
- Zlib (new) extension is essential part of many PHP projects, mainly because of its gzip compression support. A part of Phalanger now.

To simplify development, Phalanger comes with its integration for Visual Studio. This allows to create .NET desktop applications written in PHP, or a WebSite running on ASP.NET and compiled seamlessly from PHP. See Visual Studio Gallery for more information. Note the integration requires Phalanger to be installed using its Setup.

# Commercial Services

Phalanger is still a free software. Companies and single developers may consider visiting commercial services page for information about migration assistance, technical support or feature requests.

As a part of commercial services you can also obtain additional memcached extension for Phalanger, which was not open-sourced currently.

# Mono runtime

Don’t forget about Mono; thanks to cooperation with Mono core developers, Phalanger runs on Mono fluently. This helped both Phalanger and Mono to improve their stability and reliability.

# Future plans

Phalanger is growing every day. Currently there are big plans about another increase of compiled apps performance. More smart compiler stuff will be introduced, and more compiler purchase actos online configuration options will be designed.

Of course every day task is to keep up with new PHP features; Anyway great thing about Phalanger is it takes advantage of .NET, ASP.NET and IIS – most of PHP issues are simply not possible on these platforms which are continuously developed by Microsoft. In future Phalanger will get missing PHP 5.4 sweets like traits and closures rebinding.

Stay tuned on twitter or facebook group for more details. Thanks!
