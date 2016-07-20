using System;
using System.Collections;
using System.Configuration;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceProcess;
using DD.ApiProxy.Contracts;
using DD.ApiProxy.Owin;

namespace DD.ApiProxy.ServiceHost
{
    /// <summary>
	/// Console host for Api Proxy Worker
	/// </summary>	
	static class Program
	{
		/// <summary>
		///		The main program entry-point.
		/// </summary>
		/// <param name="commandLineArguments">
		///		Command-line arguments.
		/// </param>
		/// <returns>
		///		The process exit-code.
		/// </returns>
		static int Main(string[] commandLineArguments)
		{
			if (commandLineArguments == null)
				throw new ArgumentNullException("commandLineArguments");

			try
			{
				ProgramOptions options;
				if (!ProgramOptions.ParseCommandLine(commandLineArguments, out options))
				{
					Console.Error.WriteLine(options.Help());
					return 5;
				}

				if (options.InstallService)
				{
					string serviceUser = options.ServiceUserName.Trim();
					string servicePassword = options.ServicePassword.Trim();
                    if (String.IsNullOrWhiteSpace(serviceUser) || String.IsNullOrWhiteSpace(servicePassword))
                    {
                        Console.WriteLine("Must supply a valid user-name and password.");
                        return 5;
                    }

                    if (serviceUser.IndexOf('\\') == -1)
						serviceUser = ".\\" + serviceUser;

					Console.WriteLine("Installing service (will run as user '{0}')...", serviceUser);
					InstallService(serviceUser, servicePassword);
					Console.WriteLine("Done.");

					return 0;
				}

				if (options.UninstallService)
				{
					Console.WriteLine("Uninstalling service...");
					UninstallService();
					Console.WriteLine("Done.");

					return 0;
				}

				if (options.RunService)
					return RunAsService();

				if (options.RunConsole)
					return RunAsConsole();

				// Nothing to do.
				return RunAsConsole();

				// return 1;
			}
			catch (Exception eUnexpected)
			{
				Console.Error.WriteLine(eUnexpected);
				Console.ReadLine();
				ApiProxyEvents.Raise.UnhandledException(string.Empty, eUnexpected.Message);
				return 1;
			}
		}

		/// <summary>
		///		Run as a console application.
		/// </summary>
		/// <returns>
		///		The process exit-code.
		/// </returns>
		private static int RunAsConsole()
		{
			try
			{
				// Raise a start service
				ApiProxyEvents.Raise.ApiStart();

				Startup.Start();
			}					
			catch (Exception ex)
			{
				Console.WriteLine("Error starting service - " + ex.Message);
				ApiProxyEvents.Raise.UnhandledException(string.Empty, ex.Message);
				Console.ReadLine();
				return -1;
			}
			Console.WriteLine("Started listening on the base address : " + ConfigurationManager.AppSettings["BaseAddress"]);
			Console.ReadLine();

			// Raise a stop service
			ApiProxyEvents.Raise.ApiStop();

			return 0;
		}

		/// <summary>
		///		Run the monitor as a Windows Service.
		/// </summary>
		/// <returns>
		///		The process exit-code.
		/// </returns>
		static int RunAsService()
		{
			try
			{
				// Raise a start service
				ApiProxyEvents.Raise.ApiStart();

				ServiceBase.Run(new ApiProxyService());
			}			
			catch (Exception ex)
			{
				Console.WriteLine("Error starting service - " + ex.Message);
				ApiProxyEvents.Raise.UnhandledException(string.Empty, ex.Message);
				Console.ReadLine();
				return -1;
			}
			return 0;
		}

		/// <summary>
		/// 		Register the Windows service.
		/// </summary>
		/// <param name="serviceUser">Service user</param>
		/// <param name="servicePassword">Service password</param>
		static void InstallService(string serviceUser, string servicePassword)
		{
			if (String.IsNullOrWhiteSpace(serviceUser))
				throw new ArgumentException("Argument cannot be null, empty, or composed entirely of whitespace: 'serviceUser'.", "serviceUser");

			if (String.IsNullOrWhiteSpace(servicePassword))
				throw new ArgumentException("Argument cannot be null, empty, or composed entirely of whitespace: 'servicePassword'.", "servicePassword");

			InstallContext installContext = CreateInstallContext();
			using (TransactedInstaller installer = CreateInstaller(installContext, serviceUser, servicePassword))
			{
				if (!EventLog.SourceExists(ApiProxyService.EventLogSourceName))
					EventLog.CreateEventSource(ApiProxyService.EventLogSourceName, "Application");

				Hashtable stateStore = new Hashtable();
				installer.Install(stateStore);

				string storeFilePath =
					Path.Combine(
					// ReSharper disable once AssignNullToNotNullAttribute
						Path.GetDirectoryName(
							typeof(Program)
								.Assembly
								.Location
						),
						"ApiProxyServiceInstallState.bin"
					);
				if (File.Exists(storeFilePath))
					File.Delete(storeFilePath);

				using (FileStream storeStream = File.Create(storeFilePath))
				{
					new BinaryFormatter()
						.Serialize(storeStream, stateStore);
				}
			}
		}

		/// <summary>
		///		Unregister the monitoring Windows service.
		/// </summary>
		static void UninstallService()
		{
			InstallContext installContext = CreateInstallContext();
			using (TransactedInstaller installer = CreateInstaller(installContext))
			{
				Hashtable stateStore;

				string storeFilePath =
					Path.Combine(
					// ReSharper disable once AssignNullToNotNullAttribute
						Path.GetDirectoryName(
							typeof(Program)
								.Assembly
								.Location
						),
                        "ApiProxyServiceInstallState.bin"
                    );
				if (File.Exists(storeFilePath))
				{
					using (FileStream storeStream = File.OpenRead(storeFilePath))
					{
						stateStore =
							(Hashtable)
								new BinaryFormatter()
									.Deserialize(storeStream);
					}
				}
				else
				{
					stateStore = new Hashtable();
				}

				installer.Uninstall(stateStore);
			}
		}

		/// <summary>
		///		Create the service installer.
		/// </summary>
		/// <param name="installContext">
		///		The installer context.
		/// </param>
		/// <param name="serviceUser">
		///		The service user name.
		/// </param>
		/// <param name="servicePassword">
		///		The service password.
		/// </param>
		/// <returns>
		///		The service transacted installer.
		/// </returns>
		static TransactedInstaller CreateInstaller(InstallContext installContext, string serviceUser = null, string servicePassword = null)
		{
			return
				new TransactedInstaller
				{
					Context = installContext,
					Installers =
					{
						new ServiceProcessInstaller
						{
							Context = installContext,
							Username = serviceUser,
							Password = servicePassword
						},
						new ServiceInstaller
						{
							Context = installContext,
							ServiceName = "ApiProxy",
							DisplayName = "Api Proxy service",
							Description = "Starts a Proxy API service that will return mocked api responses if provided, else will redirect to a base uri.",
							StartType = ServiceStartMode.Automatic
						}
					}
				};
		}

		/// <summary>
		///		Create an <see cref="InstallContext"/> for use by the service installer.
		/// </summary>
		/// <returns>
		///		The configured <see cref="InstallContext"/>.
		/// </returns>
		static InstallContext CreateInstallContext()
		{
			InstallContext installContext =
				new InstallContext(
					logFilePath: String.Empty,
					commandLine: new string[0]
				);

			installContext.Parameters["assemblyPath"] =
				String.Format(
					"\"{0}\" --service",
					typeof(Program)
						.Assembly
						.Location
				);

			return installContext;
		}
	}
}