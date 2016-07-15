using System.Net.Http;
using System.Threading.Tasks;

namespace ApiProxy.Contracts
{   
    public interface IApiProxyRecordProvider
    {        
        ApiRecord GetApiRecord(HttpRequestMessage request);
    }
}
