using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DD.ApiProxy.Contracts
{
    /// <summary>
    /// Api Recorder
    /// </summary>
    public interface IApiProxyProvider
    {
        /// <summary>
        /// Process The Http Request Asynchronously
        /// </summary>        
        /// <param name="request">The Http Request Object</param>
        /// <param name="activityId">Activity Id</param>
        /// <returns>Task that returns Http Response</returns>
        Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, Guid activityId);
    }
}
