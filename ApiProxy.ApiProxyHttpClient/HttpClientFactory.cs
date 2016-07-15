using System.Net.Http;
using ApiProxy.Contracts;

namespace ApiProxy.ApiProxyHttpClient
{
    public static class HttpClientFactory
    {
        public static HttpClient GetHttpClient(IApiProxyConfiguration configuration)
        {
            return new HttpClient(new ApiProxyClientHandler(configuration));
        }
    }
}
