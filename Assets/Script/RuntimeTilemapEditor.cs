using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>튜토리얼 1단계에서만 동작하는 게임 내 Tilemap 편집기입니다.</summary>
public class RuntimeTilemapEditor : MonoBehaviour
{
    private readonly struct EditCommand
    {
        public readonly Vector3Int Cell;
        public readonly TileBase Before;
        public readonly TileBase After;

        public EditCommand(Vector3Int cell, TileBase before, TileBase after)
        {
            Cell = cell;
            Before = before;
            After = after;
        }
    }

    private Camera editCamera;
    private Tilemap groundTilemap;
    private Tilemap previewTilemap;
    private TileBase currentTile;
    private TilemapCollider2D tilemapCollider;
    private readonly Dictionary<Vector3Int, TileBase> originalTiles = new();
    private readonly Dictionary<Vector3Int, bool> changedCells = new();
    private readonly Stack<EditCommand> undoStack = new();
    private readonly Stack<EditCommand> redoStack = new();
    private Vector3Int lastDragCell = new(int.MinValue, int.MinValue, 0);
    private Font uiFont;
    private string status = "왼쪽 클릭으로 타일을 그려보세요.";
    private float statusUntil;

    private bool IsEditing => CameraLearningTutorial.CurrentMission == 1;
    public int PaintedTileCount
    {
        get
        {
            int count = 0;
            foreach (KeyValuePair<Vector3Int, bool> change in changedCells)
            {
                if (change.Value && !originalTiles.ContainsKey(change.Key))
                    count++;
            }
            return count;
        }
    }

    public void Initialize(Camera cameraToUse)
    {
        editCamera = cameraToUse;
    }

    private void Start()
    {
        if (editCamera == null)
            editCamera = Camera.main;

        currentTile = Resources.Load<TileBase>("Rock1");
        uiFont = Resources.Load<Font>("Fonts/NanumGothic-Regular");
        FindGroundTilemap();

        if (groundTilemap == null || currentTile == null)
        {
            enabled = false;
            Debug.LogError("Runtime Tilemap Editor: Ground Tilemap 또는 Rock1 타일을 찾지 못했습니다.");
            return;
        }

        CaptureOriginalMap();
        CreatePreviewTilemap();
    }

