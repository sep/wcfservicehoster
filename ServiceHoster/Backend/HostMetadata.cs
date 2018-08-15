using System;

namespace ServiceHoster.Backend
{
    [Serializable]
    public class HostMetadata
    {
        public string FileVersion { get; set; }
        public string AssemblyVersion { get; set; }
    }
}