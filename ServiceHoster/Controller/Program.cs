using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using CommandLine;

namespace ServiceHoster.Controller
{
    static class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts => PidFile(opts, Run))
                .WithNotParsed(_ => { });
        }

        private static void Run(Options options)
        {
            Console.Title = "WCF Service Hoster";

            var hosts = options.ServiceDlls
                .Select(dll => new{dll, config = $"{dll}.config"}) // assumes the assembly's config file is in the same location, named the same with .config appended
                .Where(h => File.Exists(h.dll) && File.Exists(h.config))
                .Select(h =>
                {
                    Console.Out.WriteLine($"Creating ServiceHost for for {h.dll} ({h.config}).");

                    return new AppDomainHost(h.dll, h.config);
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
                Console.Out.WriteLine(service.Name);
                Console.WriteLine($"  WSDL: {service.WsdlAddress ?? "N/A"}");
                foreach (var endpoint in service.MexEndpoints)
                    Console.WriteLine($"  {endpoint}");
                foreach (var endpoint in service.Endpoints)
                    Console.WriteLine($"  {endpoint}");
            });

            var closing = false;
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.Out.WriteLine($"Closing services...");
                foreach (var host in hosts)
                    host.CloseServices();
                Console.Out.WriteLine($"Closed services...");
                closing = true;
                eventArgs.Cancel = true;
            };
            Console.Out.WriteLine("Press <Ctrl+C> to stop.");

            while (!closing)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                if (options.HasStatus)
                {
                    var allOpen = hosts.SelectMany(h => h.Status).All(s => s.State == CommunicationState.Opened);

                    UpdateStatus(
                        options.StatusFile,
                        allOpen ? "OK" : string.Join(", ", hosts.SelectMany(h => h.Status).Select(s => $"{s.ServiceName} - {s.State}")));
                }
            }
        }

        private static void UpdateStatus(string filename, string status)
        {
            File.WriteAllText(filename, status);
        }

        private static void PidFile(Options options, Action<Options> runner)
        {
            try
            {
                if (options.HasPidFile)
                {
                    File.WriteAllText(options.PidFile, Process.GetCurrentProcess().Id.ToString());
                }

                runner(options);
            }
            finally
            {
                if (options.HasPidFile && File.Exists(options.PidFile))
                {
                    File.Delete(options.PidFile);
                }
            }
        }

        private class Options
        {
            [Option('p', "pid-file", HelpText = "Location of the PID file. No PID file if not specified.")]
            public string PidFile { get; set; }

            [Option('s', "status-file", HelpText = "Location of the STATUS file. No STATUS file if not specified. Contains 'OK' in the nominal case.")]
            public string StatusFile { get; set; }

            [Value(0, Min = 1, MetaName = "service dll's", HelpText = "List of service DLL's to host.")]
            public IEnumerable<string> ServiceDlls { get; set; }

            public bool HasPidFile
            {
                get { return PidFile != null; }
            }
            public bool HasStatus
            {
                get { return StatusFile != null; }
            }
        }
    }
}