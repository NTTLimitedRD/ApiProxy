using System;
using System.Net.Http;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public class RequestReceivedEventArgs : EventArgs
    {
        public Uri RequestUri { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public string RequestContent { get; set; }
    }
}
