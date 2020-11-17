# Hosting Next.js client-side apps on ASP .NET Core


This library helps you host a statically-generated [Next.js](https://nextjs.org/) client-side application on ASP .NET Core with full support for SSG apps (see [Next.js: Server-side Rendering vs. Static Generation](https://vercel.com/blog/nextjs-server-side-rendering-vs-static-generation)).

This is an ideal and scalable option for large scale production deployments (more on this later) of Next.js apps without requiring Node.js on the server.


# Why is this necessary?

Next.js applications are usually hosted on a Node.js server which, among other things, takes care of routing concerns and serves the appropriate files for each incoming request. While this is a fine choice in many scenarios, there are use cases where it may be desirable to use a different stack such as ASP .NET Core (e.g. for scalability concerns or because an application's backend may already use ASP .NET Core, and setting up a separate infra to host the client app may be challenging and / or costly).

One may think that pure static hosting is possible thanks to Next.js' support for [Static HTML Export](https://nextjs.org/docs/advanced-features/static-html-export), and hence no additional server side support would be necessary. And one would be *almost* correct. While that statement bears some truth, and Static HTML Export is indeed an amazing feature of Next.js that this library relies on, it alone does not solve all problems.

Particularly, it is critical to note that Next.js does **not** produce SPA's (Single Page Applications). While the framework goes through extreme lengths to ensure an optimal user experience on first-page-loads as well as for client-driven navigations without reloading the page, its design **requires** that the correct static content be served to the client during initial load. This is in fact one of its main advantages compared to other stacks.

For example, imagine a Next.js application consisting of the following pages:

* `/pages/index.js`
* `/pages/post/[pid].js`

When statically exported to HTML using `npm next export`, the output will contain the following entry-point HTML files:

* `/out/index.html`
* `/out/post/[pid].html`

When a browser issues a request for `/`, the server is expected to return the contents of `/out/index.html`. Similarly, a request for `/post/123` is supposed to return the contents of `/out/post/[pid].html`. As long as the appropriate initial HTML is served for the incoming request paths, Next.js takes care of the rest, and will rehydrate the page on the client-side providing full React and interaction capabilities.

**However**, if the wrong page is served (e.g., if `/out/index.html` were served on a request for `/post/123`), Next.js is unable to recover (by design!). The client will render the contents of `/pages/index.js` page even though the URL bar will say `/post/123`, and it will NOT rehydrate the contents that were expected for `/post/123` -- code running in the browser at that time in fact would not even know that it was supposed to be showing the contents of a different page.

The purpose of this library is to add the necessary routing configuration so that ASP .NET Core will serve the correct Next.js statically-generated HTML files according to user requests. **It leverages ASP .NET Core Endpoint Routing for unparalleled performance, and avoids any superfluous computations on the hot path**.

By design, **this library does NOT support dynamic SSR (Server Side Rendering)**. Applications requiring dynamic Server Side Rendering likely would prefer or need to use Node.js on the server anyway.


# Scalability and Security considerations

Because no custom code is exeuted to serve static files (other than ASP .NET Core's own highly optimized Endpoint Routing and Static Files capabilities), this is just about the most efficient way to host a staticcally generated Next.js application.

Additionally, because we deliberately do not support dynamic SSR, you do not need to worry about running Node.js or JavaScript in your production servers.


# Limitations

As mentioned, this library does not support Server Side Rendering, although it offers full support for SSG (Statically Generated content). This offers all of the advantages of SSR, expect for running dynamic JavaScript on the server while serving a request.

A simple way to reason about this is as follows:

* **If your Next.js application is eligible for [Static HTML Export](https://nextjs.org/docs/advanced-features/static-html-export), then it can be hosted on ASP .NET Core with this library. If not, then likely it will not work -- just run it on a Node.js server following Next.js existing docs.**


# Usage

See the sections below to get started quickly.

## Running the sample with your own Next.js app

1. Export your Next.js app using `npx next export`

2. Copy the generated outputs from `out` to the sample app's `wwwroot/ClientApp` folder

3. Run the sample and try things out at `https://locahost:5001`.


## Adding `NextjsStaticHosting` to an existing ASP .NET Core project

Add the following to your `Startup.cs`:

```diff
 public void ConfigureServices(IServiceCollection services)
 {
     //
     // Other DI registrations...
     //

+    services.AddNextjsStaticHosting(options => options.RootPath = "wwwroot/ClientApp");
 }

 public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
 {
     //
     // Other middlewares...
     //

     app.UseRouting();
     app.UseEndpoints(endpoints =>
     {
         //
         // Other mappings...
         //

+        endpoints.MapNextjsStaticHtmls();
     });

+    app.UseNextjsStaticHosting();
}
```
