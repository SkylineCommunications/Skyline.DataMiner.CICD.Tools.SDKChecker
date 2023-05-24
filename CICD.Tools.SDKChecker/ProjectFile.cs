namespace Skyline.DataMiner.CICD.Tools.SDKChecker
{
    using System;
    using System.IO;
    using System.Xml.Linq;

    /// <summary>
    /// Represents a csproj file.
    /// </summary>
    public class ProjectFile
    {
        private readonly string projectFilePath;
        private readonly XDocument projectFileDocument;
        private readonly XElement root;
        private readonly XNamespace ns;

        /// <summary>
        /// Create an instance of <see cref="ProjectFile"/> by reading a csproj file.
        /// </summary>
        /// <param name="pathToProjectFile">Path to the csproj file.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public ProjectFile(string pathToProjectFile)
        {
            this.projectFilePath = pathToProjectFile;

            using (var reader = File.OpenText(pathToProjectFile))
            {
                projectFileDocument = XDocument.Load(reader);
            }

            root = projectFileDocument.Root ?? throw new InvalidOperationException("Unexpected content in '" + pathToProjectFile + "': Root is null.");
            ns = root.Name.Namespace;
        }

        /// <summary>
        /// Checks if a project is using SDK Style.
        /// </summary>
        /// <returns>True if a project is SDK Style.</returns>
        public bool UsesSDKStyle()
        {
            var sdkAttribute = root.Attribute("Sdk");
            return sdkAttribute != null && !String.IsNullOrEmpty(sdkAttribute.Value);
        }
    }
}
