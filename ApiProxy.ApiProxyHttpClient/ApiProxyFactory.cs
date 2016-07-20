using ApiProxy.ApiProxyHttpClient;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public static class ApiProxyFactory
    {
        public static IApiProxy GetApiProxy(IApiProxyConfiguration configuration)
        {            
            var folderBasedRecordProvider = new FolderHeirarchyBasedApiProxyRecordProvider(configuration);
            var apiProxyProviderFactory = new ApiProxyProviderFactory(folderBasedRecordProvider, configuration);
            return new DD.ApiProxy.ApiProxy(configuration, apiProxyProviderFactory, folderBasedRecordProvider);
        }
    }
}
