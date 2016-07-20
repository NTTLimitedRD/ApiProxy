using System;
using ApiProxy.Contracts;
using System.Net.Http;
using System.Net.Http.Headers;
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
            // This is only to support basic auth
            if (this.Credentials != null && request.Headers.Authorization == null)
            {
                var cred = this.Credentials.GetCredential(new Uri("http://localhost/"), string.Empty);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        System.Text.Encoding.ASCII.GetBytes($"{cred.UserName}:{cred.Password}")));
            }

            var proxy = ApiProxyFactory.GetApiProxy(_configuration);
            return await proxy.ProcessRequestAsync(request);
        }
    }
}
