using System;
using System.Net;
using System.Net.Http;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public static class HttpClientHandlerFactory
    {
        public static HttpClientHandler GetHttpClientHandler(IApiProxyConfiguration configuration,
            ICredentials credentials = null, EventHandler<RequestReceivedEventArgs> requestReceivedEventHandler = null)
        {            
            var clientHandler = new ApiProxyClientHandler(configuration)
            {
                Credentials = credentials,
                PreAuthenticate = true
            };

            if (requestReceivedEventHandler != null)
                clientHandler.RequestReceived += requestReceivedEventHandler;

            return clientHandler;
        }
    }
}
