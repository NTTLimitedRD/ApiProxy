namespace ApiProxy.Xml
{
    public enum XmlAction
    {
        None = -1,
        ReplayFromMock = 0,
        TransformResponse = 1,
        ReplayFromRealApi,
        ReplayFromTransformedRequestMock
    }
}
