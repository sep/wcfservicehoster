namespace ServiceHoster.Controller
{
    public class ServiceMetadata
    {
        public string Name { get; }
        public string[] Endpoints { get; }
        public string[] MexEndpoints { get; }
        public string WsdlAddress { get; }

        private ServiceMetadata(string name, string[] endpoints, string[] mexEndpoints, string wsdlAddress)
        {
            Name = name;
            Endpoints = endpoints;
            MexEndpoints = mexEndpoints;
            WsdlAddress = wsdlAddress;
        }
        public static ServiceMetadata From(Backend.ServiceMetadata other)
        {
            return new ServiceMetadata(
                other.FullName,
                other.Endpoints,
                other.MexEndpoints,
                other.WsdlEndpointAddress);
        }
    }
}
