using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace UnitySkills
{
    /// <summary>
    /// Asset management skills - import, create, delete, search.
    /// </summary>
    public static class AssetSkills
    {
        [UnitySkill("asset_import", "Import an asset from external path")]
        public static object AssetImport(string sourcePath, string destinationPath)
        {
            if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                return new { error = $"Source not found: {sourcePath}" };

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.Copy(sourcePath, destinationPath, true);
            AssetDatabase.ImportAsset(destinationPath);

            return new { success = true, imported = destinationPath };
        }

        [UnitySkill("asset_delete", "Delete an asset")]
        public static object AssetDelete(string assetPath)
        {
            if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
                return new { error = $"Asset not found: {assetPath}" };

            AssetDatabase.DeleteAsset(assetPath);
            return new { success = true, deleted = assetPath };
        }

        [UnitySkill("asset_move", "Move or rename an asset")]
        public static object AssetMove(string sourcePath, string destinationPath)
        {
            var error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
                return new { error };

            return new { success = true, from = sourcePath, to = destinationPath };
        }

        [UnitySkill("asset_duplicate", "Duplicate an asset")]
        public static object AssetDuplicate(string assetPath)
        {
            var newPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CopyAsset(assetPath, newPath);
            return new { success = true, original = assetPath, copy = newPath };
        }

        [UnitySkill("asset_find", "Find assets by name, type, or label")]
        public static object AssetFind(string searchFilter, int limit = 50)
        {
            var guids = AssetDatabase.FindAssets(searchFilter);
            var results = guids.Take(limit).Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                return new
                {
                    path,
                    name = asset?.name,
                    type = asset?.GetType().Name
                };
            }).ToArray();

            return new { count = results.Length, totalFound = guids.Length, assets = results };
        }

        [UnitySkill("asset_create_folder", "Create a new folder in Assets")]
        public static object AssetCreateFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
                return new { error = "Folder already exists" };

            var parent = Path.GetDirectoryName(folderPath);
            var name = Path.GetFileName(folderPath);

            var guid = AssetDatabase.CreateFolder(parent, name);
            return new { success = true, path = folderPath, guid };
        }

        [UnitySkill("asset_refresh", "Refresh the Asset Database")]
        public static object AssetRefresh()
        {
            AssetDatabase.Refresh();
            return new { success = true, message = "Asset database refreshed" };
        }

        [UnitySkill("asset_get_info", "Get information about an asset")]
        public static object AssetGetInfo(string assetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
                return new { error = $"Asset not found: {assetPath}" };

            return new
            {
                path = assetPath,
                name = asset.name,
                type = asset.GetType().Name,
                guid = AssetDatabase.AssetPathToGUID(assetPath),
                labels = AssetDatabase.GetLabels(asset)
            };
        }
    }
}
