using System.Net.Http;

namespace DD.ApiProxy.Contracts
{   
    public interface IApiProxyRecordProvider
    {        
        ApiRecord GetApiRecord(HttpRequestMessage request);
    }
}
