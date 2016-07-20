using Newtonsoft.Json;

namespace DD.ApiProxy.Xml
{
    [JsonObject]
    public class Transform
    {
        public TransformConfigution Body { get; set; }

        public TransformConfigution Query { get; set; }

        public TransformConfigution RequestContent { get; set; }
    }
}