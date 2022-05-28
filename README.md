[![NuGet Gallery | NextjsStaticHosting.AspNetCore](https://img.shields.io/nuget/v/NextjsStaticHosting.AspNetCore?style=plastic)](https://www.nuget.org/packages/NextjsStaticHosting.AspNetCore)

Host a statically-generated [Next.js](https://nextjs.org/) client-side application on ASP .NET Core
with full support for SSG apps
(see [Next.js: Server-side Rendering vs. Static Generation](https://vercel.com/blog/nextjs-server-side-rendering-vs-static-generation)).

This is an ideal and scalable option for large scale production deployments (more on this later) of Next.js apps without requiring Node.js on the server.

## Benchmarks

Measured on Windows 11 version 21H2 (OS Build 22000.675) on an	AMD Ryzen 9 5900HX using latest LTS releases of .NET 6 (6.0.5) and Node.js (16.15.0) as of 5/27/2022.
Tests were run against localhost on raw HTTP using Apache Bench Version 2.3 Revision: 1879490 as the test client.

The ASP .NET Core project was published in Release configuration and executed without custom tweaks. The Next.js app was run with `npm run start` without custom tweaks (see: [docs](https://nextjs.org/docs/deployment#nodejs-server)). Each scenario was run 3 times, and the median run is shown.

Measured throughput was ~6-7x greater on ASP .NET Core; P99 latency was ~2-4x better on ASP .NET Core.

### Scenario 1: 10 concurrent, 50k requests

Command line: `ab -c 10 -n 50000 -k http://localhost:PORT/post/1`

Hosting stack | Avg (ms) | P99 (ms)
--------------|----------|---------
ASP .NET Core | 0.469    | 1
Node.js       | 3.355    | 5


### Scenario 2: 100 concurrent, 100k requests

Command line: `ab -c 100 -n 100000 -k http://localhost:PORT/post/1`

Hosting stack | Avg (ms)   | P99 (ms)
--------------|------------|---------
ASP .NET Core | 4.957      | 22
Node.js       | 29.652     | 41

### Is this a fair comparison?

Not really, for a few reasons. But it is still informative:
* The capabilities of the two stacks are not equivalent, and the ASP .NET Core hosting stack enabled by this project does not support SSR content. To a large degree, we are comparing apples and oranges. On the other hand, entire apps can be built without needing SSR. In those cases, the additional capabilities of running in Node.js are unused
* Only measuring the time to load the HTML for a page. Loading other static content (js, css, etc.) may account for longer delays in observed page load performance in practice, and those aren't taken into account in these tests
* Not necessarily following best practices for production hosting, though the setup followed the official guidance for both Next.js and ASP .NET Core without additional tweaks in either
* Run on Windows, whereas each stack could exhibit different perf characteristics on a different OS

# Usage

## Option 1: Running the sample with your own Next.js app

1. Export your Next.js app using `npx next export`
2. Copy the generated outputs from `out` to `.\samples\PreBuiltClientDemo\ClientApp`
3. Run `samples\PreBuiltClientDemo` and see it working at `https://locahost:5001`.


## Option 2: Adding `NextjsStaticHosting` to an existing ASP .NET Core project (minimal API's)

Modify your `Program.cs` as follows:

```diff
+using NextjsStaticHosting.AspNetCore;

 var builder = WebApplication.CreateBuilder(args);

+builder.Services.Configure<NextjsStaticHostingOptions>(builder.Configuration.GetSection("NextjsStaticHosting"));
+builder.Services.AddNextjsStaticHosting();

 var app = builder.Build();
 app.UseRouting();
 app.UseEndpoints(endpoints =>
 {
+    endpoints.MapNextjsStaticHtmls();
 });

+app.UseNextjsStaticHosting();

 app.Run();
```


## Option 3: Adding `NextjsStaticHosting` to an existing ASP .NET Core project (traditional startup style API's)

Add the following to your `Startup.cs`:

```diff
 public void ConfigureServices(IServiceCollection services)
 {
+    services.Configure<NextjsStaticHostingOptions>(builder.Configuration.GetSection("NextjsStaticHosting"));
+    services.AddNextjsStaticHosting();
 }

 public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
 {
     app.UseRouting();
     app.UseEndpoints(endpoints =>
     {
+        endpoints.MapNextjsStaticHtmls();
     });

+    app.UseNextjsStaticHosting();
}
```

# Why use ASP .NET Core

Next.js applications are usually hosted on a Node.js server which, among other things, takes care of routing concerns and serves the appropriate files for each incoming request. While this is a fine choice in many scenarios, there are use cases where it may be desirable to use a different stack such as ASP .NET Core (e.g. for scalability concerns or because an application's backend may already use ASP .NET Core, and setting up a separate infra to host the client app may be challenging and / or costly).

One may think that pure static hosting is possible thanks to Next.js' support for [Static HTML Export](https://nextjs.org/docs/advanced-features/static-html-export), and hence no additional server side support would be necessary. And one would be *almost* correct. While that statement bears some truth, and Static HTML Export is indeed a powerful feature of Next.js that this library relies on, it alone does not solve all problems.

Critically, Next.js static export does **not** produce SPA's (Single Page Applications). Next.js goes through great lengths to ensure an optimal user experience on first-page-loads as well as for client-driven navigations without reloading the page, and its design **requires** that precisely the right page be served to the client during initial load of a page. This is in fact one of its main advantages compared to other stacks.

For example, imagine a Next.js application consisting of the following pages:

* `/pages/index.js`
* `/pages/post/[pid].js`

When statically exported to HTML using `npm next export`, the exported output will contain the following entry-point HTML files:

* `/out/index.html`
* `/out/post/[pid].html`

When a browser issues a request for `/`, the server is expected to return the contents of `/out/index.html`. Similarly, a request for `/post/123` is supposed to return the contents of `/out/post/[pid].html`. As long as the appropriate initial HTML is served for the incoming request paths, Next.js takes care of the rest, and will rehydrate the page on the client-side providing full React and interaction capabilities.

**However**, if the wrong page is served (e.g., if `/out/index.html` were served on a request for `/post/123`), rehydration won't work (by design!). The client will render the contents of `/pages/index.js` page even though the URL bar will say `/post/123`, and it will NOT rehydrate the contents that were expected for `/post/123` -- code running in the browser at that time in fact would not even know that it was supposed to be showing the contents of a different page.

The purpose of this library is to add the necessary routing machinery so that ASP .NET Core serves the correct Next.js statically-generated HTML files according to user requests. **It leverages ASP .NET Core Endpoint Routing for unparalleled performance, and avoids any superfluous computations on the hot path**.

By design, **this library does NOT support dynamic SSR (Server Side Rendering)**. Applications requiring dynamic Server Side Rendering likely would prefer or need to use Node.js on the server anyway.


## Scalability and Security considerations

Because no custom code is executed to serve static files (other than ASP .NET Core's built-in Endpoint Routing and Static Files), this is just about the most efficient way to host a statically generated Next.js application.

Additionally, because we deliberately do not support dynamic SSR, you do not need to worry about running Node.js or JavaScript in your production servers.


## Limitations

By design, this library does not support Server Side Rendering, yet it offers full support for SSG (Static Site Generation). This offers many of the advantages of SSR, including fast initial load and SEO-friendliness.

A simple way to reason about this is as follows:

> **If your Next.js application is eligible for [Static HTML Export](https://nextjs.org/docs/advanced-features/static-html-export), then it can be hosted on ASP .NET Core with this library.**

