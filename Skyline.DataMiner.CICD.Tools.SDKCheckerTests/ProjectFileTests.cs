namespace Skyline.DataMiner.CICD.Tools.SDKCheckerTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Skyline.DataMiner.CICD.Tools.SDKChecker;

    [TestClass]
    public class ProjectFileTests
    {
        [TestMethod]
        public void IsSdkTest_TrueTest()
        {
            string path = Path.GetFullPath("../../../TestData/sdk.csprojTest");

            // Arrange.

            // Act.
            ProjectFile file = new ProjectFile(path);
            var isSdk = file.UsesSDKStyle();

            // Assert.
            Assert.IsTrue(isSdk);
        }

        [TestMethod]
        public void IsLegacyTest_TrueTest()
        {
            string path = Path.GetFullPath("../../../TestData/legacy.csprojTest");

            // Arrange.

            // Act.
            ProjectFile file = new ProjectFile(path);
            var isSdk = file.UsesSDKStyle();

            // Assert.
            Assert.IsFalse(isSdk);
        }
    }
}