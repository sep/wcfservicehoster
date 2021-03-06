﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ServiceHoster.Backend;

namespace ServiceHoster.Controller
{
    public class AppDomainHost
    {
        private readonly HostManager _hostManager;
        private readonly string _assemblyPath;
        private readonly string _assemblyConfig;

        public AppDomainHost(string assemblyPath, string configPath)
        {
            _assemblyPath = assemblyPath;
            _assemblyConfig = configPath;

            var assemblyFullPath = Path.GetFullPath(assemblyPath);
            var appDomain = CreateAppDomain(Path.GetDirectoryName(assemblyFullPath), configPath);

            _hostManager = CreateInstanceAndUnwrap<HostManager>(appDomain);
            _hostManager.LoadServiceAssemblyAndServices(assemblyPath);
        }

        public HostMetadata HostInfo => HostMetadata.From(_hostManager.HostInfo, _assemblyPath, _assemblyConfig);
        public IEnumerable<ServiceMetadata> Services => _hostManager.Services.Select(ServiceMetadata.From);
        public IEnumerable<ServiceStatus> Status => _hostManager.Status;

        public void OpenServices()
        {
            _hostManager.OpenServices();
        }

        public void CloseServices()
        {
            _hostManager.CloseServices();
        }

        private static AppDomain CreateAppDomain(string assemblyPath, string configPath)
        {
            return AppDomain.CreateDomain(configPath, AppDomain.CurrentDomain.Evidence, new AppDomainSetup
            {
                ConfigurationFile = Path.GetFullPath(configPath),
                ApplicationBase = assemblyPath
            });
        }

        private static T CreateInstanceAndUnwrap<T>(AppDomain appDomain)
        {
            var instanceFrom = appDomain.CreateInstanceFrom(Assembly.GetExecutingAssembly().Location, typeof(T).FullName);
            if (instanceFrom == null)
            {
                throw new TypeLoadException(string.Format(CultureInfo.CurrentUICulture, "couldnt load {0} - {1}", new object[]
                {
                    typeof (T).FullName,
                    Assembly.GetExecutingAssembly().FullName
                }));
            }
            return (T)instanceFrom.Unwrap();
        }
    }
}
