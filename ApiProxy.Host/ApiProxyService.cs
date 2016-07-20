using System;
using System.ServiceProcess;
using DD.ApiProxy.Owin;

namespace DD.ApiProxy.ServiceHost
{
    /// <summary>
	/// The dynamic power shell api service.
	/// </summary>
	public sealed class ApiProxyService : ServiceBase
	{
		/// <summary>
		/// The server.
		/// </summary>
		private IDisposable server;

		/// <summary>
		///		The Windows service name.
		/// </summary>
		private const string Name = "ApiProxyService";

		/// <summary>
		///		The service event log source name.
		/// </summary>
		public const string EventLogSourceName = "Api Proxy Service";

		/// <summary>
		/// Initialises a new instance of the <see cref="ApiProxyService"/> class.
		/// </summary>
		public ApiProxyService()
		{
			this.ServiceName = Name;
			EventLog.Source = EventLogSourceName;
		}

		/// <summary>
		/// The on start.
		/// </summary>
		/// <param name="args">
		/// The args.
		/// </param>
		protected override void OnStart(string[] args)
		{
			this.server = Startup.Start();
		}

		/// <summary>
		/// The on stop.
		/// </summary>
		protected override void OnStop()
		{
			if (this.server != null) this.server.Dispose();

			base.OnStop();
		}
	}
}