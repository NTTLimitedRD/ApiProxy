using System;
using System.Configuration;
using Microsoft.Owin.Hosting;

namespace ApiProxy.Owin
{
    /// <summary>
	/// The main program.
	/// </summary>
	class Program
	{
		/// <summary>
		/// The main.
		/// </summary>
		static void Main()
		{
            Uri baseAddress = new Uri(ConfigurationManager.AppSettings["BaseAddress"]);

            try
			{
				// Start OWIN host 
				using (WebApp.Start<Startup>(url: baseAddress.ToString()))
				{
					Console.WriteLine("Listening on {0}. Press any key to exit.", baseAddress);

					Console.ReadLine();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Could not start service {0}", ex.Message);
				Console.ReadLine();
			}
		}
	}
}