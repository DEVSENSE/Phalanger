> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [here](www.github.com/iolevel/peachpie)




> **Note:** This is detailed tutorial mainly for Windows. Now there is a package of WordPress with embedded Phalanger which is prepared both for .NET and Mono. You can download directly from http://wpdotnet.com

WordPress is an open-source content management system (CMS) built using PHP and MySQL. It’s of the most frequently used solutions for blog publishing. In this article I describe how to compile this PHP application to .NET Framework 4.0 using Phalanger.

# Contents

1.	Motivation
2.	Requirements
3.	Copy to publishing location
4.	Set-up IIS
5.	Configure ASP.NET using Web.config
6.	MySQL configuration
7.	Precompilation
8.	WordPress installation
9.	Settings permalinks (nice URLs)

# Motivation
Why would you want to run WordPress as a .NET application? There are several good reasons:
- If you are working for a customer who requires using the .NET platform, you can compile WordPress using Phalanger and it will run as a native .NET application.
- Applications compiled using Phalanger are very efficient. They outperform standard PHP installation. We will write about performance comparison in some later article.
- Thanks to Phalanger, it is easier to access .NET functionality from your PHP code. Therefore plugins using .NET functions are easily done. In some future article, we will look how to integrate WordPress with ASP.NET.
- Extending WordPress can be done even in a .NET language like C#. 
- Syntactic and semantic errors may occur in PHP in run time, but compilation process in Phalanger discoveres them right away. 
 
Now let’s look at the steps that are needed to compile and run WordPress using Phalanger.

# Requirements
Before you can continue, you need to download and install the following software:
- WordPress 3.3.2. Lower version should be fine, but there were some minor issues up to WordPress 3.0 code (e.g. http://core.trac.wordpress.org/ticket/14995). All of them are fixed in the later versions.
- Phalanger 3.0 for .NET 4.0
- Phalanger MySQL managed extension. This is optional but highly recommended, an alternative is PHP native extension bundled with Phalanger.
- MySQL 5.1/5.5.
- IIS 7.0/7.5. Lower versions work too, but some of the configuration steps described in this post will differ.

# Copy to publishing location
First copy WordPress into its directory in wwwroot of IIS (or any other virtual directory). In this tutorial I will be using c:\inetpub\wwwroot\wordpress\ as a directory for WordPress. Set write and modify permissions for IIS_IUSRS on this folder. This is necessary since WordPress creates wp-config.php file during the installation. Also WordPress needs the write permission to allow auto update feature, downloading themes, plugins,… If you won’t allow this you can create the configuration file manually during the installation. WordPress recognizes that it doesn’t have permissions to create the file and gives you content for this file. Then you can create the file manually.
To set the permissions, open Command Prompt with the Administrator permissions and run the following command:

`c:\inetpub\wwwroot\wordpress /grant BUILTIN\IIS_IUSRS:(OI)(CI)(M)`

# Set-up IIS
As any other ASP.NET application, WordPress compiled with Phalanger will have to run in some application pool. For our purposes we create application pool called Phalanger v3.0 and set it to .NET Framework v4.0. After creating the pool, you need to go to Advanced Settings and set “Enable 32-Bit Applications” to True (if you have 64bit operating system). This is necessary, because Phalanger uses native PHP extensions that are compiled as 32bit DLLs.
