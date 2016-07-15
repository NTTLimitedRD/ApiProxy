using Newtonsoft.Json;

namespace ApiProxy.Xml
{
    [JsonObject]
    public class TransformConfigution
    {
        public string XsltFileName { get; set; }

        // Comma seperated query parameters to be trimmed
        public string TrimQueryParameters { get; set; }
    }
}