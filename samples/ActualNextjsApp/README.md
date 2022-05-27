# ActualNextjsApp sample

This sample illustrates a few things that help run a Next.js application reliably and at scale with an ASP .NET Core server.

## 1. Folder structure

Notice the folders `ActualNextjsApp.Client` and `ActualNextjsApp.Server`.

Your Next.js application should be placed inside of `./ActualNextjsApp.Client`, such that when you run `npx next export` on it,
Next.js will produce the outputs at `./ActualNextjsApp.Client/out/`.
This is important because the corresponding server app at `ActualNextjsApp.Server`
is configured to look for the Next.js app outputs at precisely that location.
See property `NextJsCompiledOutputPath` in [ActualNextjsApp.Server.csproj](./ActualNextjsApp.Server/ActualNextjsApp.Server.csproj)
and `NextjsStaticHosting:RootPath` in [appsettings.json](./ActualNextjsApp.Server/appsettings.json)

## Dev experience

When developing locally, you need to do two things:

1. Run your Next.js app in development, which gives you full Hot-Module-Reload support with NextJsStaticHosting.AspNetCore.
   Example: `cd ./ActualNextjsApp.Client && npm run dev`

2. Run the server ASP .NET Core app in development mode (i.e. `ASPNETCORE_ENVIRONMENT=Development`).
   This is done automatically for you when running from Visual Studio.
   Notice that when the server app runs in development mode, the `appsettings.Development.json` file
   instructs `NextJsStaticHosting.AspNetCore` to proxy appropriate requests to your local Next.js dev server
   via configuration keys `NextjsStaticHosting:DevServer` and `NextjsStaticHosting:ProxyToDevServer`

## Production experience

When deploying to production, `ActualNextjsApp.Server` is configured to copy the compiled Next.js app outputs to the published outputs.
When running in production, configuration option `NextjsStaticHosting:ProxyToDevServer` is `false` (default), therefore no proxying
will be performed, and the right files will be served directly from disk.

This means you WILL NOT run Node.js in production. All Next.js compiled outputs will be served as static files
but with smart routing capabilities to enable full fidelity to an actual Next.js server running in SSG-only mode.

**Remember to build and export the client app before publishing `ActualNextjsApp.Server`!** This can be automated with MSBuild in the csproj if desired, but isn't shown in this demo.
