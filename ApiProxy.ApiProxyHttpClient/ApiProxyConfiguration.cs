using System;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    /// <summary>
    /// Api Proxy Configuration
    /// </summary>
    public class ApiProxyConfiguration : IApiProxyConfiguration
    {       
        public string ApiMocksPath { get; set; }

        public string ApiRecordingPath { get; set; }

        public Uri DefaultApiAddress { get; set; }

        public bool FallbackToDefaultApi { get; set; }

        public bool RecordApiRequestResponse { get; set; }
    }
}
