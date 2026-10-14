using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>게임 및 튜토리얼 씬에 배치된 모든 가시 타일을 제거합니다.</summary>
public static class RemoveAllSpikeTiles
{
    private const string CleanupKey = "CameraTutorial.AllSpikesRemoved.Version1";

    [InitializeOnLoadMethod]
    private static void RemoveOnceAfterCompile()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(CleanupKey, false))
                return;
            RemoveFromAllLearningScenes();
            EditorPrefs.SetBool(CleanupKey, true);
        };
    }

    [MenuItem("Tools/Camera Tutorial/5. Remove ALL Spike Tiles")]
    public static void RemoveFromAllLearningScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets(
            "t:Scene", new[] { "Assets/Scenes", "Assets/CameraTutorial" });
        int removed = 0;

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasAlreadyOpen = scene.IsValid() && scene.isLoaded;
            if (!wasAlreadyOpen)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            removed += ClearSpikesInScene(scene);

            if (wasAlreadyOpen)
            {
                // 다른 미저장 작업까지 몰래 저장하지 않고, 사용자가 확인 후 저장하게 합니다.
                EditorSceneManager.MarkSceneDirty(scene);
            }
            else
            {
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"가시 타일 전체 삭제 완료: {removed}개");
    }

    private static int ClearSpikesInScene(Scene scene)
    {
        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                BoundsInt bounds = tilemap.cellBounds;
                foreach (Vector3Int position in bounds.allPositionsWithin)
                {
                    TileBase tile = tilemap.GetTile(position);
                    if (tile == null || !tile.name.ToLowerInvariant().Contains("needle"))
                        continue;
                    tilemap.SetTile(position, null);
                    removed++;
                }

                if (tilemap.name.Contains("Hazard") || tilemap.name.Contains("가시"))
                {
                    tilemap.ClearAllTiles();
                    TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
                    if (collider != null)
                        collider.enabled = false;
                }

                tilemap.CompressBounds();
                tilemap.RefreshAllTiles();
            }
        }
        return removed;
    }
}
