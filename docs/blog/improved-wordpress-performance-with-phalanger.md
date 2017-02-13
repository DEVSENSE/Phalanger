> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [the Peachpie repository](https://github.com/iolevel/peachpie)

By Jakub Misek, 05/09/2011

**Update: Latest benchmarks are depicted on Benchmarks page.**

One of the reasons why you may be interested in using Phalanger is that you need to run your PHP applications faster. Phalanger is a PHP language compiler for .NET. It is almost fully compatible with PHP, and it also adds several useful extensions. It makes it easy to integrate PHP applications with .NET or ASP.NET and it adds better compile time checking, but we’ll write about these in some other article.

For many people, the most interesting aspect of Phalanger is that it can be used to improve the performance of your existing PHP applications. We recently spent some time tuning the Phalanger performance and our recent check-ins implement several important optimizations. This article shows that Phalanger is more efficient than standard PHP interpreter and is comparable to the fastest solutions using e.g. WinCache extension.

A few days ago we published a tutorial explaining how to run WordPress on .NET 4.0 using Phalanger. In fact, we’re using this solution for many of our personal web sites, both as a demonstration and because it runs fast and it is very easy to deploy. When using WordPress, we use the latest (automatically updated) version, running as a native ASP.NET applications, with some plugins written in C#. The performance is comparable with other highly-optimized PHP solutions.

# Settings

There are many possible settings in Phalanger, mostly you don’t need to care about them. But if you would like a nice .NET assembly as a result and you care about every 1/10 percents, look on optimizing Phalanger. In the following test we configured Phalanger a little, so that the compiler can do the best possible job when compiling WordPress. We configured inclusion mappings so that the compiler can resolve all source files. This gave us about 4% performance improvement (which is great!), but we did not touch the PHP source code at all!

Also we used managed MySQL extension. There is a lot of data being copied from the database to the web application, and Phalanger calling .NET extensions from PHP is really fast in Phalanger. This way, the data access is as efficient as if the code was written in C#.

In practice, you can optimize WordPress using plugins that cache the output HTML. These plugins work well with Phalanger. They improve the page response time a lot, but we are not using them here. The aim of the test is to measure the raw performance when running PHP code. On our web sites, we don’t use caching plugins for a different reason – it is easier to simply turn on the automatic output caching in ASP.NET :-) .

# Benchmarks

We tested three different approaches on Windows: classic PHP 5.3.5 via FastCGI, PHP 5.3.5 via FastCGI with WinCache 1.1 extension and latest Phalanger 2.1 from May 2011. The WinCache extension caches the parsed opcode, accesses to file system and some other things. There are more similar extensions, but on the average all of them give 3 times better performance compared to classic PHP configuration.

To get some numbers, we used Visual Studio 2010 and its Load Test project; starting with 5 users up to 250 users at the same time. The users were continuously requesting the pages of a WordPress 3.1.2 site with only a few articles in it. Note that this doesn’t correspond to actual 250 human users, because the test runner requests the web site in a loop without waiting (and reading the article). The tests depicted below were run on Core 2 Duo, 2.4 GHz, 2MB cache, 4GB DDR3 RAM with Windows 7 Professional and IIS 7.5. The machine was a typical business notebook. A server machine would likely give a better performance, but the comparison would look similar.

![bench](Screen%20Shot%202017-02-12%20at%2021.21.16.png)

As you can see, Phalanger can be easily used as a PHP accelerator. In addition to that, it provides easy access to all the .NET features. Just to note, the tests were run on a development version of Phalanger, and we’re still able to increase the performance every few weeks.

Conclusion

In conclusion Phalanger has comparable performance with the fastest optimized PHP cached solutions. We also tested some other accelerators and the results were roughly the same.

Still, there are several parts of Phalanger that can be optimized and will be optimized in future. We expect this can give results better by additional tens of percents. The future enhancements include implementing more managed extensions, to avoid of using native reputable on line pharmacies PHP ones (that are unsafe and 32 bit only), and also more advanced compile time analysis.

Follow this web site for more information. In some future post we will take a look at a micro-benchmarking Phalanger and at the performance of other open-source PHP projects.
