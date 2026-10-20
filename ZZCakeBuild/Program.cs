using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace CakeBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .Run(args);
        }
    }

    /// <summary>
    /// One mod of the repository: its project folder and modinfo
    /// </summary>
    public record ModProject(string ProjectName, string Name, string Version);

    public class BuildContext : FrostingContext
    {
        // The core mod and its add-ons, every one is packaged into its own zip
        public static readonly string[] ProjectNames = ["GiMap", "GiMap.Underground"];

        public string BuildConfiguration { get; }
        public bool SkipJsonValidation { get; }
        public IReadOnlyList<ModProject> Projects { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            BuildConfiguration = context.Argument("configuration", "Release");
            SkipJsonValidation = context.Argument("skipJsonValidation", false);
            Projects = ProjectNames
                .Select(projectName =>
                {
                    var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{projectName}/modinfo.json");
                    return new ModProject(projectName, modInfo.ModID, modInfo.Version);
                })
                .ToList();
        }
    }

    [TaskName("ValidateJson")]
    public sealed class ValidateJsonTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (context.SkipJsonValidation)
            {
                return;
            }
            foreach (var project in context.Projects)
            {
                var jsonFiles = context.GetFiles($"../{project.ProjectName}/assets/**/*.json");
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file.FullPath);
                        JToken.Parse(json);
                    }
                    catch (JsonException ex)
                    {
                        throw new Exception($"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
                    }
                }
            }
        }
    }

    [TaskName("Build")]
    [IsDependentOn(typeof(ValidateJsonTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            foreach (var project in context.Projects)
            {
                context.DotNetClean($"../{project.ProjectName}/{project.ProjectName}.csproj",
                    new DotNetCleanSettings
                    {
                        Configuration = context.BuildConfiguration
                    });


                context.DotNetPublish($"../{project.ProjectName}/{project.ProjectName}.csproj",
                    new DotNetPublishSettings
                    {
                        Configuration = context.BuildConfiguration
                    });
            }
        }
    }

    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists("../Releases");
            context.CleanDirectory("../Releases");
            foreach (var project in context.Projects)
            {
                var source = $"../{project.ProjectName}";
                var target = $"../Releases/{project.Name}";
                context.EnsureDirectoryExists(target);
                context.CopyFiles($"{source}/bin/{context.BuildConfiguration}/Mods/mod/publish/*", target);
                if (context.DirectoryExists($"{source}/assets"))
                {
                    context.CopyDirectory($"{source}/assets", $"{target}/assets");
                }
                context.CopyFile($"{source}/modinfo.json", $"{target}/modinfo.json");
                if (context.FileExists($"{source}/modicon.png"))
                {
                    context.CopyFile($"{source}/modicon.png", $"{target}/modicon.png");
                }
                context.Zip(target, $"../Releases/{project.Name}_{project.Version}.zip");
            }
        }
    }

    [TaskName("Default")]
    [IsDependentOn(typeof(PackageTask))]
    public class DefaultTask : FrostingTask
    {
    }
}