    private void FindGroundTilemap()
    {
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Tilemap candidate in tilemaps)
        {
            if (!candidate.CompareTag("Ground"))
                continue;
            groundTilemap = candidate;
            tilemapCollider = candidate.GetComponent<TilemapCollider2D>();
            return;
        }
    }

    private void CaptureOriginalMap()
    {
        groundTilemap.CompressBounds();
        foreach (Vector3Int cell in groundTilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = groundTilemap.GetTile(cell);
            if (tile != null)
                originalTiles[cell] = tile;
        }
    }

    private void CreatePreviewTilemap()
    {
        GameObject preview = new GameObject("TILE BRUSH PREVIEW", typeof(Tilemap), typeof(TilemapRenderer));
        preview.transform.SetParent(groundTilemap.transform.parent, false);
        previewTilemap = preview.GetComponent<Tilemap>();
        previewTilemap.color = new Color(0.35f, 0.95f, 1f, 0.55f);

        TilemapRenderer groundRenderer = groundTilemap.GetComponent<TilemapRenderer>();
        TilemapRenderer previewRenderer = preview.GetComponent<TilemapRenderer>();
        if (groundRenderer != null)
        {
            previewRenderer.sortingLayerID = groundRenderer.sortingLayerID;
            previewRenderer.sortingOrder = groundRenderer.sortingOrder + 20;
        }
    }

    private void Update()
    {
        if (previewTilemap == null)
            return;

        if (!IsEditing || Mouse.current == null || editCamera == null)
        {
            previewTilemap.ClearAllTiles();
            lastDragCell = new Vector3Int(int.MinValue, int.MinValue, 0);
            return;
        }

        HandleShortcuts();
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (PointerIsOverTutorialUI(mousePosition))
        {
            previewTilemap.ClearAllTiles();
            return;
        }

        Vector3 world = editCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -editCamera.transform.position.z));
        Vector3Int cell = groundTilemap.WorldToCell(world);
        previewTilemap.ClearAllTiles();
        previewTilemap.SetTile(cell, currentTile);

        bool painting = Mouse.current.leftButton.isPressed;
        bool erasing = Mouse.current.rightButton.isPressed;
        if (!painting && !erasing)
        {
            lastDragCell = new Vector3Int(int.MinValue, int.MinValue, 0);
            return;
        }
        if (cell == lastDragCell)
            return;

        SetCell(cell, painting ? currentTile : null, true);
        lastDragCell = cell;
    }

    private static bool PointerIsOverTutorialUI(Vector2 mousePosition)
    {
        bool overTopPanel = mousePosition.y > Screen.height - 245f && mousePosition.x < 650f;
        bool overBottomUi = mousePosition.y < 125f;
        bool overEditorPanel = mousePosition.x > Screen.width - 371f && mousePosition.y < 235f;
        return overTopPanel || overBottomUi || overEditorPanel;
    }

    private void HandleShortcuts()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        bool control = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        if (control && keyboard.zKey.wasPressedThisFrame) Undo();
        if (control && keyboard.yKey.wasPressedThisFrame) Redo();
        if (keyboard.tKey.wasPressedThisFrame) ResetMap();
    }

    private void SetCell(Vector3Int cell, TileBase nextTile, bool recordUndo)
    {
        TileBase previous = groundTilemap.GetTile(cell);
        if (previous == nextTile)
            return;

        groundTilemap.SetTile(cell, nextTile);
        UpdateChangedCell(cell, nextTile);
        if (recordUndo)
        {
            undoStack.Push(new EditCommand(cell, previous, nextTile));
            redoStack.Clear();
        }
        RefreshCollision(cell);
    }

    private void UpdateChangedCell(Vector3Int cell, TileBase tile)
    {
        originalTiles.TryGetValue(cell, out TileBase original);
        if (original == tile)
            changedCells.Remove(cell);
        else
            changedCells[cell] = tile != null;
    }

    private void RefreshCollision(Vector3Int cell)
    {
        groundTilemap.RefreshTile(cell);
        if (tilemapCollider != null && tilemapCollider.hasTilemapChanges)
            tilemapCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
    }

    private void Undo()
    {
        if (undoStack.Count == 0)
        {
            ShowStatus("되돌릴 작업이 없습니다.");
            return;
        }
        EditCommand command = undoStack.Pop();
        SetCell(command.Cell, command.Before, false);
        redoStack.Push(command);
        ShowStatus("마지막 타일 작업을 되돌렸습니다.");
    }

    private void Redo()
    {
        if (redoStack.Count == 0)
        {
            ShowStatus("다시 실행할 작업이 없습니다.");
            return;
        }
        EditCommand command = redoStack.Pop();
        SetCell(command.Cell, command.After, false);
        undoStack.Push(command);
        ShowStatus("타일 작업을 다시 실행했습니다.");
    }

    private void ResetMap()
    {
        foreach (Vector3Int cell in new List<Vector3Int>(changedCells.Keys))
        {
            originalTiles.TryGetValue(cell, out TileBase original);
            groundTilemap.SetTile(cell, original);
        }
        changedCells.Clear();
        undoStack.Clear();
        redoStack.Clear();
        groundTilemap.RefreshAllTiles();
        if (tilemapCollider != null && tilemapCollider.hasTilemapChanges)
            tilemapCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        ShowStatus("Tilemap을 처음 상태로 초기화했습니다.");
    }

    private void ShowStatus(string message)
    {
        status = message;
        statusUntil = Time.unscaledTime + 3f;
    }

    private void OnGUI()
    {
        if (!IsEditing)
            return;
        if (uiFont != null)
            GUI.skin.font = uiFont;

        const float width = 355f;
        const float height = 126f;
        float x = Screen.width - width - 16f;
        float y = Screen.height - height - 82f;
        GUI.Box(new Rect(x, y, width, height), "TILEMAP EDIT MODE");
        GUI.Label(new Rect(x + 18f, y + 30f, width - 36f, 24f), "Current Tile: Rock");
        GUI.Label(new Rect(x + 18f, y + 55f, width - 36f, 44f), "LMB/Drag: 그리기   RMB/Drag: 삭제\nCtrl+Z/Y: 실행 취소/다시 실행   T: 초기화");
        GUI.Label(new Rect(x + 18f, y + 100f, width - 36f, 22f),
            Time.unscaledTime < statusUntil ? status : $"제작 타일: {PaintedTileCount}/5");
    }

    private void OnDestroy()
    {
        if (previewTilemap != null)
            Destroy(previewTilemap.gameObject);
    }
}
