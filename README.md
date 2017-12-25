# Beanstalk Seeder

Emulates the `SQS Daemon` surrounding an [`Elastic Beanstalk Worker Tier`][worker-tier] so that you can replicate the interaction between a `Web Tier` and a `Worker Tier` on your machine.

The goal of `Beanstalk Seeder` is to allow you to go through an end-to-end flow, not to replicate the feature set of the `SQS Daemon`.

## Assumptions

- Payload in `JSON`

## Configuration

- `AWS_SECRET_ACCESS_KEY`
- `AWS_ACCESS_KEY_ID`

[worker-tier]: http://docs.aws.amazon.com/elasticbeanstalk/latest/dg/using-features-managing-env-tiers.html