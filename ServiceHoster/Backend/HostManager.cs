﻿using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.ServiceModel;
using System.ServiceModel.Description;
using Optional;
using Optional.Collections;

namespace ServiceHoster.Backend
{
    public class HostManager : MarshalByRefObject
    {
        public ServiceMetadata[] Services { get; private set; } = new ServiceMetadata[0];

        public void LoadServiceAssemblyAndServices(string assemblyPath)
        {
            var svcAssembly = Assembly.Load(new AssemblyName { CodeBase = assemblyPath });

            Services = svcAssembly
                .GetTypes()
                .Where(t => t.IsWcfServiceClass())
                .Select(t =>
                {
                    var metadata = new ServiceMetadata
                    {
                        FullName = t.FullName,
                        Host = new ServiceHost(t, new Uri[0]),
                    };

                    metadata.MexEndpoints = metadata.Host.Description.Endpoints
                        .Where(e => e.Contract.ContractType == typeof(IMetadataExchange))
                        .Select(e => e.Address.Uri.AbsoluteUri)
                        .ToArray();
                    metadata.Endpoints = metadata.Host.Description.Endpoints
                        .Where(e => e.Contract.ContractType != typeof(IMetadataExchange))
                        .Select(e => e.Address.Uri.AbsoluteUri)
                        .ToArray();

                    GetWsdlEndpoint(metadata).Match(
                        some: _ => metadata.WsdlEndpointAddress = _,
                        none: () => { });
                    return metadata;
                })
                .ToArray();
        }

        
        public void OpenServices()
        {
            foreach (var info in Services)
                info.Host.Open();
        }

        public void CloseServices()
        {
            foreach (var info in Services)
                info.Host.Close();
        }

        public override object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial)
                lease.InitialLeaseTime = TimeSpan.FromSeconds(0d);
            return lease;
        }

        private static Option<string> GetWsdlEndpoint(ServiceMetadata metadata)
        {
            var baseAddress = metadata.Host.BaseAddresses
                .Select(a => a.AbsoluteUri.EndsWith("/") ? a : new Uri(a.AbsoluteUri + "/"))
                .FirstOrDefault(a => a.Scheme == Uri.UriSchemeHttp);

            if (baseAddress == null)
                return Option.None<string>();

            return metadata.Host.Description.Behaviors
                .Select(x => x as ServiceMetadataBehavior)
                .Where(x => x != null)
                .Where(x => x.HttpGetEnabled)
                .Select(x => new Uri(baseAddress, x.HttpGetUrl) + "?wsdl")
                .FirstOrNone();
        }
    }
}