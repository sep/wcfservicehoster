using System;
using System.ServiceModel;

namespace ServiceHoster.Backend
{
    [Serializable]
    public class ServiceStatus
    {
        public string ServiceName { get; }
        public CommunicationState State { get; }

        public ServiceStatus(string serviceName, CommunicationState state)
        {
            ServiceName = serviceName;
            State = state;
        }
    }
}