using System.Collections.Generic;

namespace ServiceHoster.Controller
{
    public class RunnerMetadata
    {
        public HostMetadata HostInfo { get; }
        public IEnumerable<ServiceMetadata> ServiceInfo { get; }

        public RunnerMetadata(HostMetadata hostInfo, IEnumerable<ServiceMetadata> serviceInfo)
        {
            HostInfo = hostInfo;
            ServiceInfo = serviceInfo;
        }
    }
}