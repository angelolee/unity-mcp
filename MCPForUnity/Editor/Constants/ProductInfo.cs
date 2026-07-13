namespace com.tgs.mcpforunity.editor.Constants
{
    /// <summary>Canonical user-facing product identity strings.</summary>
    public static class ProductInfo
    {
        public const string ProductName = "MCP for Unity";
        public const string MenuRoot = "Window/MCP for Unity";

        // TGS releases are synchronized through this repository, not Coplay's public release channels.
        public static bool EnableUpstreamPackageUpdateChecks => false;
    }
}
