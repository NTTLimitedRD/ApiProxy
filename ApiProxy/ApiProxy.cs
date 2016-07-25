using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy
{
    public class ApiProxy : IApiProxy
    {
        private readonly IApiProxyProviderFactory _apiProxyProviderFactory;
        readonly IApiProxyConfiguration _proxyConfiguration;
        private readonly IApiProxyRecordProvider _apiProxyRecordProvider;
        public ApiProxy(IApiProxyConfiguration proxyConfiguration, IApiProxyProviderFactory apiProxyProviderFactory, IApiProxyRecordProvider apiProxyRecordProvider)
        {
            if (proxyConfiguration == null)
                throw new ArgumentNullException(nameof(proxyConfiguration));
            _proxyConfiguration = proxyConfiguration;

            if (apiProxyProviderFactory == null)
                throw new ArgumentNullException(nameof(apiProxyProviderFactory));
            _apiProxyProviderFactory = apiProxyProviderFactory;

            if (apiProxyRecordProvider == null)
                throw new ArgumentNullException(nameof(apiProxyRecordProvider));
            _apiProxyRecordProvider = apiProxyRecordProvider;
        }
        	
		public async Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request)
		{
		    if (request == null)
		        throw new ArgumentNullException(nameof(request));

		    if (!request.Properties.ContainsKey(HttpPropertyKeys.HttpConfigurationKey))
		    {		      
                request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
            }

            var activityId = Guid.NewGuid();            
            ApiProxyEvents.Raise.ReceivedRequest(request.RequestUri.ToString());		                

            // We now catch an exception from the runner
            try
            {
                var apiRecord = _apiProxyRecordProvider.GetApiRecord(request);
                // Neither the mock cannot be executed nor the default api, so cant do anything
                if (!apiRecord.Mock && _proxyConfiguration.DefaultApiAddress == null)
                {
                    return request.CreateResponse(HttpStatusCode.InternalServerError,
                        new ErrorResponse
                        {
                            ActivityId = activityId,
                            Message = "Neither the mock path is there nor the default api, so cant do anything"
                        }
                        );
                }

                var proxyProvider = _apiProxyProviderFactory.GetApiProxyProvider(apiRecord, request, _proxyConfiguration);
                return await proxyProvider.ProcessRequestAsync(request, activityId);
            }
            catch (Exception ex)
            {
                ApiProxyEvents.Raise.UnhandledException(request.RequestUri.ToString(), ex.Message, ex.StackTrace ?? String.Empty);
                return request.CreateResponse(HttpStatusCode.InternalServerError,
                    new ErrorResponse
                    {
                        ActivityId = activityId,
                        Message = ex.Message
                    }
                    );
            }            
        }
    }
}