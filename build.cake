#tool nuget:?package=OpenCover
#tool nuget:?package=codecov
#tool nuget:?package=gitlink
#tool nuget:?package=GitVersion.CommandLine&prerelease
#addin nuget:?package=Cake.Codecov

var target = Argument("target", "Build");
var configuration = Argument("Configuration", "Debug");

GitVersion version;

var libraryProjects = GetFiles("./Libraries/**/*.csproj");

Task("CI")
    .IsDependentOn("Pack")
    .IsDependentOn("Codecov").Does(() => {});

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
    .Does(() => {
        Codecov("opencover.xml");
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        var openCoverSettings = new OpenCoverSettings
        {
            OldStyle = true,
            MergeOutput = true,
            MergeByHash = true,
            Register = "user",
            ReturnTargetCodeOffset = 0
        }
        .WithFilter("+[VDS.RDF]*");

        var testProjects = new Dictionary<string, string[]> 
        {
            { "unittest.csproj", new [] { "-notrait \"Category=explicit\"", "-trait \"Category=fulltext\"" } },
            { "dotNetRdf.MockServerTests.csproj", new [] { "-notrait \"Category=explicit\"" } },
        };

        bool success = true;
        foreach (var kvp in testProjects)
        {
            var projectFile = GetFiles($"**\\{kvp.Key}").Single();

            foreach (var args in testProjects[kvp.Key])
            {
                try
                {
                    openCoverSettings.WorkingDirectory = projectFile.GetDirectory();

                    OpenCover(context => {
                        context.DotNetCoreTool(
                            projectPath: projectFile.FullPath,
                            command: "xunit", 
                            arguments: $"-noshadow -configuration {configuration} {args}");
                    },
                    "opencover.xml",
                    openCoverSettings);
                }
                catch(Exception ex)
                {
                    success = false;
                    Error("There was an error while running the tests", ex);
                }
            }
        }
 
        if(success == false)
        {
            throw new CakeException("There was an error while running the tests");
        }
    });

RunTarget(target);
