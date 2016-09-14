using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DD.ApiProxy.Contracts;
using Newtonsoft.Json;

namespace DD.ApiProxy
{   
    public class FileBasedApiProxyRecordProvider : IApiProxyRecordProvider, IApiRecorder
    {
        readonly IApiProxyConfiguration _proxyConfiguration;
        private IList<ApiRecord> _apiRecords;
         
        public FileBasedApiProxyRecordProvider(IApiProxyConfiguration proxyConfiguration)
        {
            if (proxyConfiguration == null)
                throw new ArgumentNullException(nameof(proxyConfiguration));            
            _proxyConfiguration = proxyConfiguration;

            LoadAllApiRecords();
        }

        private void LoadAllApiRecords()
        {
            var effectiveMockResponsesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _proxyConfiguration.ApiMocksPath);
            // if mock api path is present, then try to find the matching api recording
            if (string.IsNullOrWhiteSpace(_proxyConfiguration.ApiMocksPath) || !Directory.Exists(effectiveMockResponsesPath))
            {
                _apiRecords = new List<ApiRecord>();
            }
            var jsonFiles = Directory.GetFiles(effectiveMockResponsesPath, "*.json", SearchOption.AllDirectories);
            foreach (var configFile in jsonFiles)
            {
                try
                {
                    var fileContent = File.ReadAllText(configFile);
                    var apiRecord = JsonConvert.DeserializeObject<ApiRecord>(fileContent);
                    apiRecord.MockFilePath = configFile;
                    apiRecord.RawContent = fileContent;
                    _apiRecords.Add(apiRecord);
                }
                catch (Exception ex)
                { 
                    ApiProxyEvents.Raise.VerboseMessaging(string.Format("Skipping Api config file at : {0}, error:{1}", configFile, ex.ToString()));                   
                }
            }
        }

        public virtual ApiRecord GetApiRecord(HttpRequestMessage request)
        {
            // only consider url parts without queries
            var pathParts = request.RequestUri.AbsolutePath.Split(new char[] { '/' },
                StringSplitOptions.RemoveEmptyEntries);

            var apiRecord = _apiRecords.FirstOrDefault(record => String.CompareOrdinal(record.Uri.Trim(new char[] {'/'}), request.RequestUri.AbsolutePath.Trim(new []{'/'})) == 0);

            var currentMatchedPartialUri = string.Empty;
            List<ApiRecord> selectedRecords = _apiRecords.ToList();
            
            // Loop over the url path
            for (var i = 0; i <= pathParts.Length - 1; i++)
            {
                // Try matching exact path
                string partialUri = currentMatchedPartialUri + '/' + pathParts[i];
                var matchingApiRecords = selectedRecords.Where(record => record.Uri.Trim(new char[] { '/' }).ToLowerInvariant().StartsWith(partialUri.ToLowerInvariant())).ToList();

                if (!matchingApiRecords.Any())
                {
                    // Do wildCard Match 
                    partialUri = currentMatchedPartialUri + '/' + '*';
                    matchingApiRecords = selectedRecords.Where(record => record.Uri.Trim(new char[] { '/' }).ToLowerInvariant().StartsWith(partialUri.ToLowerInvariant())).ToList();
                }                

                if (!matchingApiRecords.Any())
                {
                    ApiProxyEvents.Raise.VerboseMessaging($"Stopping the search of the url {request.RequestUri} at : {partialUri}");
                    ApiProxyEvents.Raise.VerboseMessaging($"No Api config file found for url {request.RequestUri}");
                    return new ApiRecord { Mock = false };
                }

                currentMatchedPartialUri = partialUri;
                selectedRecords = matchingApiRecords;
            }
                        
            // now handle queries
            var queries = request.RequestUri.Query.TrimStart('?').Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            var matchedQueryPart = string.Empty;
            foreach (var q in queries)
            {               
                var prevQueryString = String.IsNullOrWhiteSpace(matchedQueryPart) ? string.Empty : matchedQueryPart + "&";
                var newMatchQueryPart = prevQueryString + q;

                string partialUri = currentMatchedPartialUri + '?' + newMatchQueryPart;
                var matchingApiRecords = selectedRecords.Where(record => record.Uri.Trim(new char[] { '/' }).ToLowerInvariant().StartsWith(partialUri.ToLowerInvariant())).ToList();

                if (!matchingApiRecords.Any())
                {
                    // Do wildCard Match 
                    newMatchQueryPart = prevQueryString + q.Split('=')[0] + "=*";                    
                    partialUri = currentMatchedPartialUri + '?' + newMatchQueryPart;
                    matchingApiRecords = selectedRecords.Where(record => record.Uri.Trim(new char[] { '/' }).ToLowerInvariant().StartsWith(partialUri.ToLowerInvariant())).ToList();
                }

                if (!matchingApiRecords.Any())
                {
                    break;
                }

                matchedQueryPart = newMatchQueryPart;
                selectedRecords = matchingApiRecords;
            }

            if (selectedRecords.Count >= 1)
            {
                ApiProxyEvents.Raise.VerboseMessaging($"Found the match for the url {request.RequestUri} in file : {selectedRecords[0].MockFilePath}");
                return selectedRecords[0];
            }

            ApiProxyEvents.Raise.VerboseMessaging($"No Api config file found for url {request.RequestUri}");
            return new ApiRecord { Mock = false };
        }

        public virtual async Task RecordApi(HttpRequestMessage request, HttpResponseMessage response)
        {
            try
            {
                var recordApi = _proxyConfiguration.RecordApiRequestResponse;
                if (!recordApi)
                    return;

                var apiRecordingPath = _proxyConfiguration.ApiRecordingPath;
                var effectiveMockResponsesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, apiRecordingPath);

                var recordedResponse = new ApiRecord
                {
                    Method = request.Method.ToString(),
                    RequestContent = request.Content != null
                            ? await request.Content.ReadAsStringAsync()
                            : string.Empty,
                    StatusCode = HttpStatusCode.OK.ToString(),
                    Uri = request.RequestUri.ToString(),
                    ResponseContentType = response.Content.Headers.ContentType.MediaType,
                    Mock = true,
                    ResponseContent =
                        response.Content != null
                            ? await response.Content.ReadAsStringAsync()
                            : string.Empty

                };
                var urlParts =
                    request.RequestUri.PathAndQuery.TrimEnd(new char[] {'?', '&'})
                        .Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                var fileName = "_";
               
                // last one contains the queryparameters 
                if (urlParts[urlParts.Count - 1].Contains("?"))
                {
                    var lastPart = urlParts[urlParts.Count - 1];
                    urlParts.RemoveAt(urlParts.Count - 1);
                    var queryParts = lastPart.Split(new char[] { '?' });
                    urlParts.AddRange(new string[] { queryParts[0], "_q" });
                    // file name will contain all the query parameters
                    fileName = queryParts[1];
                }
                else
                {
                    // always use _q to make sure filename and folder name doesnt clash for different paths
                    urlParts.Add("_q");
                }

                var folderParts = new List<string> { effectiveMockResponsesPath, request.Method.ToString() };
                folderParts.AddRange(urlParts);
                effectiveMockResponsesPath = Path.Combine(folderParts.ToArray());
                Directory.CreateDirectory(effectiveMockResponsesPath);
                string effectiveFilePath = Path.Combine(effectiveMockResponsesPath, fileName);
                File.WriteAllText(effectiveFilePath, JsonConvert.SerializeObject(recordedResponse));
            }
            catch (Exception ex)
            {
                ApiProxyEvents.Raise.VerboseMessaging(ex.ToString() + ex.StackTrace);
            }
        }
    }
}
