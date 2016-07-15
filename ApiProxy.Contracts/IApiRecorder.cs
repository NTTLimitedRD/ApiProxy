using System.Net.Http;
using System.Threading.Tasks;

namespace ApiProxy.Contracts
{
    /// <summary>
    /// Api Recorder
    /// </summary>
    public interface IApiRecorder
    {
        Task RecordApi(HttpRequestMessage request, HttpResponseMessage response);
    }
}
