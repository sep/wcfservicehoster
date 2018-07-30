using System;
using System.Linq;

namespace ServiceHoster.Controller
{
    static class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "WCF Service Hoster";

            // assumes the assembly's config file is in the same location, named the same with .config appended
            var hosts = (args ?? new string[0])
                .Select(dll =>
                {
                    var config = dll + ".config";
                    Console.Out.WriteLine($"Creating ServiceHost for for {dll} ({config}).");

                    return new AppDomainHost(dll, dll + ".config");
                })
                .ToList();

            Console.Out.WriteLine(
                "opening...{0}",
                string.Join(Environment.NewLine, hosts.SelectMany(h => h.Services).Select(h => h.Name).ToArray()));
            foreach (var host in hosts)
                host.OpenServices();


            Console.Out.WriteLine("Service Endpoints:");
            hosts.SelectMany(h => h.Services).ToList().ForEach(service =>
            {
                Console.Out.WriteLine(service);
                Console.WriteLine($"  WSDL: {service.WsdlAddress ?? "N/A"}");
                foreach (var endpoint in service.MexEndpoints)
                    Console.WriteLine($"  {endpoint}");
                foreach (var endpoint in service.Endpoints)
                    Console.WriteLine($"  {endpoint}");
            });

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.Out.WriteLine($"Closing services...");
                foreach (var host in hosts)
                    host.CloseServices();
                Console.Out.WriteLine($"Closed services...");
            };
            Console.Out.WriteLine("Press <Ctrl+C> to stop.");
            Console.ReadLine();
        }
    }
}