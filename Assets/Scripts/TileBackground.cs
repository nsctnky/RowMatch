using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TileBackground : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Tile tile;
    
    public Vector2 BoundsSize
    {
        get
        {
            return spriteRenderer.bounds.size;
        }
    }

    public void SetTileArrayIndex(Vector2Int pos)
    {
       SetTileArrayIndex(pos.x, pos.y);
    }

    public void SetTileArrayIndex(int x, int y)
    {
        tile.SetArrayIndex(x, y);
    }
    
    public void SetTile(Tile tile)
    {
        this.tile = tile;
    }

    public Tile GetTile()
    {
        return tile;
    }

    public void SetResetTileBackgroundEvent(ResetTileBackgroundEvent evnt)
    {
        evnt.AddListener(OnResetTileBackground);
    }

    private void OnResetTileBackground()
    {
        tile.Reset();
        tile = null;
        
        PoolManager.Instance.GiveObjectToPool(gameObject);
    }
}
