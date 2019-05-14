using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Optional;

namespace ServiceHoster.Controller
{
    public class ServiceRunner : IDisposable
    {
        private readonly Option<string> _pidFile;
        private readonly Option<string> _statusFile;
        private readonly TextWriter _out;
        private readonly TextWriter _err;
        private readonly List<AppDomainHost> _hosts;
        private bool _closing;

        public ServiceRunner(IEnumerable<string> serviceDlls, Option<string> pidFile, Option<string> statusFile, TextWriter @out, TextWriter err)
        {
            _pidFile = pidFile;
            _statusFile = statusFile;
            _out = @out;
            _err = err;

            _hosts = serviceDlls
                .Select(dll => new
                {
                    dll,
                    config = $"{dll}.config"
                }) // assumes the assembly's config file is in the same location, named the same with .config appended
                .Where(h => File.Exists(h.dll) && File.Exists(h.config))
                .Select(h =>
                {
                    _out.WriteLine($"Creating ServiceHost for for {h.dll} ({h.config}).");

                    return new AppDomainHost(h.dll, h.config);
                })
                .ToList();
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _out.WriteLine($"Closing services...");
            foreach (var host in _hosts.AsEnumerable().Reverse())
            {
                host.CloseServices();
                _out.WriteLine($"  Closed {host.HostInfo.AssemblyPath}");
            }
            _out.WriteLine($"Closed services...");
            _closing = true;
        }

        public void Start(AutoResetEvent started)
        {
            PidFile(() => RunServices(started));
        }

        private void PidFile(Action runner)
        {
            try
            {
                _pidFile.Match(
                    filename => File.WriteAllText(filename, Process.GetCurrentProcess().Id.ToString()),
                    () => { });

                runner();
            }
            finally
            {
                _pidFile.Match(
                    filename =>
                    {
                        if (File.Exists(filename))
                            File.Delete(filename);
                    },
                    () => { });
            }
        }

        private void RunServices(AutoResetEvent onServicesStarted)
        {
            foreach (var host in _hosts)
            {
                LogOpeningService(host);
                host.OpenServices();
            }

            _out.WriteLine("Service Endpoints:");
            _hosts
                .SelectMany(h => h.Services)
                .ToList()
                .ForEach(LogServiceEndpoints);

            onServicesStarted.Set();

            _closing = false;
            while (!_closing)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                _statusFile.Match(
                    filename => UpdateStatus(_hosts, filename),
                    () => { });
            }

            void LogOpeningService(AppDomainHost serviceHost)
            {
                _out.WriteLine($"Opening services for ...{serviceHost.HostInfo.AssemblyPath}");
                _out.WriteLine($"  config: {serviceHost.HostInfo.AssemblyConfig}");
                _out.WriteLine($"  assembly version: {serviceHost.HostInfo.AssemblyVersion}");
                _out.WriteLine($"  file version: {serviceHost.HostInfo.FileVersion}");
                _out.WriteLine("  services: {0}",
                    string.Join(", ", serviceHost.Services.Select(h => $"{h.Name}").ToArray()));
            }

            void LogServiceEndpoints(ServiceMetadata serviceInfo)
            {
                _out.WriteLine(serviceInfo.Name);
                _out.WriteLine($"  WSDL: {serviceInfo.WsdlAddress ?? "N/A"}");
                foreach (var endpoint in serviceInfo.MexEndpoints)
                    _out.WriteLine($"  {endpoint}");
                foreach (var endpoint in serviceInfo.Endpoints)
                    _out.WriteLine($"  {endpoint}");
            }
        }

        private static void UpdateStatus(List<AppDomainHost> hosts, string statusFilename)
        {
            var allOpen = hosts.SelectMany(h => h.Status).All(s => s.State == CommunicationState.Opened);

            UpdateStatusFile(
                statusFilename,
                allOpen
                    ? "OK"
                    : string.Join(", ",
                        hosts.SelectMany(h => h.Status).Select(s => $"{s.ServiceName} - {s.State}")));
        }

        private static void UpdateStatusFile(string filename, string status)
        {
            File.WriteAllText(filename, status);
        }
    }
}