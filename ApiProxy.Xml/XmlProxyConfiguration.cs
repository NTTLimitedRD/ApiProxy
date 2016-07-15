using Newtonsoft.Json;

namespace ApiProxy.Xml
{
    [JsonObject]
    public class XmlProxyConfiguration
    {
        public Transform Transform { get; set; }        
    }
}
