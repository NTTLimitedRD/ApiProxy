using Newtonsoft.Json;

namespace ApiProxy.Contracts
{
    [JsonObject]
    public class ApiRecord
    {
        public string Method { get; set; }
        public string Uri { get; set; }
        public string StatusCode { get; set; }
        public string RequestContent { get; set; }
        public string ResponseContent { get; set; }
        public string ResponseContentType { get; set; }       
        public bool Mock { get; set; }

        [JsonIgnore]
        public string MockFilePath { get; set; }
        [JsonIgnore]
        public string RawContent { get; set; }
    }
}
