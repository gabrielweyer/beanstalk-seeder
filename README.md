# Beanstalk Seeder

Emulates the `SQS Daemon` surrounding an [`Elastic Beanstalk Worker Tier`][worker-tier] so that you can replicate the interaction between a `Web Tier` and a `Worker Tier` on your machine.

The goal of `Beanstalk Seeder` is to allow you to go through an end-to-end flow, not to replicate the feature set of the `SQS Daemon`.

## Assumptions

- Payload in `JSON`

## Configuration

You'll need to configure those three settings, either in `appsettings.json` or via environment variables:

- `Worker:Endpoint` - accessible `URI`, for example `http://localhost:9999`
- `Aws:RegionSystemName` - [region code][available-regions], for example `ap-southeast-2`
- `Aws:Queue:WorkerQueueUrl` - `URL` of the `SQS` queue, for example `https://sqs.ap-southeast-2.amazonaws.com/375985941080/dev-gabriel`

Create a `iAM` user (if you don't have one already) which has access to `SQS`. Then create two environment variables:

- `AWS_ACCESS_KEY_ID` - this is the `Access key ID`
- `AWS_SECRET_ACCESS_KEY` - this is the `Secret access key`

## AppVeyor

The `AppVeyor` [script][app-veyor] should contain enough comments to explain how the build works in `AppVeyor`. This is the entire configuration, I didn't need to configure anything in the UI.

[worker-tier]: http://docs.aws.amazon.com/elasticbeanstalk/latest/dg/using-features-managing-env-tiers.html
[available-regions]: http://docs.aws.amazon.com/AWSEC2/latest/UserGuide/using-regions-availability-zones.html#concepts-available-regions
[app-veyor]: appveyor.yml
