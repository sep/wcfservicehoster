# WCF Service Hoster

## What is it?

Console app for self-hosting WCF services.

Sometimes IIS is too much, and `WcfSvcHost.exe` doesn't fit your needs (i.e. Docker).

## Usage

Single service assembly:

`ServiceHoster.exe FULL_PATH_TO_WCF_SERVICE_DLL`

Multiple service assemblies:

`ServiceHoster.exe FULL_PATH_TO_WCF_SERVICE_DLL FULL_PATH_TO_OTHER_WCF_SERVICE_DLL`

Call the executable with a list of assembly filenames (space separated).

Each `Assembly` will be loaded into it's own `AppDomain` with a _working directory_ of where that assembly is located.

**Note:** It is assumed the **configuration** for the assembly is in the same location, and named the same as the service assembly, with `.config` appended.

## Installation

WcfServiceHoster is also available via NuGet:

PM> Install-Package WcfServiceHoster 
Or visit: https://www.nuget.org/packages/WcfServiceHoster/
