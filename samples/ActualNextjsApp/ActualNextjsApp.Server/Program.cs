using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NextjsStaticHosting.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Step 1: Add Next.js hosting support
builder.Services.Configure<NextjsStaticHostingOptions>(builder.Configuration.GetSection("NextjsStaticHosting"));
builder.Services.AddNextjsStaticHosting();

var app = builder.Build();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    // Not necessary for the sample, added to demonstrate that controllers still work as usual
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller}/{action=Index}/{id?}");

    // Step 2: Register dynamic endpoints to serve the correct HTML files at the right request paths.
    // Endpoints are created dynamically based on HTML files found under the specified RootPath during startup.
    // Endpoints are currently NOT refreshed if the files later change on disk.
    endpoints.MapNextjsStaticHtmls();
});

// Step 3: Serve other required files (e.g. js, css files in the exported next.js app).
app.UseNextjsStaticHosting();

app.Run();
