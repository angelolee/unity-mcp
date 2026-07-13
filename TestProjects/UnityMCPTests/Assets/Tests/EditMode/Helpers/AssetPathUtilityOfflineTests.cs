using NUnit.Framework;
using com.tgs.mcpforunity.editor.Helpers;
using com.tgs.mcpforunity.editor.Constants;
using UnityEditor;

namespace MCPForUnityTests.Editor.Helpers
{
    public class AssetPathUtilityOfflineTests
    {
        private bool _originalForceRefresh;
        private string _originalGitUrlOverride;

        [SetUp]
        public void SetUp()
        {
            _originalForceRefresh = EditorPrefs.GetBool(EditorPrefKeys.DevModeForceServerRefresh, false);
            _originalGitUrlOverride = EditorPrefs.GetString(EditorPrefKeys.GitUrlOverride, "");
            EditorPrefs.SetString(EditorPrefKeys.GitUrlOverride, "");
        }

        [TearDown]
        public void TearDown()
        {
            EditorPrefs.SetBool(EditorPrefKeys.DevModeForceServerRefresh, _originalForceRefresh);
            EditorPrefs.SetString(EditorPrefKeys.GitUrlOverride, _originalGitUrlOverride);
        }

        [Test]
        public void ShouldUseUvxOffline_WhenForceRefreshEnabled_ReturnsFalse()
        {
            EditorPrefs.SetBool(EditorPrefKeys.DevModeForceServerRefresh, true);
            Assert.IsFalse(AssetPathUtility.ShouldUseUvxOffline());
        }

        [Test]
        public void ShouldUseUvxOffline_DoesNotThrow()
        {
            EditorPrefs.SetBool(EditorPrefKeys.DevModeForceServerRefresh, false);
            Assert.DoesNotThrow(() => AssetPathUtility.ShouldUseUvxOffline());
        }

        [Test]
        public void PackageManifest_UsesSingleVersion_ForPackageAndServer()
        {
            var packageJson = AssetPathUtility.GetPackageJson();

            Assert.IsNotNull(packageJson);
            Assert.IsNull(packageJson["mcpServerVersion"]);
            Assert.AreEqual("10.0.0", AssetPathUtility.GetPackageVersion());
            Assert.AreEqual("mcpforunityserver==10.0.0", AssetPathUtility.GetMcpServerPackageSource());
        }
    }
}
