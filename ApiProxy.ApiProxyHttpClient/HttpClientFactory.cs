using System;
using System.Net;
using System.Net.Http;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public static class HttpClientFactory
    {
        public static HttpClient GetHttpClient(IApiProxyConfiguration configuration, ICredentials credentials = null, EventHandler<RequestReceivedEventArgs> requestReceivedEventHandler = null)
        {
            var baseUri = configuration.DefaultApiAddress ?? new Uri("https://localhost/");
            var clientHandler = new ApiProxyClientHandler(configuration)
            {
                Credentials = credentials,
                PreAuthenticate = true
            };

            if (requestReceivedEventHandler != null)
                clientHandler.RequestReceived += requestReceivedEventHandler;

            return new HttpClient(clientHandler)
            {
                BaseAddress = baseUri
            };
        }

        public static HttpClient GetHttpClient(IApiProxyConfiguration configuration, HttpClientHandler httpClientHandler)
        {
            var baseUri = configuration.DefaultApiAddress ?? new Uri("https://localhost/");           
            return new HttpClient(httpClientHandler)
            {
                BaseAddress = baseUri
            };
        }
    }
}
