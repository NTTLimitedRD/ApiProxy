using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using ApiProxy.Contracts;

namespace ApiProxy
{
    public class RealApiApiProxyProvider : IApiProxyProvider
    {
        protected IApiProxyConfiguration ProxyConfiguration { get; private set; }

        protected IApiRecorder ApiRecorder { get; private set; }
        
        public RealApiApiProxyProvider(IApiProxyConfiguration proxyConfiguration, IApiRecorder recorder)
        {
            if (proxyConfiguration == null)
                throw new ArgumentNullException(nameof(proxyConfiguration));
            ProxyConfiguration = proxyConfiguration;

            if (recorder == null)
                throw new ArgumentNullException(nameof(recorder));
            ApiRecorder = recorder;
        }
        	
		public virtual async Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, Guid activityId)
		{
		    if (request == null)
		        throw new ArgumentNullException(nameof(request));

            ApiProxyEvents.Raise.ReceivedRequest(request.RequestUri.ToString());            
            try
            {
                return await GetApiResponseFromDefaultApi(request, activityId);
            }
            catch (Exception ex)
            {
                ApiProxyEvents.Raise.UnhandledException(request.RequestUri.ToString(), ex.Message, ex.StackTrace ?? String.Empty);

                return request.CreateResponse(HttpStatusCode.InternalServerError,
                                                new ErrorResponse
                                                {
                                                    ActivityId = activityId,
                                                    Message = ex.Message
                                                },
                                                new XmlMediaTypeFormatter()
                                                );
            }            
        }

        private async Task<HttpResponseMessage> GetApiResponseFromDefaultApi(HttpRequestMessage request, Guid activityId)
        {
            // Else pass the request to the default api
            if (ProxyConfiguration.DefaultApiAddress == null || !ProxyConfiguration.DefaultApiAddress.IsAbsoluteUri)
            {
                return request.CreateResponse(HttpStatusCode.InternalServerError,
                    new ErrorResponse
                    {
                        ActivityId = activityId,
                        Message = "Default Api Address is not valid"
                    }
                    );
            }

            //Trust all certificates
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = ProxyConfiguration.DefaultApiAddress;
                var requestUri = new Uri(client.BaseAddress, request.RequestUri.PathAndQuery);
                ApiProxyEvents.Raise.VerboseMessaging($"Routing Request to the Default Api Address : {requestUri}");
                client.DefaultRequestHeaders.Clear();
                foreach (var header in request.Headers)
                {
                    if (header.Key != "Host")
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                HttpResponseMessage defaultApiresponse = null;

                if (request.Method == HttpMethod.Get)
                {
                    defaultApiresponse =
                        await
                            client.GetAsync(requestUri,
                                HttpCompletionOption.ResponseContentRead);
                }
                else if (request.Method == HttpMethod.Post)
                {
                    defaultApiresponse =
                        await client.PostAsync(requestUri, request.Content);
                }
                else if (request.Method == HttpMethod.Put)
                {
                    defaultApiresponse =
                        await client.PutAsync(requestUri, request.Content);

                }
                else if (request.Method == HttpMethod.Delete)
                {
                    defaultApiresponse = await client.DeleteAsync(requestUri);
                }
                if (defaultApiresponse != null)
                {
                    ApiProxyEvents.Raise.VerboseMessaging(
                        $"Response from the Default Api , request :{requestUri}, status :{defaultApiresponse.StatusCode}");
                    // Record the api req/res if enabled
                    await ApiRecorder.RecordApi(request, defaultApiresponse);
                    return defaultApiresponse;
                }
                return request.CreateResponse(HttpStatusCode.InternalServerError,
                    new ErrorResponse
                    {
                        ActivityId = activityId,
                        Message = "Request to Default Api with the given Method cannot be done"
                    }
                    );
            }
        }
    }
}