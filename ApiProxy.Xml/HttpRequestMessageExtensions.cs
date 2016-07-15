using System;
using System.Net.Http;

namespace ApiProxy.Xml
{
    public static class HttpRequestMessageExtensions
    {
        public static bool IsContentMediaTypeXml(this HttpRequestMessage request)
        {
            return IsContentMediaType(request, "application/xml");
        }

        public static bool IsContentMediaType(this HttpRequestMessage request, string typestring)
        {
            return string.Equals(request?.Content?.Headers?.ContentType.MediaType, typestring, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}