using Newtonsoft.Json;

namespace ApiProxy.Xml
{
    [JsonObject]
    public class Transform
    {
        public TransformConfigution Body { get; set; }

        public TransformConfigution Query { get; set; }

        public TransformConfigution RequestContent { get; set; }
    }
}