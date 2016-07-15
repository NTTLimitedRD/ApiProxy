using System;

namespace ApiProxy.Contracts
{
    /// <summary>
    /// Api Proxy Configuration
    /// </summary>
    public interface IApiProxyConfiguration
    {
        string ApiMocksPath { get; }

        string ApiRecordingPath { get; }

        Uri DefaultApiAddress { get; }    
            
        bool FallbackToDefaultApi { get; }

        bool RecordApiRequestResponse { get; }
    }
}
