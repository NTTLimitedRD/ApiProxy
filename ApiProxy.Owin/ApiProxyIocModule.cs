using System;
using ApiProxy.ApiControllers;
using ApiProxy.Contracts;
using ApiProxy.Xml;
using Autofac;

namespace ApiProxy.Owin
{
    public class ApiProxyIocModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");

            builder.RegisterType<ApiProxyConfiguration>().AsImplementedInterfaces();
            builder.RegisterType<ApiProxy>().AsImplementedInterfaces();
            // Api Provider Factory 
            builder.RegisterType<XmlContentTypeApiProxyProvider>().Named<IApiProxyProvider>("application/xml");
            builder.RegisterType<RealApiApiProxyProvider>().Named<IApiProxyProvider>("default");
            
            builder.RegisterType<ApiProxyProviderFactory>().AsImplementedInterfaces();

            // For reading the Api Records
            builder.RegisterType<FolderHeirarchyBasedApiProxyRecordProvider>().AsImplementedInterfaces();

        }
    }
}
