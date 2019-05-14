using System;
using System.Collections.Generic;
using System.Threading;
using CommandLine;
using Optional;

namespace ServiceHoster.Controller
{
    static class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run)
                .WithNotParsed(_ => { });
        }

        private static void Run(Options options)
        {
            var runner = new ServiceRunner(options.ServiceDlls, options.PidFileOption, options.StatusFileOption, Console.Out, Console.Error);
            runner.Start(new AutoResetEvent(false));

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                runner.Stop();
                eventArgs.Cancel = true;
            };
            Console.Out.WriteLine("Press <Ctrl+C> to stop.");
        }
    }

    public class Options
    {
        [Option('p', "pid-file", HelpText = "Location of the PID file. No PID file if not specified.")]
        public string PidFile { get; set; }

        [Option('s', "status-file", HelpText = "Location of the STATUS file. No STATUS file if not specified. Contains 'OK' in the nominal case.")]
        public string StatusFile { get; set; }

        [Value(0, Min = 1, MetaName = "service dll's", HelpText = "List of service DLL's to host.")]
        public IEnumerable<string> ServiceDlls { get; set; }

        public bool HasPidFile => PidFile != null;
        public bool HasStatus => StatusFile != null;

        public Option<string> PidFileOption => HasPidFile ? Option.Some(PidFile) : Option.None<string>();
        public Option<string> StatusFileOption => HasStatus ? Option.Some(StatusFile) : Option.None<string>();
    }
}