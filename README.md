# Beanstalk Seeder

| CI | Status | Platform(s) | Framework(s) | Test Framework(s) |
| --- | --- | --- | --- | --- |
| [AppVeyor][app-veyor] | [![Build Status][app-veyor-shield]][app-veyor] | `Windows` | `netcoreapp2.1` | `netcoreapp2.1` |

Emulates the `SQS Daemon` surrounding an [`Elastic Beanstalk Worker Tier`][worker-tier] so that you can replicate the interaction between a `Web Tier` and a `Worker Tier` on your machine.

The goal of `Beanstalk Seeder` is to allow you to go through an end-to-end flow, not to replicate the feature set of the `SQS Daemon`.

## Assumptions

- Payload in `JSON`

## Configuration

You'll need to configure those two settings, either in `appsettings.json` or via environment variables:

- `Worker:Endpoint` - accessible `URI`, for example `http://localhost:9999`
- `Aws:Queue:WorkerQueueUrl` - `URL` of the `SQS` queue, for example `https://sqs.ap-southeast-2.amazonaws.com/375985941080/dev-gabriel`

Create a `iAM` user (if you don't have one already) which has access to `SQS`. Then create two environment variables:

- `AWS_ACCESS_KEY_ID` - this is the `Access key ID`
- `AWS_SECRET_ACCESS_KEY` - this is the `Secret access key`

## Run

1. If you don't have the [.NET Core Runtime v2.1.300][dotnet-runtime], you'll need to dowload it and install it
1. Download the binaries from the [releases][releases]
1. In the directory where you extracted the archive:

```posh
dotnet .\BeanstalkSeeder.dll
```

## AppVeyor

The `AppVeyor` [script][app-veyor-yml] should contain enough comments to explain how the build works in `AppVeyor`. This is the entire configuration, I didn't need to configure anything in the UI.

[worker-tier]: http://docs.aws.amazon.com/elasticbeanstalk/latest/dg/using-features-managing-env-tiers.html
[available-regions]: http://docs.aws.amazon.com/AWSEC2/latest/UserGuide/using-regions-availability-zones.html#concepts-available-regions
[app-veyor-yml]: appveyor.yml
[app-veyor]: https://ci.appveyor.com/project/GabrielWeyer/beanstalk-seeder
[app-veyor-shield]: https://ci.appveyor.com/api/projects/status/github/gabrielweyer/beanstalk-seeder?branch=master&svg=true
[releases]: https://github.com/gabrielweyer/beanstalk-seeder/releases
[dotnet-runtime]: https://www.microsoft.com/net/download/windows
