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
    public class FolderHeirarchyBasedApiProxyRecordProvider:IApiProxyRecordProvider, IApiRecorder
    {
        readonly IApiProxyConfiguration _proxyConfiguration;
        public FolderHeirarchyBasedApiProxyRecordProvider(IApiProxyConfiguration proxyConfiguration)
        {
            if (proxyConfiguration == null)
                throw new ArgumentNullException(nameof(proxyConfiguration));            
            _proxyConfiguration = proxyConfiguration;
        }

        public virtual ApiRecord GetApiRecord(HttpRequestMessage request)
        {
            // if mock api path is present, then try to find the matching api recording
            if (string.IsNullOrWhiteSpace(_proxyConfiguration.ApiMocksPath))
            {
                return new ApiRecord { Mock = false };
            }

            var effectiveMockResponsesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _proxyConfiguration.ApiMocksPath);
            var filePath = GetApiRecordFilePath(request, effectiveMockResponsesPath);
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                ApiProxyEvents.Raise.VerboseMessaging(string.Format("Found Api config file at : {0}", filePath));
                var fileContent = File.ReadAllText(filePath);
                var apiRecord = JsonConvert.DeserializeObject<ApiRecord>(fileContent);
                apiRecord.MockFilePath = filePath;
                apiRecord.RawContent = fileContent;
                return apiRecord;
            }
            ApiProxyEvents.Raise.VerboseMessaging("No Api config file found going to the real Api");
            return new ApiRecord { Mock = false };
        }

        private string GetApiRecordFilePath(HttpRequestMessage request, string mockResponsesRootPath)
        {
            // only consider url parts without queries
            var pathParts = request.RequestUri.AbsolutePath.Split(new char[] { '/' },
                StringSplitOptions.RemoveEmptyEntries);

            var currentNodePath = mockResponsesRootPath;

            currentNodePath = Path.Combine(currentNodePath, request.Method.ToString());
            if (!Directory.Exists(currentNodePath))
            {
                return string.Empty;
            }
            if (File.Exists(Path.Combine(currentNodePath, "_")))
            {
                // this is a global override for paths underneath
                return Path.Combine(currentNodePath, "_");
            }

            // Go till the last part as the last should be a file name
            for (var i = 0; i <= pathParts.Length - 1; i++)
            {
                string filePath = Path.Combine(currentNodePath, pathParts[i]);
                if (Directory.Exists(filePath))
                {
                    // this is the exact path
                    currentNodePath = filePath;
                    continue;
                }
                if (File.Exists(filePath))
                {
                    // this is the exact path
                    return filePath;
                }
                if (Directory.Exists(Path.Combine(currentNodePath, "_")))
                {
                    // this is a global override for paths underneath
                    currentNodePath = Path.Combine(currentNodePath, "_");
                    continue;
                }
                if (File.Exists(Path.Combine(currentNodePath, "_")))
                {
                    // this is a global override for paths underneath
                    return Path.Combine(currentNodePath, "_");
                }
                ApiProxyEvents.Raise.VerboseMessaging(
                    $"Exact match not found, the look up failed at : {currentNodePath}");
                return string.Empty;
            }

            // special folder for end path        
            var queryStringDirectoryPath = Path.Combine(currentNodePath, "_q");

            // special dir for query parameters
            if (Directory.Exists(queryStringDirectoryPath))
            {
                currentNodePath = queryStringDirectoryPath;

                // get all the files in the directory
                var files = Directory.GetFiles(currentNodePath);

                var querystring = request.RequestUri.Query.TrimStart('?');
                if (files.Contains(Path.Combine(currentNodePath, querystring)))
                    return Path.Combine(currentNodePath, querystring);

                // now handle queries
                var queries = request.RequestUri.Query.TrimStart('?').Split(new char[] { '&' },
                StringSplitOptions.RemoveEmptyEntries);

                // Add a special path for wildcards in all queryParameters 
                if (queries.Length > 0)
                {
                    var matchedQueryPart = string.Empty;
                    foreach (var q in queries)
                    {
                        var query = q.Replace(':', '.'); // As we can't have ':' in file name, so replacing this with '.'. This case came up with time value query e.g. 2014-05-31T00:00:00Z

                        var prevQueryString = String.IsNullOrWhiteSpace(matchedQueryPart) ? string.Empty : matchedQueryPart + "&";
                        var newMatchQueryPart = prevQueryString + query + "*";
                        var matchedFiles = Directory.GetFiles(currentNodePath, newMatchQueryPart);
                        if (matchedFiles.Length > 0)
                        {
                            matchedQueryPart = prevQueryString + query;
                            continue;
                        }

                        newMatchQueryPart = prevQueryString + query.Split('=')[0] + "=_" + "*";
                        matchedFiles = Directory.GetFiles(currentNodePath, newMatchQueryPart);
                        if (matchedFiles.Length > 0)
                        {
                            matchedQueryPart = prevQueryString + query.Split('=')[0] + "=_";
                            continue;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(matchedQueryPart))
                    {
                        var subFiles = Directory.GetFiles(currentNodePath, matchedQueryPart + "*");

                        if (subFiles.Length > 0)
                            return subFiles[0];
                    }
                    ApiProxyEvents.Raise.VerboseMessaging(
                        $"No Exact match found for the query parameters in : {currentNodePath}");
                }

                // wild card in the query strings folder
                if (files.Contains(Path.Combine(currentNodePath, "_")))
                    return Path.Combine(currentNodePath, "_");
            }
            // Flat node
            else if (File.Exists(Path.Combine(currentNodePath, "_")))
            {
                // this is a global override for paths underneath
                return Path.Combine(currentNodePath, "_");
            }

            return string.Empty;
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
                    RequestContent = await response.Content.ReadAsStringAsync(),
                    StatusCode = HttpStatusCode.OK.ToString(),
                    Uri = request.RequestUri.ToString(),
                    ResponseContentType = response.Content.Headers.ContentType.MediaType,
                    Mock = true,
                    ResponseContent =
                        response != null && response.Content != null
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
