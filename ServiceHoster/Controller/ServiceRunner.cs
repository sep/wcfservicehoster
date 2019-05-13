using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace ServiceHoster.Controller
{
    public class ServiceRunner : IDisposable
    {
        private readonly Options _options;
        private readonly TextWriter _out;
        private readonly TextWriter _err;
        private readonly List<AppDomainHost> _hosts;
        private bool _closing;

        public ServiceRunner(Options options, TextWriter @out, TextWriter err)
        {
            _options = options;
            _out = @out;
            _err = err;

            _hosts = _options
                .ServiceDlls
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
                if (_options.HasPidFile)
                {
                    File.WriteAllText(_options.PidFile, Process.GetCurrentProcess().Id.ToString());
                }

                runner();
            }
            finally
            {
                if (_options.HasPidFile && File.Exists(_options.PidFile))
                {
                    File.Delete(_options.PidFile);
                }
            }
        }

        private void RunServices(AutoResetEvent onServicesStarted)
        {
            foreach (var host in _hosts)
            {
                _out.WriteLine($"Opening services for ...{host.HostInfo.AssemblyPath}");
                _out.WriteLine($"  config: {host.HostInfo.AssemblyConfig}");
                _out.WriteLine($"  assembly version: {host.HostInfo.AssemblyVersion}");
                _out.WriteLine($"  file version: {host.HostInfo.FileVersion}");
                _out.WriteLine("  services: {0}",
                    string.Join(", ", host.Services.Select(h => $"{h.Name}").ToArray()));
                host.OpenServices();
            }

            _out.WriteLine("Service Endpoints:");
            _hosts.SelectMany(h => h.Services).ToList().ForEach(service =>
            {
                _out.WriteLine(service.Name);
                _out.WriteLine($"  WSDL: {service.WsdlAddress ?? "N/A"}");
                foreach (var endpoint in service.MexEndpoints)
                    _out.WriteLine($"  {endpoint}");
                foreach (var endpoint in service.Endpoints)
                    _out.WriteLine($"  {endpoint}");
            });
            onServicesStarted.Set();

            _closing = false;
            while (!_closing)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                if (_options.HasStatus)
                {
                    var allOpen = _hosts.SelectMany(h => h.Status).All(s => s.State == CommunicationState.Opened);

                    UpdateStatus(
                        _options.StatusFile,
                        allOpen
                            ? "OK"
                            : string.Join(", ",
                                _hosts.SelectMany(h => h.Status).Select(s => $"{s.ServiceName} - {s.State}")));
                }
            }
        }

        private static void UpdateStatus(string filename, string status)
        {
            File.WriteAllText(filename, status);
        }
    }
}