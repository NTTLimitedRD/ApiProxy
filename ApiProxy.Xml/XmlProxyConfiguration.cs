using Newtonsoft.Json;

namespace DD.ApiProxy.Xml
{
    [JsonObject]
    public class XmlProxyConfiguration
    {
        public Transform Transform { get; set; }        
    }
}
