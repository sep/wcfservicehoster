﻿using System;
using System.Collections.Generic;
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
            Services = Assembly.Load(new AssemblyName { CodeBase = assemblyPath })
                .GetTypes()
                .Where(t => t.IsWcfService())
                .Select(SetUpService)
                .ToArray();
        }

        private static ServiceMetadata SetUpService(Type serviceType)
        {
            var metadata = new ServiceMetadata
            {
                FullName = serviceType.FullName,
                Host = new ServiceHost(serviceType, new Uri[0]),
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
        }

        public void OpenServices()
        {
            foreach (var service in Services)
                service.Host.Open();
        }

        public void CloseServices()
        {
            foreach (var service in Services)
                service.Host.Close();
        }

        public (string, CommunicationState)[] Status => Services.Select(s => (s.FullName, s.Host.State)).ToArray();

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
