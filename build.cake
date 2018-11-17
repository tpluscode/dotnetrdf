#tool nuget:?package=codecov
#tool nuget:?package=gitlink
#tool nuget:?package=GitVersion.CommandLine&prerelease
#addin nuget:?package=Cake.Codecov
#tool "nuget:?package=Microsoft.TestPlatform&version=15.7.0"
#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=JetBrains.dotCover.CommandLineTools"
#tool "nuget:?package=ReportGenerator"

var target = Argument("target", "Build");
var configuration = Argument("Configuration", "Debug");

GitVersion version;

var libraryProjects = GetFiles("./Libraries/**/*.csproj");

Task("Pack")
    .IsDependentOn("Build")
    .DoesForEach(libraryProjects, path => {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = "./nugets/",
            NoBuild = true,
            MSBuildSettings = new DotNetCoreMSBuildSettings()
        };

        settings.MSBuildSettings.Properties["version"] = new [] { version.NuGetVersion };

        DotNetCorePack(path.FullPath, settings);
    });

Task("GitVersion")
    .WithCriteria(BuildSystem.IsLocalBuild)
    .Does(() => {
        version = GitVersion(new GitVersionSettings {
            UpdateAssemblyInfo = true,
        });

        if (BuildSystem.IsLocalBuild == false) 
        {
            GitVersion(new GitVersionSettings {
                OutputType = GitVersionOutput.BuildServer
            });
        }
    });

Task("Build")
    .IsDependentOn("GitVersion")
    .Does(() => {
        DotNetCoreBuild("dotnetrdf.sln", new DotNetCoreBuildSettings {
            Configuration = configuration
        });
    });

Task("Codecov")
    .Does(() => {
        Codecov("coverage\\cobertura.xml");
    });

Task("TestNet452")
    .DoesForEach(GetTests("net452"), testRun => {
        DotNetCoreTool(
            projectPath: (string)testRun.projectFile.FullPath,
            command: "xunit", 
            arguments: (string)testRun.arguments);
    })
    .ContinueOnError();

Task("TestNetCore20")
    .DoesForEach(GetTests("netcoreapp2.0"), testRun => {
        DotNetCoreTool(
            projectPath: (string)testRun.projectFile.FullPath,
            command: "xunit", 
            arguments: (string)testRun.arguments);
    })
    .ContinueOnError();

Task("TestNetCore11")
    .DoesForEach(GetTests("netcoreapp1.1"), testRun => {
        DotNetCoreTool(
            projectPath: (string)testRun.projectFile.FullPath,
            command: "xunit", 
            arguments: (string)testRun.arguments);
    })
    .ContinueOnError();

Task("Test")
    .IsDependentOn("TestNet452")
    .IsDependentOn("TestNetCore20")
    .IsDependentOn("TestNetCore11")
    .Does(() => { });

Task("Cover")
    .IsDependentOn("TestNetCore20")
    .IsDependentOn("TestNetCore11")
    .Does(() => {
        if (DirectoryExists("coverage"))
            CleanDirectories("coverage");
    })
    .Does(() => {        
        foreach(var testRun in GetTests("net452"))
        {           
            var xunitSettings = new XUnit2Settings
                  {
                     ShadowCopy = false
                  }
                  .ExcludeTrait("Category", new [] { "Explicit" });

            DotCoverAnalyse(context => {
                context.XUnit2(
                  $@"Testing\unittest\bin\Debug\net452\dotNetRDF.Test.dll",
                  xunitSettings);
            },
            "./dotcover.xml",
            new DotCoverAnalyseSettings {
                ReportType = DotCoverReportType.DetailedXML,
            }
            .WithFilter("-xunit*")
            .WithFilter("-dotnetrdf.test")
            .WithFilter("-dotNetRDF.MockServerTests"));
        }
    })
    .Does(() => {
        StartProcess(
          @".\tools\ReportGenerator.4.0.4\tools\net47\ReportGenerator.exe",
          @" -reports:.\dotcover.xml -targetdir:.\coverage -reporttypes:Cobertura -assemblyfilters:-xunit*;-dotNetRDF.Test");
    })
    .DeferOnError();

public IEnumerable<dynamic> GetTests(params string[] frameworks) 
{
    var testProjects = new dynamic[]
    {
        new { name = "unittest.csproj", arguments = "-trait Category=fulltext" },
        new { name = "unittest.csproj", arguments = "-notrait Category=explicit" },
        new { name = "dotNetRdf.MockServerTests.csproj", arguments = "-notrait Category=explicit" }
    };

    foreach (var project in testProjects)
    {
        foreach(var framework in frameworks)
        {
            var arguments = $"-noshadow -configuration {configuration} {project.arguments} -framework {framework}";
            var projectFile = GetFiles($"**\\{project.name}").Single();

            yield return new { projectFile, arguments };
        }
    }
}

RunTarget(target);
