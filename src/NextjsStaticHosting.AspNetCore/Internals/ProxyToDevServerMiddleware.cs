using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;

namespace NextjsStaticHosting.AspNetCore.Internals
{
    internal class ProxyToDevServerMiddleware
    {
        private readonly NextjsStaticHostingOptions options;
        private readonly IHttpForwarder yarpForwarder;
        private readonly RequestDelegate next;
        private readonly ILogger<ProxyToDevServerMiddleware> logger;
        private readonly HttpMessageInvoker httpClient;

        public ProxyToDevServerMiddleware(IOptions<NextjsStaticHostingOptions> options, IHttpForwarder yarpForwarder, RequestDelegate next, ILogger<ProxyToDevServerMiddleware> logger)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.yarpForwarder = yarpForwarder ?? throw new ArgumentNullException(nameof(yarpForwarder));
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!this.options.ProxyToDevServer ||
                string.IsNullOrEmpty(this.options.DevServer))
            {
                throw new InvalidOperationException($"{nameof(ProxyToDevServerMiddleware)} should only be added when {nameof(options)}.{nameof(NextjsStaticHostingOptions.ProxyToDevServer)} is set and a valid {nameof(options)}.{nameof(NextjsStaticHostingOptions.DevServer)} is provided. This is a coding defect.");
            }

            this.httpClient = new HttpMessageInvoker(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                UseProxy = false
            });
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var error = await this.yarpForwarder.SendAsync(context, this.options.DevServer, this.httpClient, new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromMinutes(5) });
            if (error != ForwarderError.None)
            {
                string message =
                    $"[NextjsStaticHosting.AspNetCore] Unable to reach Next.js dev server. Please ensure it is running at {this.options.DevServer}.{Environment.NewLine}" +
                    $"If you are running in production and did not intend to proxy to a Next.js dev server, please ensure {nameof(NextjsStaticHostingOptions)}.{nameof(NextjsStaticHostingOptions.ProxyToDevServer)} is false.{Environment.NewLine}" +
                    $"YARP error: {error}";
                this.logger.LogError(message);
                if (!context.Response.HasStarted)
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(message);
                }
            }
        }
    }
}
