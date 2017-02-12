> **Note:** There is a new, modern PHP compiler to .NET entitled Peachpie, which is being developed at the moment. Please see [here](www.github.com/iolevel/peachpie)

Global.asax file gives web developers great posibilities of handling the life-cycle of their web app. It is a common practice to take advantage of this file in ASP.NET applications. Now you can make use of it in a PHP web application too.

Since Phalanger runs PHP web as a native ASP.NET application, you can extend its functionality using ASP.NET features. In this post I would like to show some advantages and possibilities of using Global.asax file in PHP web applications.

# Adding Global.asax

Global.asax file is an optional file that contains code for handling application-level and session-level events raised by ASP.NET. To add that file into you Phalanger powered web, simply create Global.asax file in the web root, aside of your Web.config. You can use Visual Studio to create file for you (New > File > Global application class) or just copy following content. The process is the same as in any other ASP.NET application.

```csharp
<%@ Application Language="C#" %>
<script runat="server">
    void Application_Start(object sender, EventArgs e)
    {
        // Code that runs on application startup
    }

    void Application_End(object sender, EventArgs e)
    {
        //  Code that runs on application shutdown
    }

    void Application_Error(object sender, EventArgs e)
    {
        // Code that runs when an unhandled error occurs
    }

    void Session_Start(object sender, EventArgs e)
    {
        // Code that runs when a new session is started
    }

    void Session_End(object sender, EventArgs e)
    {
        // Code that runs when a session ends.
    }
</script>
```
From the code above you can see some possibilities. There are many other events you can handle, e.g. Application_BeginRequest or Application_AuthenticateRequest.

# ASP.NET output cache

The advantage of using Global.asax in a PHP web is that it gives you a way to easily extend the functionality of an application without the need to modify existing PHP code. This way, you can separate the core functionality of the application from an additional cross-cutting concerns, such as caching.

As a sample I will show how to enable output caching as easily as it is on ASP.NET pages. The whole trick consists of making use of Application_BeginRequest method and simulating <%@OutputCache%> tag, well known from .aspx code.

The OutputCache tag has many attributes; and when compiled by ASP.NET, it is translated into code calling several methods on Response.Cache object in specific order. This order have to be preserved to simulate the behaviour properly. Also it is valid for single page, whilst Application_BeginRequest is called on every page.

```csharp
void Application_BeginRequest(object sender, EventArgs e) {
if (request.CurrentExecutionFilePath=="/index.php")
{
    var cache = Response.Cache;

    cache.SetCacheability(HttpCacheability.Server);
    cache.SetExpires(this.Context.Timestamp.AddDays((double)365));
    cache.SetMaxAge(new TimeSpan(365, 0, 0, 0));
    cache.SetValidUntilExpires(true);
    cache.SetLastModified(System.IO.File.GetLastWriteTime(request.PhysicalPath));
    cache.VaryByParams["*"] = true;
}
```

The code above simulates OutputCache tag, that would be placed on index.php page. By this, you programmically turn on caching of index.php. Using Global.asax has several advantages, as you can customize the caching behaviour. For example, you can extend the above example to turn off caching for pages if there is a cookie with specific values etc.

# Cache dependency

Another feature of the ASP.NET caching mechanism is that you can specify dependencies. This means that a page is regenerated automatically if the dependency changes. With this we can invalidate page when something has been changed.

```csharp
var cacheKey = Request.RawUrl;
if (HttpContext.Current.Cache[cacheKey] == null)
    HttpContext.Current.Cache[cacheKey] = new object();
Response.AddCacheItemDependency(cacheKey);
```

Here we add a dependency to currently requested page. Later in the PHP code when the page has to be regenerated, we can invalidate this cache by removing cacheKey from the Cache object.

```csharp
// note: on Phalanger 2, use ::: syntax instead of \
$contextCurrent = \System\Web\HttpContext::$Current;
$key =  $RawUrlOfPageToBeInvalidated;
$contextCurrent->Cache->Remove($key);
```

# Conclusion

In this short example we have shown that you can simply turn on caching of existing PHP web responses. Also we demonstrated an approach of extending functionality of PHP application by handling ASP.NET life-cycle. In the similar way you can handle new sessions, users authentication, authorization, errors, application start etc.
