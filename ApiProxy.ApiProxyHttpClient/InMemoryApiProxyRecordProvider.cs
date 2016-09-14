using System;
using System.Collections.Generic;
using System.Net.Http;
using DD.ApiProxy.Contracts;
using Newtonsoft.Json;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public class InMemoryApiProxyRecordProvider : FileBasedApiProxyRecordProvider
    {
        private readonly IDictionary<string, ApiRecord> _mockApiResponses = new Dictionary<string, ApiRecord>();
        public InMemoryApiProxyRecordProvider(IApiProxyConfiguration proxyConfiguration) : base(proxyConfiguration)
        {           
        }

        public override ApiRecord GetApiRecord(HttpRequestMessage request)
        {
            ApiRecord record;
            if (_mockApiResponses.TryGetValue(GetApiRequestPathAndQuery(request.RequestUri), out record))
                return record;
            return base.GetApiRecord(request);
        }

        public bool AddApiRecord(ApiRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.RawContent))
            {
                var raw = JsonConvert.SerializeObject(record);
                record.RawContent = raw;
            }

            var requestUri = new Uri(record.Uri);
            if (_mockApiResponses.ContainsKey(GetApiRequestPathAndQuery(requestUri)))
                return false;
            _mockApiResponses.Add(GetApiRequestPathAndQuery(requestUri), record);
            return true;
        }

        private string GetApiRequestPathAndQuery(Uri requestUri)
        {
            return requestUri.PathAndQuery.ToLowerInvariant().TrimEnd(new[] {'?', '&'}).TrimStart('/');
        }
    }
}
