using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public static class ApiProxyRecordProviderFactory
    {
        public static IApiProxyRecordProvider GetApiProxyRecordProvider(IApiProxyConfiguration configuration)
        {            
            return new InMemoryApiProxyRecordProvider(configuration);           
        }
    }
}
