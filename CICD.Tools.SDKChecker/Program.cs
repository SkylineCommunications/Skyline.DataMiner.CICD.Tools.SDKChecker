namespace Skyline.DataMiner.CICD.Tools.SDKChecker
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Build.Locator;
    using Microsoft.CodeAnalysis.MSBuild;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.Tools.Reporter;

    /// <summary>
    /// Checks what projects are Legacy style or SDK Style.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// This tool will check if the projects in the solution are all SDK style.
        /// If any legacy style projects are found, they are logged into the console.
        /// </summary>
        /// <param name="args">Extra arguments.</param>
        /// <returns>0 if successful.</returns>
		public static async Task<int> Main(string[] args)
        {
            var workspaceOption = new Option<string>(
                name: "--workspace",
                description: "Folder location containing the solution.")
            {
                IsRequired = true
            };

            var repoSourceOption = new Option<string>(
            name: "--repositoryName",
            description: "The name of the repository, when provided a call will be made to devopsmetrics.skyline.be.")
            {
                IsRequired = false
            };
            
            var repoBranchOption = new Option<string>(
            name: "--repositoryBranch",
            description: "The branch of the repository, when provided a call will be made to devopsmetrics.skyline.be.")
            {
                IsRequired = false
            };

            var rootCommand = new RootCommand("Returns any project not using SDK Style.")
            {
                workspaceOption,
                repoSourceOption,
                repoBranchOption,
            };

            rootCommand.SetHandler(Process, workspaceOption, repoSourceOption, repoBranchOption);

            await rootCommand.InvokeAsync(args);

            return 0;
        }

        /// <summary>
        /// Retrieves all projects not using SDK style.
        /// </summary>
        /// <param name="pathToSolution">Directory containing the .sln</param>
        /// <returns>A collection of project names.</returns>
        private static ISet<string> RetrieveLegacyStyleProjects(string pathToSolution)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            HashSet<string> projectsWithLegacyStyle = new HashSet<string>();
            foreach (var projectFile in GetProjects(pathToSolution))
            {
                var projectFileProcessor = new ProjectFile(projectFile);
                if (!projectFileProcessor.UsesSDKStyle())
                {
                    projectsWithLegacyStyle.Add(Path.GetFileNameWithoutExtension(projectFile));
                }
            }

            return projectsWithLegacyStyle;
        }

        private static IList<string> GetProjects(string solutionFilePath)
        {
            List<string> projects = new();

            var workspace = MSBuildWorkspace.Create();

            var solution = Task.Run(() => workspace.OpenSolutionAsync(solutionFilePath)).GetAwaiter().GetResult();

            foreach (var project in solution.Projects)
            {
                projects.Add(project.FilePath);
            }

            return projects;
        }

        private static async Task Process(string workspace, string repoName, string branch)
        {
            DevOpsMetrics metrics = new DevOpsMetrics();
            var pathToSolution = FileSystem.Instance.Directory.EnumerateFiles(workspace, "*.sln", SearchOption.AllDirectories).FirstOrDefault();
            if (String.IsNullOrWhiteSpace(pathToSolution))
            {
                throw new InvalidOperationException("Could not located a solution file (.sln) in workspace: " + workspace);
            }

            var projectsWithPackageConfig = RetrieveLegacyStyleProjects(pathToSolution);

            string output = String.Join("#", projectsWithPackageConfig);

            if (!String.IsNullOrWhiteSpace(output))
            {
                try
                {
                    if (!String.IsNullOrWhiteSpace(repoName) && !String.IsNullOrWhiteSpace(branch))
                    {
                        await metrics.ReportAsync($"Skyline.DataMiner.CICD.Tools.SDKChecker|Legacy|repoName:{repoName}|repoBranch:{branch}");
                    }
                }
                catch
                {
                    // gobble up any issues. Not important for end-user.
                }

                Console.Write(output);
            }
            else
            {
                try
                {
                    if (!String.IsNullOrWhiteSpace(repoName) && !String.IsNullOrWhiteSpace(branch))
                    {
                        await metrics.ReportAsync($"Skyline.DataMiner.CICD.Tools.SDKChecker|SDK|repoName:{repoName}|repoBranch:{branch}");
                    }
                }
                catch
                {
                    // gobble up any issues. Not important for end-user.
                }
            }
        }
    }
}