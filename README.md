# WCF Service Hoster

## What is it?

Console app for self-hosting WCF services.

Sometimes IIS is too much, and `WcfSvcHost.exe` doesn't fit your needs (i.e. Docker).

## Usage

### CLI

```
ServiceHoster 1.0.5
Copyright c 2018 SEP, Inc

  -p, --pid-file            Location of the PID file. No PID file if not specified.

  -s, --status-file         Location of the STATUS file. No STATUS file if not specified. Contains 'OK' in the nominal
                            case.

  --help                    Display this help screen.

  --version                 Display version information.

  service dll's (pos. 0)    List of service DLL's to host.
```

### Hosting

Single service assembly:

`ServiceHoster.exe FULL_PATH_TO_WCF_SERVICE_DLL`

Multiple service assemblies:

`ServiceHoster.exe FULL_PATH_TO_WCF_SERVICE_DLL FULL_PATH_TO_OTHER_WCF_SERVICE_DLL`

Call the executable with a list of assembly filenames (space separated).

Each `Assembly` will be loaded into it's own `AppDomain` with a _working directory_ of where that assembly is located.

**Note:** It is assumed the **configuration** for the assembly is in the same location, and named the same as the service assembly, with `.config` appended.

### Host Health (PID and STATUS)

If you specify the `--pid-file` option, the `PID` of the process will be written to the specified file.

If you specify the `--status-file` option, the `State` of each hosted service will be inspected and reported into the specified file. If __ALL__ states are `Opened`, `OK` is written to the file. Otherwise all the states will be written.

This may be useful if you are wanting to do a health check of the host process, for example, using Docker's `HEALTHCHECK` instruction.

Here's an example `HEALTHCHECK` instruction for your `Dockerfile`:

```
HEALTHCHECK --interval=10s --timeout=10s --start-period=60s --retries=2 \
  CMD powershell -Command \
      $pidok = (Get-Process -Id (Get-Content PID)).ProcessName -eq "ServiceHoster"; \
      $statusok = (Get-Content STATUS) -eq "OK"; \
      if ($pidok -and $statusok) { [Environment]::Exit(0) } else {  [Environment]::Exit(1) }
```


## Installation

WcfServiceHoster is also available via NuGet:

PM> Install-Package WcfServiceHoster 
Or visit: https://www.nuget.org/packages/WcfServiceHoster/
