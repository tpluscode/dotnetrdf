#tool nuget:?package=OpenCover
#tool nuget:?package=codecov
#tool nuget:?package=gitlink
#tool nuget:?package=GitVersion.CommandLine&prerelease
#addin nuget:?package=Cake.Codecov

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
    .IsDependentOn("Test")
    .IsDependentOn("Cover")
    .Does(() => {
        Codecov("opencover.xml");
    });

Task("Test")
    .DoesForEach(GetTests(), testRun => {

        DotNetCoreTool(
            projectPath: (string)testRun.projectFile.FullPath,
            command: "xunit", 
            arguments: (string)testRun.arguments);

    }).DeferOnError();

Task("Cover")
    .Does(() => {
        if (FileExists("opencover.xml"))
        {
            DeleteFile("opencover.xml");
        }

        var openCoverSettings = new OpenCoverSettings
        {
            MergeOutput = true,
            MergeByHash = true,
            Register = "user",
            ReturnTargetCodeOffset = 0,
            SkipAutoProps = true
        }
        .WithFilter("+[dotNetRDF*]*");
        
        foreach(var testRun in GetTests("net452"))
        {           
            openCoverSettings.WorkingDirectory = testRun.projectFile.GetDirectory();

            OpenCover(context => {
                context.DotNetCoreTool(
                    projectPath: (string)testRun.projectFile.FullPath,
                    command: "xunit", 
                    arguments: (string)testRun.arguments);
            },
            "opencover.xml",
            openCoverSettings);
        }
 
    });

public IEnumerable<dynamic> GetTests(string framework = null) 
{
    var testProjects = new dynamic[]
    {
        new { name = "unittest.csproj", arguments = "-trait Category=fulltext" },
        new { name = "unittest.csproj", arguments = "-notrait Category=explicit" },
        new { name = "dotNetRdf.MockServerTests.csproj", arguments = "-notrait Category=explicit" }
    };

    foreach (var project in testProjects)
    {
        var arguments = $"-noshadow -configuration {configuration} {project.arguments}";
        var projectFile = GetFiles($"**\\{project.name}").Single();

        if (framework != null)
        {
            arguments += $" -framework {framework}";
        }
         
        yield return new { projectFile, arguments };
    }
}

RunTarget(target);
