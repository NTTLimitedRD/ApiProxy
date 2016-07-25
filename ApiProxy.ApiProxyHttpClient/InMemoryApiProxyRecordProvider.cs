using System;
using System.Collections.Generic;
using System.Net.Http;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public class InMemoryApiProxyRecordProvider : FolderHeirarchyBasedApiProxyRecordProvider
    {
        private readonly IDictionary<string, ApiRecord> _mockApiResponses = new Dictionary<string, ApiRecord>();
        public InMemoryApiProxyRecordProvider(IApiProxyConfiguration proxyConfiguration) : base(proxyConfiguration)
        {           
        }

        public override ApiRecord GetApiRecord(HttpRequestMessage request)
        {
            ApiRecord record;
            if (_mockApiResponses.TryGetValue(request.RequestUri.PathAndQuery, out record))
                return record;
            return base.GetApiRecord(request);
        }

        public bool AddApiRecord(ApiRecord record)
        {
            var requestUri = new Uri(record.Uri);
            if (_mockApiResponses.ContainsKey(requestUri.PathAndQuery))
                return false;
            _mockApiResponses.Add(requestUri.PathAndQuery, record);
            return true;
        }
    }
}
