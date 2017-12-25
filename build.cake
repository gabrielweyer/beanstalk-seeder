#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var assemblyVersion = "0.0.1";
var zipVersion = "0.0.1";

var publishDir = MakeAbsolute(Directory("publish"));
var artefactsDir = MakeAbsolute(Directory("artefacts"));
FilePath artefactFilePath;

var solutionPath = "./BeanstalkSeeder.sln";

Task("Clean")
    .Does(() =>
{
    CleanDirectory(publishDir);
    CleanDirectory(artefactsDir);

    var settings = new DotNetCoreCleanSettings
    {
        Configuration = configuration
    };

    DotNetCoreClean(solutionPath, settings);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore();
});

Task("SemVer")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var gitVersion = GitVersion();
    assemblyVersion = gitVersion.AssemblySemVer;
    zipVersion = gitVersion.NuGetVersion;

    Information($"AssemblySemVer: {assemblyVersion}");
    Information($"Zip version: {zipVersion}");
});

Task("SetAppVeyorVersion")
    .IsDependentOn("Semver")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    SetAppVeyorVariable("RELEASE_VERSION", zipVersion);
});

Task("Build")
    .IsDependentOn("SetAppVeyorVersion")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        NoIncremental = true,
        MSBuildSettings = new DotNetCoreMSBuildSettings()
            .SetVersion(assemblyVersion)
            .WithProperty("FileVersion", zipVersion)
            .WithProperty("InformationalVersion", zipVersion)
            .WithProperty("nowarn", "7035"),
        ArgumentCustomization = args => args.Append("--no-restore")
    };

    DotNetCoreBuild(solutionPath, settings);
});

Task("PublishLocal")
    .IsDependentOn("Build")
    .WithCriteria(() => HasArgument("publish"))
    .Does(() =>
{
    var settings = new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir,
        ArgumentCustomization = args => args.Append("--no-restore", "--no-build")
    };

    DotNetCorePublish("./src/BeanstalkSeeder/BeanstalkSeeder.csproj", settings);
});

Task("Zip")
    .IsDependentOn("PublishLocal")
    .WithCriteria(() => HasArgument("publish"))
    .Does(() =>
{
    artefactFilePath = artefactsDir.GetFilePath(new FilePath($"beanstalk-seeder-{zipVersion}.zip"));
    Zip(publishDir, artefactFilePath);
});

Task("PublishAppVeyor")
    .IsDependentOn("Zip")
    .WithCriteria(() => HasArgument("publish") && AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    CopyFileToDirectory(artefactFilePath, MakeAbsolute(Directory("./")));
    PublishAppVeyorArtefact(artefactFilePath.GetFilename().ToString());
});

Task("Default")
    .IsDependentOn("PublishAppVeyor");

RunTarget(target);

private void PublishAppVeyorArtefact(string fileName)
{
    StartProcess("appveyor", new ProcessSettings {
        Arguments = new ProcessArgumentBuilder()
            .Append("PushArtifact")
            .AppendQuoted(fileName)
            .Append("-DeploymentName")
            .AppendQuoted("archive")
        }
    );
}

private void SetAppVeyorVariable(string name, string value)
{
    StartProcess("appveyor", new ProcessSettings {
        Arguments = new ProcessArgumentBuilder()
            .Append("SetVariable")
            .Append("-Name")
            .AppendQuoted(name)
            .Append("-Value")
            .AppendQuoted(value)
        }
    );
}