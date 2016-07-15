using ApiProxy.Contracts;
using Newtonsoft.Json;

namespace ApiProxy.Xml
{
    [JsonObject]
    public class XmlApiRecord : ApiRecord
    {
        public XmlProxyConfiguration Configuration { get; set; }
    }
}
