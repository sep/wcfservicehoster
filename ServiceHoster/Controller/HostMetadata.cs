namespace ServiceHoster.Controller
{
    public class HostMetadata
    {
        public string FileVersion { get; private set; }
        public string AssemblyVersion { get; private set; }
        public string AssemblyPath { get; private set; }
        public string AssemblyConfig { get; private set; }

        private HostMetadata()
        {
        }

        public static HostMetadata From(Backend.HostMetadata other, string assemblyPath, string assemblyConfig)
        {
            return new HostMetadata
            {
                AssemblyPath = assemblyPath,
                AssemblyConfig = assemblyConfig,
                AssemblyVersion = other.AssemblyVersion,
                FileVersion = other.FileVersion,
            };
        }
    }
}