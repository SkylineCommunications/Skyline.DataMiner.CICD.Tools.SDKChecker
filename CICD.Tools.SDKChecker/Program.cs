namespace Skyline.DataMiner.CICD.Tools.SDKChecker
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.CICD.Parsers.Common.VisualStudio;
    using Skyline.DataMiner.CICD.Parsers.Common.VisualStudio.Projects;
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

            var solutionFilePath = new Option<string>(
                name: "--solution-filepath",
                description: "The filepath to the solution.")
            {
                IsRequired = false
            };
            solutionFilePath.LegalFilePathsOnly();

            var rootCommand = new RootCommand("Returns any project not using SDK Style.")
            {
                workspaceOption,
                repoSourceOption,
                repoBranchOption,
                solutionFilePath
            };

            rootCommand.SetHandler(Process, workspaceOption, repoSourceOption, repoBranchOption, solutionFilePath);

            await rootCommand.InvokeAsync(args);

            return 0;
        }
        
        private static async Task Process(string workspace, string repoName, string branch, string solutionFilepath)
        {
            DevOpsMetrics metrics = new DevOpsMetrics();

            if (String.IsNullOrWhiteSpace(solutionFilepath))
            {
                solutionFilepath = FileSystem.Instance.Directory.EnumerateFiles(workspace, "*.sln", SearchOption.AllDirectories).FirstOrDefault();
            }
            
            if (String.IsNullOrWhiteSpace(solutionFilepath))
            {
                throw new InvalidOperationException("Could not locate a solution file (.sln) in workspace: " + workspace);
            }

            Solution solution = Solution.Load(solutionFilepath);

            List<string> legacyStyleProjects = new List<string>();
            foreach (var projectInSolution in solution.Projects)
            {
                var project = solution.LoadProject(projectInSolution);

                if (project is not { ProjectStyle: ProjectStyle.Sdk })
                {
                    legacyStyleProjects.Add(projectInSolution.Name);
                }
            }

            string output = String.Join("#", legacyStyleProjects);

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