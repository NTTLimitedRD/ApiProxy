using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.Xml
{
    public static class XmlApiRecordExtensions
    {
        public static XmlAction GetXmlAction(this XmlApiRecord apiRecord, IApiProxyConfiguration proxyConfiguration)
        {
            if (!proxyConfiguration.FallbackToDefaultApi)
            {
                if (apiRecord.ResponseContent != null || apiRecord.Mock)
                    return XmlAction.ReplayFromMock;
                return XmlAction.None;
            }

            var xmlConfiguration = apiRecord.Configuration;

            if ((xmlConfiguration != null && apiRecord.Mock && !string.IsNullOrWhiteSpace(xmlConfiguration.Transform?.RequestContent?.XsltFileName)))
                return XmlAction.ReplayFromTransformedRequestMock;

            if (xmlConfiguration?.Transform != null)
                return XmlAction.TransformResponse;
            if (apiRecord.ResponseContent != null || apiRecord.Mock)
                return XmlAction.ReplayFromMock;            

            return XmlAction.ReplayFromRealApi;            
        }
    }
}
