using System;
using System.Configuration;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Autofac;
using Autofac.Integration.WebApi;
using DD.ApiProxy.ApiControllers;
using Microsoft.Owin.Hosting;
using Owin;

namespace DD.ApiProxy.Owin
{
    /// <summary>
	/// The startup.
	/// </summary>
	public class Startup
	{
		/// <summary>
		/// This code configures Web API. The Startup class is specified as a type
		/// parameter in the WebApp.Start method.
		/// </summary>
		/// <param name="appBuilder">The application builder.</param>
		public void Configuration(IAppBuilder appBuilder)
		{
			// Configure Web API for self-host. 
			HttpConfiguration config = CreateConfiguration();

			// Construct the Autofac container
			IContainer container = BuildContainer();

			// Use autofac's dependency resolver, not the OWIN one
			config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

			// Wait for the initialization to complete (setup the socket)
			config.EnsureInitialized();
			
			appBuilder.UseAutofacMiddleware(container);
		    appBuilder.UseWebApi(config);
			appBuilder.UseAutofacWebApi(config);            
		}

		/// <summary>
		/// The build container.
		/// </summary>
		/// <returns>
		/// The <see cref="IContainer"/>.
		/// </returns>
		private IContainer BuildContainer()
		{
			ContainerBuilder builder = new ContainerBuilder();

			builder.RegisterApiControllers(typeof(GenericController).Assembly);
		    builder.RegisterModule(new ApiProxyIocModule());
			return builder.Build();
		}

		/// <summary>
		/// The create configuration.
		/// </summary>
		/// <returns>
		/// The <see cref="HttpConfiguration"/>.
		/// </returns>
		private HttpConfiguration CreateConfiguration()
		{
			// Configure Web API for self-host. 
			HttpConfiguration config = new HttpConfiguration();

			config.Services.Replace(typeof(IHttpControllerSelector), new GenericControllerSelector(config));
			config.Services.Replace(typeof(IHttpActionSelector), new GenericActionSelector(config));

			config.Routes.MapHttpRoute(
				name: "DefaultApi",
                routeTemplate: "{*catchall}",
                defaults: new { controller = "generic", action = "all" });

			return config;
		}

		/// <summary>
		/// The start.
		/// </summary>
		/// <returns>
		/// The <see cref="IDisposable"/>.
		/// </returns>
		public static IDisposable Start()
		{
		    var baseAddress = ConfigurationManager.AppSettings["BaseAddress"];
			// Start OWIN host 
			return WebApp.Start<Startup>(url: baseAddress);
		}
	}
}