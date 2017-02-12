> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [here](www.github.com/iolevel/peachpie)

We are happy to announce that Phalanger 2.1 for .NET 4 (August 2011), our PHP language compiler, takes advantage of Dynamic Language Runtime (DLR) which is present in .NET 4.0 Framework and Mono.

We’ve decided to use DLR for a few PHP operations in order to improve their performance. So far, the operations that use DLR are field read access and instance method invocation. Using DLR improved the performance significantly, for some operations we measured even more than 6x performance improvement. This significant progress was possible because of DLR caching system which is actual implementation of polymorfic inline cache.

Before DLR, we were classifying operations into two cases (not counting eval), bound during compilation and bound during run time. The goal for Phalanger as dynamic language compiler is to compile as generic allegra at costco much as possible as compile-time bound operations and use runtime-bound operations only for cases that can’t be determined during compilation. DLR caching system allows compiling operation at run time when we know particular types for the operation and store compiled operation into cache. This can work efficiently because of the idea that when particular operation occurs, there is a big chance that the next time operands will have the same type.

# Benchmarks

Following picture shows some selected micro-benchmarks. Each test of an operation was performed ten million times on Core i7 2600, 3.5 GHz, 16GB DDR3 desktop machine, running Windows 7 64 bit with .NET 4.0. You can clearly see the progress we’ve made with Phalanger in this release. where can i buy levothyroxine The chart shows time required to run the operation 10 million times (so a smaller value is better):

![microbenchmarl](https://github.com/bfistein/Phalanger/blob/master/docs/blog/microbenchmark.png)

You may wonder why Phalanger performs static operations so efficiently. The reason is that operation is bound during compilation. At run time there are just few CPU instructions going on. In dynamic operations where we need to bind the operation at run time, we have to do more stuff, but still it’s pretty fast now thanks to DLR.

Our slowest operation at the moment is static method indirect call which is not using DLR. The reason is this operation isn’t frequently used in any PHP application I know. Anyway we are planning to improve it in the future.

If you want to try this benchmark by yourself you can get its sources from the source code repository on CodePlex from \Testing\Benchmarks\Micro directory.

# Future work

August release is just the beginning of Phalanger’s incorporation with DLR. We’ve started with re-implementation of just few of the basic operations, mainly because of performance benefit of using DLR caching system. The goal here is that if we can’t bind the operation during compilation, the operation can take performance benefit of DLR. This doesn’t apply to the operations that are already implemented more efficiently in Phalanger and DLR would not improve their execution speed. Such operations are, for example, arithmetic operations, array access, comparison operators etc.
