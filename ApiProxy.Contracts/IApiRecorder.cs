using System.Net.Http;
using System.Threading.Tasks;

namespace DD.ApiProxy.Contracts
{
    /// <summary>
    /// Api Recorder
    /// </summary>
    public interface IApiRecorder
    {
        Task RecordApi(HttpRequestMessage request, HttpResponseMessage response);
    }
}
