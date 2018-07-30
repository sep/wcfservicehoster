using System;
using System.ServiceModel;

namespace ServiceHoster.Backend
{
    [Serializable]
    public class ServiceMetadata
    {
        [NonSerialized]
        private ServiceHost _host;

        public string FullName { get; set; }
        public string[] MexEndpoints { get; set; }
        public string[] Endpoints { get; set; }
        public string WsdlEndpointAddress { get; set; }

        public ServiceHost Host
        {
            get => _host;
            set => _host = value;
        }
    }
}
