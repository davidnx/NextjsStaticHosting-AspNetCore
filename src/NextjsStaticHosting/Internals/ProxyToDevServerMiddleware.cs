using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;

namespace NextjsStaticHosting.Internals
{
    internal class ProxyToDevServerMiddleware
    {
        private readonly NextjsStaticHostingOptions options;
        private readonly IHttpForwarder yarpForwarder;
        private readonly RequestDelegate next;
        private readonly HttpMessageInvoker httpClient;

        private readonly string baseAddress;

        public ProxyToDevServerMiddleware(IOptions<NextjsStaticHostingOptions> options, IHttpForwarder yarpForwarder, RequestDelegate next)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.yarpForwarder = yarpForwarder ?? throw new ArgumentNullException(nameof(yarpForwarder));
            this.next = next ?? throw new ArgumentNullException(nameof(next));

            if (!this.options.ProxyToDevServer)
            {
                throw new InvalidOperationException($"{nameof(ProxyToDevServerMiddleware)} should only be added when {nameof(options)}.{nameof(NextjsStaticHostingOptions.ProxyToDevServer)} is set. This is a coding defect.");
            }

            if (this.options.DevServer == null || !this.options.DevServer.IsAbsoluteUri)
            {
                throw new InvalidOperationException($"{nameof(options)}.{nameof(NextjsStaticHostingOptions.DevServer)} must be an absolute uri.");
            }
            this.baseAddress = this.options.DevServer.ToString();

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
            await this.yarpForwarder.SendAsync(context, this.baseAddress, this.httpClient, new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromMinutes(5) });
        }
    }
}
