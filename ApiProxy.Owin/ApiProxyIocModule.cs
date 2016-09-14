using System;
using Autofac;
using DD.ApiProxy.ApiControllers;
using DD.ApiProxy.Contracts;
using DD.ApiProxy.Xml;

namespace DD.ApiProxy.Owin
{
    public class ApiProxyIocModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");

            builder.RegisterType<ApiProxyConfiguration>().AsImplementedInterfaces();
            builder.RegisterType<DD.ApiProxy.ApiProxy>().AsImplementedInterfaces();
            // Api Provider Factory 
            builder.RegisterType<XmlContentTypeApiProxyProvider>().Named<IApiProxyProvider>("application/xml");
            builder.RegisterType<RealApiApiProxyProvider>().Named<IApiProxyProvider>("default");
            
            builder.RegisterType<ApiProxyProviderFactory>().AsImplementedInterfaces();

            // For reading the Api Records
            builder.RegisterType<FileBasedApiProxyRecordProvider>().AsImplementedInterfaces();

        }
    }
}
