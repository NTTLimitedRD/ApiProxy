using System;
using System.Net.Http;
using ApiProxy.Contracts;
using ApiProxy.Xml;
using Newtonsoft.Json;

namespace ApiProxy.ApiProxyHttpClient
{
    public class ApiProxyProviderFactory : IApiProxyProviderFactory
    {
        private readonly IApiRecorder _recorder;
        private readonly IApiProxyConfiguration _apiProxyConfiguration;
        public ApiProxyProviderFactory(IApiRecorder recorder, IApiProxyConfiguration apiProxyConfiguration)
        {
            if(recorder == null)
                throw new ArgumentNullException(nameof(recorder));
            _recorder = recorder;

            if (apiProxyConfiguration == null)
                throw new ArgumentNullException(nameof(apiProxyConfiguration));
            _apiProxyConfiguration = apiProxyConfiguration;
        }

        public IApiProxyProvider GetApiProxyProvider(ApiRecord record, HttpRequestMessage request, IApiProxyConfiguration configuration)
        {            
            switch (record.ResponseContentType)
            {
                case "application/xml":                    
                    var xmlApiRecord = JsonConvert.DeserializeObject<XmlApiRecord>(record.RawContent);
                    return new XmlContentTypeApiProxyProvider(configuration, _recorder, xmlApiRecord);
            }
            if(!_apiProxyConfiguration.FallbackToDefaultApi)
                throw new InvalidOperationException("Content type for mocking not supported");

            return new RealApiApiProxyProvider(configuration, _recorder);
        }
    }
}
