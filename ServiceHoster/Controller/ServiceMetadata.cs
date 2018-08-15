namespace ServiceHoster.Controller
{
    public class ServiceMetadata
    {
        public string Name { get; }
        public string[] Endpoints { get; }
        public string[] MexEndpoints { get; }
        public string WsdlAddress { get; }
        public string AssemblyVersion { get; }
        public string FileVersion { get; }

        private ServiceMetadata(string name, string assemblyVersion, string fileVersion, string[] endpoints, string[] mexEndpoints,
            string wsdlAddress)
        {
            Name = name;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
            Endpoints = endpoints;
            MexEndpoints = mexEndpoints;
            WsdlAddress = wsdlAddress;
        }

        public static ServiceMetadata From(Backend.ServiceMetadata other)
        {
            return new ServiceMetadata(
                other.FullName,
                other.AssemblyVersion,
                other.FileVersion,
                other.Endpoints,
                other.MexEndpoints,
                other.WsdlEndpointAddress);
        }
    }
}
