> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [the Peachpie repository](https://github.com/iolevel/peachpie)

Phalanger is a complete reimplementation of PHP, written in the C# language. It was always being developed with the Mono platform in mind. This means you can compile and run PHP application on Linux web servers using Mono. Since Phalanger 3.0, this become more official, periodically tested and maintained.

# Notes

Mono since 2.10.8 contains few fixes that allow running Phalanger powered applications.

Mainly it fixes the recursive ReaderWriterLockSlim issue, which disallowed Phalanger in some special cases. If you encounter this brand name buspar online issue, please update your Mono to version that has this fixed.

# Installing Phalanger on Linux

Briefly, see configuration and add listed configuration options into your web.config file. Dependant Phalanger’s assemblies copy into Global Assembly Cache using “mono gacutil.exe -i” util. You will need PhpNetCore.dll, PhpNetClassLibrary.dll and required extensions (e.g. PhpNetMySql.dll, PhpNetSimpleXml.dll).

The rest of configuration is the same as cialis at optum rx for ASP.NET 4.0 web on Mono.

# Too short?

This post is more an announcement than a tutorial of installing Phalanger on Mono. Phalanger installer for Linux will be published soon, so you don’t have to care about installing :-)
