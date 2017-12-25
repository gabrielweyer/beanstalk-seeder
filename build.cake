#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var assemblyVersion = "0.0.1";
var zipVersion = "0.0.1";

var artefactsDir = MakeAbsolute(Directory("artefacts"));

var solutionPath = "./BeanstalkSeeder.sln";

Task("Clean")
    .Does(() =>
{
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

Task("Build")
    .IsDependentOn("SemVer")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        NoIncremental = true,
        MSBuildSettings = new DotNetCoreMSBuildSettings().SetVersion(assemblyVersion),
        ArgumentCustomization = args => args.Append("--no-restore")
    };

    DotNetCoreBuild(solutionPath, settings);
});

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);