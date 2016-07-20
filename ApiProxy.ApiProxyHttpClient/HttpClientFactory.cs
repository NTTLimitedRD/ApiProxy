﻿using System;
using System.Net;
using System.Net.Http;
using DD.ApiProxy.Contracts;

namespace DD.ApiProxy.ApiProxyHttpClient
{
    public static class HttpClientFactory
    {
        public static HttpClient GetHttpClient(IApiProxyConfiguration configuration, ICredentials credentials = null)
        {
            var baseUri = configuration.DefaultApiAddress ?? new Uri("https://localhost/");
            return new HttpClient(new ApiProxyClientHandler(configuration)
                                    {
                                        Credentials = credentials,
                                        PreAuthenticate = true
                                    })
            {
                BaseAddress = baseUri
            };
        }
    }
}