# Beanstalk Seeder

`.NET Core` [global tool][dotnet-global-tools] emulating the `SQS Daemon` surrounding an [`Elastic Beanstalk Worker Tier`][worker-tier] so that you can replicate the interaction between a `Web Tier` and a `Worker Tier` on your machine.

The goal of `Beanstalk Seeder` is to allow you to go through an end-to-end flow, not to replicate the feature set of the `SQS Daemon`.

| Package | Release | Pre-release |
| --- | --- | --- |
| `dotnet-seed-beanstalk` | [![NuGet][nuget-tool-badge]][nuget-tool-command] | [![MyGet][myget-tool-badge]][myget-tool-command] |

| CI | Status | Platform(s) | Framework(s) |
| --- | --- | --- | --- |
| [AppVeyor][app-veyor] | [![Build Status][app-veyor-shield]][app-veyor] | `Windows` | `netcoreapp2.1` |

## Assumptions

- Payload in `JSON`

## Installation

```posh
> dotnet tool install -g dotnet-seed-beanstalk
```

## Usage

```posh
> dotnet seed-beanstalk -q <queue-uri> -w <worker-uri>
```

The tool will then prompt you for an `Access Key` and a `Secret Key`.

[worker-tier]: http://docs.aws.amazon.com/elasticbeanstalk/latest/dg/using-features-managing-env-tiers.html
[available-regions]: http://docs.aws.amazon.com/AWSEC2/latest/UserGuide/using-regions-availability-zones.html#concepts-available-regions
[app-veyor-yml]: appveyor.yml
[app-veyor]: https://ci.appveyor.com/project/GabrielWeyer/beanstalk-seeder
[app-veyor-shield]: https://img.shields.io/appveyor/ci/gabrielweyer/beanstalk-seeder/master.svg?label=AppVeyor&style=flat-square
[dotnet-global-tools]: https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools
[nuget-tool-badge]: https://img.shields.io/nuget/v/dotnet-seed-beanstalk.svg?label=NuGet&style=flat-square
[nuget-tool-command]: https://www.nuget.org/packages/dotnet-seed-beanstalk
[myget-tool-badge]: https://img.shields.io/myget/gabrielweyer-pre-release/v/dotnet-seed-beanstalk.svg?label=MyGet&style=flat-square
[myget-tool-command]: https://www.myget.org/feed/gabrielweyer-pre-release/package/nuget/dotnet-seed-beanstalk
