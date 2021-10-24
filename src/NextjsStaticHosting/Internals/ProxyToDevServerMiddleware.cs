using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace NextjsStaticHosting.Internals
{
    internal class ProxyToDevServerMiddleware
    {
        // See: https://github.com/microsoft/reverse-proxy/blob/342361d8e314a3d3842b851a7fd0cbb4873c8b72/src/ReverseProxy/Utilities/RequestUtilities.cs#L28-L35
        private static readonly HashSet<string> invalidH2H3ResponseHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Connection,
            HeaderNames.TransferEncoding,
            HeaderNames.KeepAlive,
            HeaderNames.Upgrade,
            "Proxy-Connection"
        };

        private readonly NextjsStaticHostingOptions options;
        private readonly RequestDelegate next;
        private readonly HttpMessageInvoker httpClient;

        public ProxyToDevServerMiddleware(IOptions<NextjsStaticHostingOptions> options, RequestDelegate next)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.next = next ?? throw new ArgumentNullException(nameof(next));

            if (this.options.DevServer == null || !this.options.DevServer.IsAbsoluteUri)
            {
                throw new InvalidOperationException($"{nameof(options)}.{nameof(NextjsStaticHostingOptions.DevServer)} must be an absolute uri.");
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
            var incomingRequest = context.Request;
            using var destinationRequest = new HttpRequestMessage(
                new HttpMethod(incomingRequest.Method),
                new Uri(this.options.DevServer, incomingRequest.PathBase + incomingRequest.Path + incomingRequest.QueryString));
            destinationRequest.Content = new StreamContent(incomingRequest.Body);
            foreach (var header in incomingRequest.Headers)
            {
                var headerName = header.Key;
                var headerValue = header.Value;
                if (StringValues.IsNullOrEmpty(headerValue))
                {
                    continue;
                }

                // Filter out HTTP/2 pseudo headers like ":method" and ":path", those go into other fields.
                if (headerName.Length > 0 && headerName[0] == ':')
                {
                    continue;
                }

                var headerValues = (IEnumerable<string>)headerValue;
                if (!destinationRequest.Headers.TryAddWithoutValidation(headerName, headerValues))
                {
                    destinationRequest.Content.Headers.TryAddWithoutValidation(headerName, headerValues);
                }
            }

            try
            {
                using var destinationResponse = await this.httpClient.SendAsync(destinationRequest, context.RequestAborted);
                context.Response.StatusCode = (int)destinationResponse.StatusCode;
                foreach (var header in destinationResponse.Headers)
                {
                    if (invalidH2H3ResponseHeaders.Contains(header.Key))
                    {
                        continue;
                    }
                    context.Response.Headers.TryAdd(header.Key, new StringValues(header.Value.ToArray()));
                }
                if (destinationResponse.Content != null)
                {
                    foreach (var header in destinationResponse.Content.Headers)
                    {
                        context.Response.Headers.TryAdd(header.Key, new StringValues(header.Value.ToArray()));
                    }
                }
                var stream = await destinationResponse.Content.ReadAsStreamAsync();
                await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
