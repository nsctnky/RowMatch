using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BoardManager : MonoBehaviour
{
    private static BoardManager _instance;
    public static BoardManager Instance
    {
        get { return _instance; }
    }
    
    [Header("Prefabs")]
    [SerializeField] private TileBackground tileBackgroundPrefab;
    [SerializeField] private Tile tilePrefab;
    
    [Header("Info")]
    [SerializeField] private float tweenTime = .5f;
    [SerializeField] private int moveCount;
    [SerializeField] private int totalPoints;

    [SerializeField] private TileBackground currentTileBackground;
    [SerializeField] private TileBackground targetTileBackground;
    [SerializeField] private Tile currentTile;
    [SerializeField] private Tile targetTile;
    
    [Header("Colors")]
    [SerializeField] private Color redColor;
    [SerializeField] private Color blueColor;
    [SerializeField] private Color greenColor;
    [SerializeField] private Color yellowColor;
    [SerializeField] private Color tickColor;

    [Header("Points")]
    [SerializeField] private int redPoints;
    [SerializeField] private int greenPoints;
    [SerializeField] private int bluePoints;
    [SerializeField] private int yellowPoints;
    
    private LevelData _currentLevel;
    private CellType _lastCompletedType;
    private Vector2Int _currentIndex;
    private Vector2Int _targetIndex;
    private TileBackground[,] _allTileBackgrounds;

    private SwipeStartEvent OnSwipeStartEvent { get; set; } = new SwipeStartEvent();
    private SwipeEndEvent OnSwipeEndEvent { get; set; } = new SwipeEndEvent();
    private TileInteractableChangedEvent OnTileInteractableChangedEvent { get; set; } = new TileInteractableChangedEvent();
    private ResetTileBackgroundEvent OnResetTileBackgroundEvent { get; set; } = new ResetTileBackgroundEvent();
    
    private void Awake()
    {
        _instance = this;
    }

    private void LoadLevel()
    {
        OnSwipeStartEvent.AddListener(OnSwipeStart);
        OnSwipeEndEvent.AddListener(OnSwipeEnd);
        
        moveCount = _currentLevel.MoveCount;
        CreateBoard(_currentLevel.Width, _currentLevel.Height);
    } 
    
    private void UnLoadLevel()
    {
        moveCount = 0;
        totalPoints = 0;
        
        transform.position = Vector3.zero;
        
        OnResetTileBackgroundEvent.Invoke();
        
        OnSwipeEndEvent.RemoveAllListeners();
        OnSwipeStartEvent.RemoveAllListeners();
        OnTileInteractableChangedEvent.RemoveAllListeners();
        OnResetTileBackgroundEvent.RemoveAllListeners();
        StopAllCoroutines();
    }
    
    #region SWIPE

    private void OnSwipeStart()
    {
        OnTileInteractableChangedEvent.Invoke(false);
    }

    private void OnSwipeEnd()
    {
        moveCount -= 1;
        EvaluateSwipe(_currentIndex, _targetIndex);
        OnTileInteractableChangedEvent.Invoke(moveCount != 0);

        if (moveCount == 0)
        {
            Debug.Log("Level is done, and your points: " + totalPoints);
        }
    }
    
    private void SwipeTiles(Vector2Int current, Vector2Int target)
    {
        currentTileBackground = GetTileBackgroundAtIndex(current);
        targetTileBackground = GetTileBackgroundAtIndex(target);

        if(currentTileBackground == null || targetTileBackground == null)
            return;
        
        currentTile = currentTileBackground.GetTile();
        targetTile = targetTileBackground.GetTile();

        if(targetTile.IsCompleted)
            return;

        _currentIndex = current;
        _targetIndex = target;
        
        OnSwipeStartEvent.Invoke();
        currentTileBackground.SetTile(targetTile);
        targetTileBackground.SetTile(currentTile);

        currentTile.SetArrayIndex(target.x, target.y);
        targetTile.SetArrayIndex(current.x, current.y);
        currentTile.SetParent(targetTileBackground.transform);
        targetTile.SetParent(currentTileBackground.transform);
        
        currentTile.DoMove(tweenTime);
        targetTile.DoMove(tweenTime);
        StartCoroutine(WaitForSwipe(tweenTime * 1.05f));
    }

    private IEnumerator WaitForSwipe(float time)
    {
        yield return new WaitForSeconds(time);
        OnSwipeEndEvent.Invoke();
    }
    
    private void EvaluateSwipe(Vector2Int current, Vector2Int target)
    {
        if (IsRowCompleted(current.y))
        {
            UpdateCompletedRow(current.y);
            UpdateTotalPoints();
        }
        
        if(current.y == target.y)
            return;

        if(IsRowCompleted(target.y))
        {
            UpdateCompletedRow(target.y);
            UpdateTotalPoints();
        }
    }

    private bool IsRowCompleted(int y)
    {
        bool isCompleted = false;
        Vector2Int cursor = new Vector2Int(0, y);
        Tile firstColumn = GetTileBackgroundAtIndex(cursor).GetTile();
        for (int i = 1; i < _currentLevel.Width; i++)
        {
            cursor.x = i;
            Tile currentTile = GetTileBackgroundAtIndex(cursor).GetTile();
            if (firstColumn.GetTileType() != currentTile.GetTileType())
            {
                isCompleted = false;
                break;
            }
            
            isCompleted = true;
        }

        _lastCompletedType = firstColumn.GetTileType();
        return isCompleted;
    }
    
    private void UpdateCompletedRow(int y)
    {
        Vector2Int cursor = new Vector2Int(0, y);
        for (int i = 0; i < _currentLevel.Width; i++)
        {
            cursor.x = i;
            Tile tile = GetTileBackgroundAtIndex(cursor).GetTile();
            tile.SetColor(tickColor);
            tile.SetCompleted(true);
        }
    }

    private void UpdateTotalPoints()
    {
        switch (_lastCompletedType)
        {
            case CellType.Blue:
                totalPoints += bluePoints;
                break;
            case CellType.Red:
                totalPoints += redPoints;
                break;
            case CellType.Green:
                totalPoints += greenPoints;
                break;
            case CellType.Yellow:
                totalPoints += yellowPoints;
                break;
            default:
                totalPoints += 0;
                break;
        }
    }
    #endregion SWIPE
    
    #region CREATE BOARD
    private void CreateBoard(int width, int height)
    {
        _allTileBackgrounds = new TileBackground[width, height];
        var offset = tileBackgroundPrefab.BoundsSize;
        var startPos = transform.position;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++ )
            {
                Vector2 pos = new Vector2(startPos.x + (offset.x * x), startPos.y + (offset.y * y));
                TileBackground instantiated =
                    PoolManager.Instance.GetPooledObjectByTag<TileBackground>(tileBackgroundPrefab.tag);
                Tile tile = GetTileFromPool(_currentLevel.Grid[x, y], instantiated.transform);
                tile.SetArrayIndex(x, y);
                
                instantiated.transform.position = pos;
                instantiated.transform.localScale = tileBackgroundPrefab.transform.localScale;
                instantiated.transform.SetParent(transform);
                instantiated.SetResetTileBackgroundEvent(OnResetTileBackgroundEvent);
                instantiated.SetTile(tile);
                _allTileBackgrounds[x, y] = instantiated;
            }
        }
        
        UpdateBoardPosition();
    }

    private void UpdateBoardPosition()
    {
        Vector2 current = transform.position;
        Vector2 size = tileBackgroundPrefab.BoundsSize;
        float x = _currentLevel.Width * .5f * size.x - size.x * .5f;
        float y = _currentLevel.Height * .5f * size.y - size.y * .5f;
        
        transform.position = new Vector3(current.x - x, current.y - y, 0);
    }

    private TileBackground GetTileBackgroundAtIndex(Vector2Int index)
    {
        if (index.x >= _allTileBackgrounds.GetLength(0) || index.x < 0 || 
            index.y >= _allTileBackgrounds.GetLength(1) || index.y < 0)
            return null;

        return _allTileBackgrounds[index.x, index.y];
    }

    private Tile GetTileFromPool(CellType type, Transform parent)
    {
        Tile instantiated = PoolManager.Instance.GetPooledObjectByTag<Tile>(tilePrefab.tag);
        instantiated.transform.SetParent(parent);
        instantiated.transform.localScale = tilePrefab.transform.localScale;
        instantiated.SetType(type);
        instantiated.SetColor(GetColorByType(type));
        instantiated.SetCompleted(false);
        instantiated.SetPositionImmediately(new Vector2(0, 0));
        
        instantiated.SetSwipeStartEvent(OnSwipeStartEvent);
        instantiated.SetSwipeEndEvent(OnSwipeEndEvent);
        instantiated.SetTileInteractableEvent(OnTileInteractableChangedEvent);
        instantiated.SetSwipeEvent(SwipeTiles);
        return instantiated;
    }

    private Color GetColorByType(CellType type)
    {
        switch (type)
        {
            case CellType.Red:
                return redColor;
            case CellType.Green:
                return greenColor;
            case CellType.Blue:
                return blueColor;
            case CellType.Yellow:
                return yellowColor;
            default:
                return Color.white;
        }
    }
    #endregion CREATE BOARD

    #region TEST
    [ContextMenu("Load FirstLevel")]
    private void StartFirstLevel()
    {
        _currentLevel = LevelLoader.Instance.GetLevelAtIndex(0);
        if(_currentLevel.LevelNumber <= 0)
            return;
        
        LoadLevel();
    }

    [ContextMenu("UnLoad Level")]
    private void TEST_Reset()
    {
        UnLoadLevel();        
    }
    #endregion TEST
}

public class SwipeStartEvent : UnityEvent
{
    
}

public class SwipeEndEvent : UnityEvent
{
    
}

public class TileInteractableChangedEvent : UnityEvent<bool>
{
    
}

public class ResetTileBackgroundEvent : UnityEvent
{
    
}