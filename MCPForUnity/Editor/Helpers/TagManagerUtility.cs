#nullable disable
using System;
using UnityEditor;

namespace MCPForUnity.Editor.Helpers
{
    public static class TagManagerUtility
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";
        private static readonly string[] BuiltInTags =
        {
            "Untagged",
            "Respawn",
            "Finish",
            "EditorOnly",
            "MainCamera",
            "Player",
            "GameController"
        };

        public static bool TagExists(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                return false;

            if (IsBuiltInTag(tagName))
                return true;

            if (!TryGetTagsProperty(out var tags, out _))
                return false;

            return FindTagIndex(tags, tagName) >= 0;
        }

        public static bool EnsureTagExists(string tagName, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(tagName))
            {
                error = "Tag name cannot be empty or whitespace.";
                return false;
            }

            if (TagExists(tagName))
                return true;

            if (!TryGetTagsProperty(out var tags, out error))
                return false;

            int index = tags.arraySize;
            tags.InsertArrayElementAtIndex(index);
            tags.GetArrayElementAtIndex(index).stringValue = tagName;

            tags.serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }

        public static bool RemoveTag(string tagName, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(tagName))
            {
                error = "Tag name cannot be empty or whitespace.";
                return false;
            }

            if (IsBuiltInTag(tagName))
            {
                error = $"Cannot remove the built-in '{tagName}' tag.";
                return false;
            }

            if (!TryGetTagsProperty(out var tags, out error))
                return false;

            int index = FindTagIndex(tags, tagName);
            if (index < 0)
            {
                error = $"Tag '{tagName}' does not exist.";
                return false;
            }

            tags.DeleteArrayElementAtIndex(index);
            tags.serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }

        private static bool TryGetTagsProperty(out SerializedProperty tags, out string error)
        {
            tags = null;
            error = null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (assets == null || assets.Length == 0)
            {
                error = "Could not load ProjectSettings/TagManager.asset.";
                return false;
            }

            var tagManager = new SerializedObject(assets[0]);
            tags = tagManager.FindProperty("tags");
            if (tags != null)
                return true;

            error = "Could not find 'tags' property in TagManager.asset.";
            return false;
        }

        private static int FindTagIndex(SerializedProperty tags, string tagName)
        {
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (string.Equals(tags.GetArrayElementAtIndex(i).stringValue, tagName, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static bool IsBuiltInTag(string tagName)
        {
            foreach (var builtInTag in BuiltInTags)
            {
                if (string.Equals(builtInTag, tagName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
