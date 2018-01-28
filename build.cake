#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var assemblyVersion = "0.0.1";
var zipVersion = "0.0.1";

var publishDir = MakeAbsolute(Directory("publish"));
var artefactsDir = MakeAbsolute(Directory("artefacts"));
var testsResultsDir = artefactsDir.Combine("test-results");
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
        AppVeyor.UpdateBuildVersion(zipVersion);
    });

Task("Build")
    .IsDependentOn("SetAppVeyorVersion")
    .Does(() =>
    {
        var settings = new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            NoIncremental = true,
            NoRestore = true,
            MSBuildSettings = new DotNetCoreMSBuildSettings()
                .SetVersion(assemblyVersion)
                .WithProperty("FileVersion", zipVersion)
                .WithProperty("InformationalVersion", zipVersion)
                .WithProperty("nowarn", "7035")
        };

        DotNetCoreBuild(solutionPath, settings);
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var settings = new DotNetCoreToolSettings();

        var argumentsBuilder = new ProcessArgumentBuilder()
            .Append("-configuration")
            .Append(configuration)
            .Append("-nobuild");

        var projectFiles = GetFiles("./tests/*/*Tests.csproj");

        foreach (var projectFile in projectFiles)
        {
            var testResultsFile = testsResultsDir.Combine($"{projectFile.GetFilenameWithoutExtension()}.xml");
            var arguments = $"{argumentsBuilder.Render()} -xml \"{testResultsFile}\"";

            DotNetCoreTool(projectFile, "xunit", arguments, settings);
        }
    });

Task("PublishLocal")
    .IsDependentOn("Test")
    .WithCriteria(() => HasArgument("publish"))
    .Does(() =>
{
    var publishPath = publishDir.FullPath + '/';

    var settings = new DotNetCoreMSBuildSettings()
        .HideLogo()
        .SetMaxCpuCount(-1)
        .WithTarget("PublishWithoutBuilding")
        .SetConfiguration(configuration)
        .WithProperty("PublishDir", publishPath);

    DotNetCoreMSBuild("./src/BeanstalkSeeder/BeanstalkSeeder.csproj", settings);

    Information($"Published to '{publishPath}'");
});

Task("Zip")
    .IsDependentOn("PublishLocal")
    .WithCriteria(() => HasArgument("publish"))
    .Does(() =>
{
    artefactFilePath = artefactsDir.GetFilePath(new FilePath($"beanstalk-seeder-{zipVersion}.zip"));
    Zip(publishDir, artefactFilePath);

    Information($"Zipped content of directory '{publishDir}' to archive '{artefactFilePath}'");
});

Task("PublishAppVeyor")
    .IsDependentOn("Zip")
    .WithCriteria(() => HasArgument("publish") && AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    CopyFileToDirectory(artefactFilePath, MakeAbsolute(Directory("./")));

    GetFiles($"./*.zip")
            .ToList()
            .ForEach(f => AppVeyor.UploadArtifact(f, new AppVeyorUploadArtifactsSettings { DeploymentName = "archive" }));
});

Task("Default")
    .IsDependentOn("PublishAppVeyor");

RunTarget(target);