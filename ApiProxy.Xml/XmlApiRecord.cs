using DD.ApiProxy.Contracts;
using Newtonsoft.Json;

namespace DD.ApiProxy.Xml
{
    [JsonObject]
    public class XmlApiRecord : ApiRecord
    {
        public XmlProxyConfiguration Configuration { get; set; }
    }
}
