using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.Xml
{
    public class XmlContentTypeApiProxyProvider :
        IApiProxyProvider
    {
        private XmlApiRecord _xmlApiRecord;
        protected IApiProxyConfiguration ProxyConfiguration { get; private set; }

        protected IApiRecorder ApiRecorder { get; private set; }

        public XmlContentTypeApiProxyProvider(IApiProxyConfiguration proxyConfiguration, IApiRecorder apiRecorder, XmlApiRecord apiRecord)            
        {
            if (proxyConfiguration == null)
                throw new ArgumentNullException(nameof(proxyConfiguration));
            ProxyConfiguration = proxyConfiguration;

            if (apiRecorder == null)
                throw new ArgumentNullException(nameof(apiRecorder));
            ApiRecorder = apiRecorder;

            if (apiRecord == null)
                throw new ArgumentNullException(nameof(apiRecord));
            _xmlApiRecord = apiRecord;
        }
        	
		public async Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, Guid activityId)
		{
		    if (request == null)
		        throw new ArgumentNullException(nameof(request));

            ApiProxyEvents.Raise.ReceivedRequest(request.RequestUri.ToString());		    

            // We now catch an exception from the runner
            try
            {               
                var action = _xmlApiRecord.GetXmlAction(ProxyConfiguration);
                switch(action)
                {
                    case XmlAction.ReplayFromTransformedRequestMock:
                        {
                            if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
                            {
                                var responseContent =
                                    await TransformXmlRequestAndReplay(request, activityId, _xmlApiRecord, request.Content);
                                return ReplayResponseFromApiRecord(_xmlApiRecord, responseContent);
                            }
                            return ReplayResponseFromApiRecord(_xmlApiRecord);
                        }
                    case XmlAction.ReplayFromMock:
                        {
                            return ReplayResponseFromApiRecord(_xmlApiRecord);
                        }                   
                    case XmlAction.ReplayFromRealApi:
                        {
                            return await GetApiResponseFromDefaultApi(request, _xmlApiRecord, activityId);
                        }
                    case XmlAction.TransformResponse:
                        {
                            var response = await GetApiResponseFromDefaultApi(request, _xmlApiRecord, activityId);
                            HttpStatusCode expectedCode = HttpStatusCode.OK;
                            Enum.TryParse(_xmlApiRecord.StatusCode, out expectedCode);
                            if (response.StatusCode != expectedCode)
                            {
                                ApiProxyEvents.Raise.VerboseMessaging(
                                    $"Skipping the api response transformation as the status code is not expected, uri : {request.RequestUri.ToString()}, status code:{response.StatusCode}");
                                return await Task.FromResult(response);
                            }
                            return await TransformApiResponse(request, activityId, _xmlApiRecord, response);
                        }
                    case XmlAction.None:
                    {
                        return request.CreateResponse(HttpStatusCode.InternalServerError,
                            new ErrorResponse
                            {
                                ActivityId = activityId,
                                Message = "The Action is not supported"
                            }
                            );
                    }
                }
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

		    return request.CreateResponse(HttpStatusCode.InternalServerError,
		        new ErrorResponse
		        {
		            ActivityId = activityId,
		            Message = "Default Api Address is not found"
		        }
		        );
		}
      
        private HttpResponseMessage ReplayResponseFromApiRecord(XmlApiRecord apiRecord)
        {            
            return ReplayResponseFromApiRecord(apiRecord, apiRecord.ResponseContent);
        }

        private HttpResponseMessage ReplayResponseFromApiRecord(XmlApiRecord apiRecord, string responseContent)
        {
            HttpStatusCode statusCode = HttpStatusCode.BadRequest;
            Enum.TryParse(apiRecord.StatusCode, true, out statusCode);
            var contentType = apiRecord.ResponseContentType;
            return new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = new StringContent(
                responseContent,
                Encoding.UTF8,
                contentType
                )
            };
        }
        private async Task<HttpResponseMessage> GetApiResponseFromDefaultApi(HttpRequestMessage request, XmlApiRecord apiRecord, Guid activityId)
        {
            // Else pass the request to the default api
            if (ProxyConfiguration.DefaultApiAddress == null)
            {
                return request.CreateResponse(HttpStatusCode.InternalServerError,
                    new ErrorResponse
                    {
                        ActivityId = activityId,
                        Message = "Default Api Address is not found"
                    }
                    );
            }

            //Trust all certificates
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = ProxyConfiguration.DefaultApiAddress;
                var requestUri = new Uri(client.BaseAddress, GetRelativeQueryParameter(request, apiRecord));
                ApiProxyEvents.Raise.VerboseMessaging($"Routing Request to the Default Api Address : {requestUri}");
                client.DefaultRequestHeaders.Clear();
                foreach (var header in request.Headers)
                {
                    if (header.Key != "Host")
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                HttpResponseMessage defaultApiresponse = null;
                var transformOutput = new List<string>();

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
                        await client.PostAsync(requestUri, await TransformApiRequestContent(request, activityId, apiRecord, request.Content));
                }
                else if (request.Method == HttpMethod.Put)
                {
                    defaultApiresponse =
                        await client.PutAsync(requestUri, await TransformApiRequestContent(request, activityId, apiRecord, request.Content));

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

        private string GetRelativeQueryParameter(HttpRequestMessage request, XmlApiRecord record)
        {
            if(string.IsNullOrWhiteSpace(record.Configuration?.Transform?.Query?.TrimQueryParameters))
            {
                return request.RequestUri.PathAndQuery;
            }
            var resultantQueries = new List<KeyValuePair<string, string>>();
            var queryParametersTobeTrimmed = record.Configuration?.Transform?.Query?.TrimQueryParameters.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(queryParameterName => queryParameterName.ToLowerInvariant()).Select(
                queryParameterName =>
                {
                    var keyValue = queryParameterName.Split(new char[] { '=' });
                    if (keyValue.Length >= 2)
                        return new KeyValuePair<string, string>(keyValue[0], keyValue[1]);

                    return new KeyValuePair<string, string>(queryParameterName, string.Empty);
                });

            var queryParameters = request.GetQueryNameValuePairs();
            if (queryParametersTobeTrimmed.Any() && queryParameters.Any())
            {
                resultantQueries.AddRange(
                   queryParameters.Where(queryParameter =>
                   {
                       var selectedQuery = queryParametersTobeTrimmed.FirstOrDefault(
                           pair =>
                           {
                               if (pair.Key == queryParameter.Key.ToLowerInvariant())
                               {
                                   if (pair.Value != String.Empty)
                                       return queryParameter.Value.ToLowerInvariant() == pair.Value;
                               }
                               return false;
                           });
                       return !string.IsNullOrWhiteSpace(selectedQuery.Key);
                   }));
                                
                return resultantQueries.Any() ? string.Format("{0}?{1}", request.RequestUri.AbsolutePath, string.Join("&", resultantQueries.Select(parameter => parameter.Key + "=" + parameter.Value))) : request.RequestUri.AbsolutePath;
            }
            return request.RequestUri.PathAndQuery;
        }

        private async Task<HttpResponseMessage> TransformApiResponse(HttpRequestMessage request, Guid activityId, XmlApiRecord apiRecord, HttpResponseMessage realResponse)
        {
            if (apiRecord.ResponseContentType != "application/xml")
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Skipping Response Transforming the as content type is not application/xml for request:{0}", request.RequestUri));
                return realResponse;
            }

            var xsltFileName = apiRecord?.Configuration?.Transform?.Body?.XsltFileName;
            if (string.IsNullOrWhiteSpace(xsltFileName))
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Skipping Response Transforming the as xmlt path is not specified for request:{0}", request.RequestUri));
                return realResponse;
            }

            var effectiveXsltFileNamePath = Path.Combine(Path.GetDirectoryName(apiRecord.MockFilePath), xsltFileName);         
            var xmlContent = await realResponse.Content.ReadAsStringAsync();
            IList<string> transformOutput;
            ApiProxyEvents.Raise.VerboseMessaging(string.Format("Transforming the response using xslt:{0} for request:{1}, real response:{2}", effectiveXsltFileNamePath, realResponse.RequestMessage.RequestUri, xmlContent));
            var transformedContent = TransformContent(request, xmlContent, effectiveXsltFileNamePath, out transformOutput);
            if (string.IsNullOrWhiteSpace(transformedContent))
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Response Transform was skipped request:{0}, real response:{1}", realResponse.RequestMessage.RequestUri, xmlContent));
                return realResponse;
            }
            var contentType = realResponse.Content.Headers.ContentType.MediaType;
            realResponse.Content = new StringContent(transformedContent,
                            Encoding.UTF8,
                            contentType);
            return realResponse;
        }


        private async Task<HttpContent> TransformApiRequestContent(HttpRequestMessage request, Guid activityId, XmlApiRecord apiRecord, HttpContent realContent)
        {
            var contentType = request.Content.Headers.ContentType.MediaType;
            if (contentType != "application/xml" && contentType != "application/x-www-form-urlencoded")
            {
                ApiProxyEvents.Raise.VerboseMessaging(
                    $"Skipping Request Transforming the as content type is not application/xml or application/x-www-form-urlencoded for request:{request.RequestUri}");
                return realContent;
            }

            if (contentType == "application/xml")
            {
                return await TransformApiXmlRequestContent(request, activityId, apiRecord, realContent);
            }
            return await TransformApiUrlEncodeRequestContent(request, activityId, apiRecord, realContent);
        }

        private async Task<HttpContent> TransformApiUrlEncodeRequestContent(HttpRequestMessage request, Guid activityId, XmlApiRecord apiRecord, HttpContent realContent)
        {
            var contentType = request.Content.Headers.ContentType.MediaType;
            if (contentType != "application/x-www-form-urlencoded")
            {
                ApiProxyEvents.Raise.VerboseMessaging(
                    $"Skipping Request Transforming the as content type is not application/x-www-form-urlencoded for request:{request.RequestUri}");
                return realContent;
            }
                      
            var queryParametersTobeTrimmed = apiRecord.Configuration?.Transform?.RequestContent?.TrimQueryParameters.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(queryParameterName => queryParameterName.ToLowerInvariant()).Select(
                queryParameterName =>
                {
                    var keyValue = queryParameterName.Split(new char[] {'='});
                    if(keyValue.Length >= 2)
                        return new KeyValuePair<string, string>(keyValue[0], keyValue[1]);

                    return new KeyValuePair<string, string>(queryParameterName, string.Empty);
                }).ToList();
            if (queryParametersTobeTrimmed != null && !queryParametersTobeTrimmed.Any())
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Skipping Request Transforming the as no query parametes needs to be trimmed for request:{0}", request.RequestUri));
                return realContent;
            }
          
            var urlEncodedContent = await realContent.ReadAsStringAsync();

            ApiProxyEvents.Raise.VerboseMessaging(string.Format("Transforming the request by trimming content:{0} for request:{1}, real response:{2}", apiRecord.Configuration?.Transform?.RequestContent?.TrimQueryParameters, request.RequestUri, urlEncodedContent));
            var urlParameters = urlEncodedContent.Split(new char[] {'&'}, StringSplitOptions.RemoveEmptyEntries).Select(
                pair =>
                {
                    var keyValue = pair.Split(new char[] {'='});
                    if (keyValue.Length > 0 && keyValue.Length < 2)
                        return new KeyValuePair<string, string>(keyValue[0], string.Empty);
                    return new KeyValuePair<string, string>(keyValue[0], keyValue[1]);
                }).ToList();

            if (string.IsNullOrWhiteSpace(urlEncodedContent))
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Request Transform was skipped for request:{0}, real response:{1}", request.RequestUri, urlEncodedContent));
                return realContent;
            }

            var transformedContent = urlEncodedContent;
            var resultantQueries = new List<KeyValuePair<string, string>>();
            if (queryParametersTobeTrimmed != null && (queryParametersTobeTrimmed.Any() && urlParameters.Any()))
            {
                resultantQueries.AddRange(
                    urlParameters.Where(queryParameter =>
                    {
                        var selectedQuery = queryParametersTobeTrimmed.FirstOrDefault(
                            pair =>
                            {
                                if (pair.Key == queryParameter.Key.ToLowerInvariant())
                                {
                                    if (pair.Value != String.Empty)
                                        return queryParameter.Value.ToLowerInvariant() != pair.Value;
                                }
                                return false;
                            });
                        return !string.IsNullOrWhiteSpace(selectedQuery.Key);
                    }));
                transformedContent = resultantQueries.Any() ? string.Join("&", resultantQueries.Select(parameter => parameter.Key + "=" + parameter.Value)) : string.Empty;
            }
            return new StringContent(transformedContent,
                            Encoding.UTF8,
                            contentType);
        }

        private async Task<HttpContent> TransformApiXmlRequestContent(HttpRequestMessage request, Guid activityId, XmlApiRecord apiRecord, HttpContent realContent)
        {
            if (!request.IsContentMediaTypeXml())
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Skipping Request Transforming the as content type is not application/xml for request:{0}", request.RequestUri));
                return realContent;
            }
            
            var xsltFileName = apiRecord?.Configuration?.Transform?.RequestContent?.XsltFileName;
            if (string.IsNullOrWhiteSpace(xsltFileName))
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Skipping Request Transforming the as xmlt path is not specified for request:{0}", request.RequestUri));
                return realContent;
            }

            var effectiveXsltFileNamePath = Path.Combine(Path.GetDirectoryName(apiRecord.MockFilePath), xsltFileName);
            var xmlContent = await realContent.ReadAsStringAsync();
            IList<string> transformOutput;
            ApiProxyEvents.Raise.VerboseMessaging(string.Format("Transforming the response using xslt:{0} for request:{1}, real response:{2}", effectiveXsltFileNamePath, request.RequestUri, xmlContent));
            var transformedContent = TransformContent(request, xmlContent, effectiveXsltFileNamePath, out transformOutput);
            if (string.IsNullOrWhiteSpace(transformedContent))
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Response Transform was skipped request:{0}, real response:{1}", request.RequestUri, xmlContent));
                return realContent;
            }
            var contentType = realContent.Headers.ContentType.MediaType;
            return new StringContent(transformedContent,
                            Encoding.UTF8,
                            contentType);
        }

        private async Task<string> TransformXmlRequestAndReplay(HttpRequestMessage request, Guid activityId, XmlApiRecord apiRecord, HttpContent realContent)
        {
            var transformedContent = apiRecord.ResponseContent;
            if (!request.IsContentMediaTypeXml())
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Skipping Request Transforming the as content type is not application/xml for request:{0}", request.RequestUri));
                return transformedContent;
            }

            var xsltFileName = apiRecord?.Configuration?.Transform?.RequestContent?.XsltFileName;
            if (string.IsNullOrWhiteSpace(xsltFileName))
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Skipping Request Transforming the as xmlt path is not specified for request:{0}", request.RequestUri));
                return transformedContent;
            }

            var effectiveXsltFileNamePath = Path.Combine(Path.GetDirectoryName(apiRecord.MockFilePath), xsltFileName);
            var xmlContent = await realContent.ReadAsStringAsync();
            IList<string> transformOutput;
            ApiProxyEvents.Raise.VerboseMessaging(string.Format("Transforming the response using xslt:{0} for request:{1}, real response:{2}", effectiveXsltFileNamePath, request.RequestUri, xmlContent));
            transformedContent = TransformContent(request, xmlContent, effectiveXsltFileNamePath, out transformOutput);
            if (string.IsNullOrWhiteSpace(transformedContent))
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Response Transform was skipped request:{0}, real response:{1}", request.RequestUri, xmlContent));
                return apiRecord.ResponseContent;
            }

            Dictionary<string, string> outDictionary = new Dictionary<string, string>();
            transformOutput.ToList().ForEach(
                msg =>
                {
                    var pair = msg.Trim(' ').Split(new char[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                    if (!outDictionary.ContainsKey(pair[0]))
                    {
                        outDictionary.Add(pair[0], pair.Length > 1 ? pair[1] : String.Empty);
                    }
                });

            if (outDictionary.ContainsKey("StatusCode"))
            {
                apiRecord.StatusCode = outDictionary["StatusCode"];
            }

            return transformedContent;
        }

        private string TransformContent(HttpRequestMessage request, string inputXmlContent, string xsltPath, out IList<string> outputMessages)
        {
            var outputMessagesFromXslt = new List<string>();
            outputMessages = new List<string>();
            if (string.IsNullOrWhiteSpace(inputXmlContent))
            {
                ApiProxyEvents.Raise.VerboseMessaging(
                    $"Skipping Transform as the xmlContent is empty for request:{request.RequestUri.ToString()}");                
                return inputXmlContent;
            }

            if (!File.Exists(xsltPath))
            {
                ApiProxyEvents.Raise.VerboseMessaging(
                    $"Skipping Transform as the xslt file {xsltPath} doesnt exists for request:{request.RequestUri.ToString()}");                
                return inputXmlContent;
            }

            var xslMarkup = File.ReadAllText(xsltPath);
            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(XmlReader.Create(new StringReader(xslMarkup)));
            using (MemoryStream stream = new MemoryStream())
            {
                StreamWriter csvWriter = new StreamWriter(stream, Encoding.UTF8);
                var xmlReader = XmlReader.Create(new StringReader(inputXmlContent));
                var xmlWriter = XmlWriter.Create(csvWriter);
                // Execute the transformation and output the results to a file.                
                var argument = new XsltArgumentList();
                argument.XsltMessageEncountered += (o, args) =>
                {
                    if (args != null && !string.IsNullOrWhiteSpace(args.Message))
                    {
                        outputMessagesFromXslt.Add(args.Message);
                    }
                };
                xslt.Transform(xmlReader, argument, xmlWriter);

                stream.Position = 0;
                var sr = new StreamReader(stream);
                outputMessages = outputMessagesFromXslt;
                return sr.ReadToEnd();
            }
        }
    }
}