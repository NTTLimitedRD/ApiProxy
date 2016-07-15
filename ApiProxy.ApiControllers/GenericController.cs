using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ApiProxy.Contracts;

namespace ApiProxy.ApiControllers
{
    public class GenericController : ApiController
    {
        private readonly IApiProxy _apiProxy;
        public GenericController(IApiProxy apiProxy)
        {
            _apiProxy = apiProxy;
        }

        public async Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _apiProxy.ProcessRequestAsync(request);
        }      
    }
}