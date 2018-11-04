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
    .Does(() => {
        Codecov("opencover.xml");
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        if (FileExists("opencover.xml"))
        {
            DeleteFile("opencover.xml");
        }

        var openCoverSettings = new OpenCoverSettings
        {
            OldStyle = true,
            MergeOutput = true,
            MergeByHash = true,
            Register = "user",
            ReturnTargetCodeOffset = 0
        }
        .WithFilter("+[dotNetRDF*]*");

        var testProjects = new Tuple<string, string>[]
        {
            Tuple.Create("unittest.csproj", "-notrait \"Category=explicit\""),
            Tuple.Create("unittest.csproj", "-trait \"Category=fulltext\""),
            Tuple.Create("dotNetRdf.MockServerTests.csproj", "-notrait \"Category=explicit\"")
        };

        bool success = true;
        foreach (var project in testProjects)
        {
            var projectFile = GetFiles($"**\\{project.Item1}").Single();

            try
            {
                openCoverSettings.WorkingDirectory = projectFile.GetDirectory();

                OpenCover(context => {
                    context.DotNetCoreTool(
                        projectPath: projectFile.FullPath,
                        command: "xunit", 
                        arguments: $"-noshadow -configuration {configuration} {project.Item2}");
                },
                "opencover.xml",
                openCoverSettings);
            }
            catch(Exception ex)
            {
                success = false;
                Error("There was an error while running the tests", ex);
            }
        };
 
        if(success == false)
        {
            throw new CakeException("There was an error while running the tests");
        }
    });

RunTarget(target);
