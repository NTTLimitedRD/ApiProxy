using System;
using ApiProxy.Contracts;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProxy.ApiProxyHttpClient
{
    public class ApiProxyClientHandler : HttpClientHandler
    {
        private readonly IApiProxyConfiguration _configuration;
        public ApiProxyClientHandler(IApiProxyConfiguration configuration)
        {
            if (configuration == null)
            {
                throw  new ArgumentNullException(nameof(configuration));
            }
            _configuration = configuration;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var proxy = ApiProxyFactory.GetApiProxy(_configuration);
            return await proxy.ProcessRequestAsync(request);
        }
    }
}
