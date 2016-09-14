using ApiProxy.ApiProxyHttpClient;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public static class ApiProxyFactory
    {
        public static IApiProxy GetApiProxy(IApiProxyConfiguration configuration, IApiProxyRecordProvider apiProxyRecordProvider)
        {            
            var folderBasedRecorder = new FileBasedApiProxyRecordProvider(configuration);
            var apiProxyProviderFactory = new ApiProxyProviderFactory(folderBasedRecorder, configuration);
            return new DD.ApiProxy.ApiProxy(configuration, apiProxyProviderFactory, apiProxyRecordProvider);
        }
    }
}
