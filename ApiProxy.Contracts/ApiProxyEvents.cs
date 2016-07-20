using System;
using Microsoft.Diagnostics.Tracing;

namespace ApiProxy.Contracts
{
    [EventSource(Name = EventSourceName)]
    public sealed class ApiProxyEvents : EventSource
	{
		/// <summary>
		/// The singleton instance.
		/// </summary>
		private static readonly Lazy<ApiProxyEvents> Instance =
			new Lazy<ApiProxyEvents>(() => new ApiProxyEvents());

		public const string EventSourceName = "ApiProxy";

		public static ApiProxyEvents Raise
		{
			get
			{
				return Instance.Value;
			}
		}

        private ApiProxyEvents()
#if DEBUG
			: base(true /* throwOnEventWriteErrors */)
#endif
		{
		}

		/// <summary>
		/// The start-up event
		/// </summary>                
		[
            Microsoft.Diagnostics.Tracing.Event(
				Events.ApiStart,
				Message = "Started API",
				Level = EventLevel.Informational
			)
		]
		public void ApiStart()
		{
			WriteEvent(Events.ApiStart);
		}

		/// <summary>
		/// The received request.
		/// </summary>                
		[
			Event(
				Events.ApiStop,
				Message = "Stopped API",
				Level = EventLevel.Informational
			)
		]
		public void ApiStop()
		{
			WriteEvent(Events.ApiStop);
		}

		/// <summary>
		/// The received request.
		/// </summary>
		/// <param name="requestUri">
		/// The request uri.
		/// </param>
		[
			Event(
				Events.ReceivedRequest,
				Message = "Received request: {0}",
				Level = EventLevel.Informational
			)
		]
		public void ReceivedRequest(string requestUri)
		{
			WriteEvent(Events.ReceivedRequest, requestUri);
		}
              
		/// <summary>
		/// Unhandled Exception message
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		/// <param name="stackTrace">The stack trace.</param>
		[
			Event(
				Events.UnhandledException,
				Message = "Unhandled exception in service uri:{0}, error:{0}, stack {1}",
				Level = EventLevel.Error,
                Version = 2
				)
		]
		public void UnhandledException(string uri, string errorMessage, string stackTrace = "")
		{
			WriteEvent(Events.UnhandledException, uri, errorMessage, stackTrace);
		}

		/// <summary>
		/// Configuration error.
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		[
			Event(
				Events.ConfigurationError,
				Message = "Cannot start service from configuration error-  {0}",
				Level = EventLevel.Error
			)
		]
		public void ConfigurationError(string errorMessage)
		{
			WriteEvent(Events.ConfigurationError, errorMessage);
		}

		/// <summary>
		/// Configuration error.
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		[
			Event(
				Events.VerboseMessaging,
				Message = "Verbose Message - {0}",
				Level = EventLevel.Verbose
			)
		]
		public void VerboseMessaging(string errorMessage)
		{
			WriteEvent(Events.VerboseMessaging, errorMessage);
		}

		/// <summary>
		/// Event Id constants.
		/// </summary>
		public static class Events
		{
			/// <summary>
			///	Received the request.
			/// </summary>
			public const int ReceivedRequest = 1000;

			/// <summary>
			/// The unhandled exception message code.
			/// </summary>
			public const int UnhandledException = 1003;

			/// <summary>
			/// The configuration error message code.
			/// </summary>
			public const int ConfigurationError = 1004;

			/// <summary>
			/// The start-up event
			/// </summary>
			public const int ApiStart = 1005;

			/// <summary>
			/// The stop event
			/// </summary>
			public const int ApiStop = 1006;
			
			public const int VerboseMessaging = 1008;
		}
	}
}