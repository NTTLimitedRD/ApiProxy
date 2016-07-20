using System.Net.Http;

namespace DD.ApiProxy.Contracts
{
    public interface IApiProxyProviderFactory
    {
        IApiProxyProvider GetApiProxyProvider(ApiRecord record, HttpRequestMessage request,
            IApiProxyConfiguration configuration);
    }
}
