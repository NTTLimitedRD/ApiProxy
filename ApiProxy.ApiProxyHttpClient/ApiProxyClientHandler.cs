using System;
using DD.ApiProxy.Contracts;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public class ApiProxyClientHandler : HttpClientHandler
    {
        private readonly IApiProxyConfiguration _configuration;

        public event EventHandler<RequestReceivedEventArgs> RequestReceived;

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
            await OnRequestReceivedAsync(request);
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

        private async Task OnRequestReceivedAsync(HttpRequestMessage request)
        {
            EventHandler<RequestReceivedEventArgs> handler = RequestReceived;
            handler?.Invoke(this, new RequestReceivedEventArgs()
            {
                RequestUri = request.RequestUri,
                HttpMethod = request.Method,
                RequestContent = await request.Content.ReadAsStringAsync()
            });
        }
    }
}
