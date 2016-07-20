using System.Net.Http;
using System.Threading.Tasks;

namespace DD.ApiProxy.Contracts
{
    /// <summary>
    /// Api Recorder
    /// </summary>
    public interface IApiProxy
    {
        /// <summary>
        /// Process The Http Request Asynchronously
        /// </summary>
        /// <param name="request">The Http Request Object</param>
        /// <returns>Task that returns Http Response</returns>
        Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request);
    }
}
