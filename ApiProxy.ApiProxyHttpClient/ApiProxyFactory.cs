using ApiProxy.Contracts;

namespace ApiProxy.ApiProxyHttpClient
{
    public static class ApiProxyFactory
    {
        public static IApiProxy GetApiProxy(IApiProxyConfiguration configuration)
        {            
            var folderBasedRecordProvider = new FolderHeirarchyBasedApiProxyRecordProvider(configuration);
            var apiProxyProviderFactory = new ApiProxyProviderFactory(folderBasedRecordProvider, configuration);
            return new ApiProxy(configuration, apiProxyProviderFactory, folderBasedRecordProvider);
        }
    }
}
