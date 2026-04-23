using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TileBoard : MonoBehaviour
{
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private TileState[] tileStates;
    
    [Header("Enemy Damage Settings")]
    [SerializeField] private float baseDamageMultiplier = 1f;
    [SerializeField] private AnimationCurve damageCurve = AnimationCurve.Linear(0, 1, 1, 10);

    private TileGrid grid;
    private List<Tile> tiles;
    private bool waiting;

    private GameInputControls input;

    private void Awake()
    {
        grid = GetComponentInChildren<TileGrid>();
        tiles = new List<Tile>(16);
        input = new GameInputControls();
    }
    
    private void Start()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ResetProgress();
        }
    }

    private void OnEnable()
    {
        input.Enable();
        input.UI.Navigate.performed += OnNavigate;
        input.UI.Swipe.performed += OnSwipe;
    }

    private void OnDisable()
    {
        input.UI.Navigate.performed -= OnNavigate;
        input.UI.Swipe.performed -= OnSwipe;
        input.Disable();
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
         if (waiting) return;

    // Игнорируем мышь — Navigate только для клавиатуры/геймпада
    if (context.control.device is Mouse) return;

    Vector2 direction = context.ReadValue<Vector2>();
    
    if (direction.magnitude < 0.5f) return;

    Vector2Int moveDirection;
    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
    {
        moveDirection = direction.x > 0 ? Vector2Int.right : Vector2Int.left;
    }
    else
    {
        moveDirection = direction.y > 0 ? Vector2Int.up : Vector2Int.down;
    }

    ExecuteMove(moveDirection);
    }

    private void OnSwipe(InputAction.CallbackContext context)
    {
         if (waiting) return;

    // Принимаем только тачскрин
    if (context.control.device is not Touchscreen) return;

    Vector2 delta = context.ReadValue<Vector2>();
    
    if (delta.magnitude < 20f) return;

    Vector2Int moveDirection;
    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
    {
        moveDirection = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
    }
    else
    {
        moveDirection = delta.y > 0 ? Vector2Int.up : Vector2Int.down;
    }

    ExecuteMove(moveDirection);
    }

    private void ExecuteMove(Vector2Int direction)
    {
        int startX, incrementX, startY, incrementY;

        if (direction == Vector2Int.up)
        {
            startX = 0; incrementX = 1;
            startY = 1; incrementY = 1;
        }
        else if (direction == Vector2Int.down)
        {
            startX = 0; incrementX = 1;
            startY = grid.Height - 2; incrementY = -1;
        }
        else if (direction == Vector2Int.left)
        {
            startX = 1; incrementX = 1;
            startY = 0; incrementY = 1;
        }
        else // right
        {
            startX = grid.Width - 2; incrementX = -1;
            startY = 0; incrementY = 1;
        }

        Move(direction, startX, incrementX, startY, incrementY);
    }

    public void ClearBoard()
    {
        foreach (var cell in grid.cells) {
            cell.tile = null;
        }

        foreach (var tile in tiles) {
            Destroy(tile.gameObject);
        }

        tiles.Clear();
    }

    public void CreateTile()
    {
        Tile tile = Instantiate(tilePrefab, grid.transform);
        tile.SetState(tileStates[0]);
        tile.Spawn(grid.GetRandomEmptyCell());
        tiles.Add(tile);
        
        SoundManager.Instance?.PlayTileSpawnSound();
    }

    private void Move(Vector2Int direction, int startX, int incrementX, int startY, int incrementY)
    {
        bool changed = false;
        int totalMergePower = 0;
        bool hasMoved = false;
        int highestMergeLevel = 0;

        for (int x = startX; x >= 0 && x < grid.Width; x += incrementX)
        {
            for (int y = startY; y >= 0 && y < grid.Height; y += incrementY)
            {
                TileCell cell = grid.GetCell(x, y);

                if (cell.Occupied) {
                    int mergePower = 0;
                    bool tileMoved = MoveTile(cell.tile, direction, out mergePower);
                    changed |= tileMoved;
                    totalMergePower += mergePower;
                
                    if (tileMoved)
                    {
                        hasMoved = true;
                        if (mergePower > highestMergeLevel)
                        {
                            highestMergeLevel = mergePower;
                        }
                    }
                }
            }
        }

        if (changed) {
            // Звуки
            if (hasMoved)
            {
                if (highestMergeLevel > 0)
                {
                    SoundManager.Instance?.PlayMergeSound(highestMergeLevel);
                }
                else
                {
                    SoundManager.Instance?.PlayMoveSound();
                }
            }
        
            if (totalMergePower > 0 && EnemyManager.Instance != null)
            {
                EnemyManager.Instance.ProcessMergeDamage(totalMergePower, Vector2Int.zero);
                SoundManager.Instance?.PlayDamageSound();
            }
        
            StartCoroutine(WaitForChanges());
        }
    }

    private bool MoveTile(Tile tile, Vector2Int direction, out int mergePower)
    {
        mergePower = 0;
        TileCell newCell = null;
        TileCell adjacent = grid.GetAdjacentCell(tile.cell, direction);

        while (adjacent != null)
        {
            if (adjacent.Occupied)
            {
                if (CanMerge(tile, adjacent.tile))
                {
                    mergePower = CalculateMergePower(tile, adjacent.tile);
                    MergeTiles(tile, adjacent.tile);
                    return true;
                }

                break;
            }

            newCell = adjacent;
            adjacent = grid.GetAdjacentCell(adjacent, direction);
        }

        if (newCell != null)
        {
            tile.MoveTo(newCell);
            return true;
        }

        return false;
    }
    
    private int CalculateMergePower(Tile a, Tile b)
    {
        int powerLevel = IndexOf(a.state) + 1;
        return Mathf.RoundToInt(damageCurve.Evaluate(powerLevel) * baseDamageMultiplier);
    }

    private bool CanMerge(Tile a, Tile b)
    {
        return a.state == b.state && !b.locked;
    }

    private void MergeTiles(Tile a, Tile b)
    {
        tiles.Remove(a);
        a.Merge(b.cell);

        int index = IndexOf(b.state) + 1;
    
        if (index >= tileStates.Length)
        {
            TileState newState = tileStates[tileStates.Length - 1];
            b.SetState(newState);
        
            int actualNumber = (int)Mathf.Pow(2, index + 1);
            TextMeshProUGUI text = b.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = actualNumber.ToString();
            }
        }
        else
        {
            TileState newState = tileStates[index];
            b.SetState(newState);
        }
    
        GameManager.Instance.IncreaseScore(b.state.number);
    }

    private int IndexOf(TileState state)
    {
        for (int i = 0; i < tileStates.Length; i++)
        {
            if (state == tileStates[i]) {
                return i;
            }
        }

        return -1;
    }

    private IEnumerator WaitForChanges()
    {
        waiting = true;

        yield return new WaitForSeconds(0.1f);

        waiting = false;

        foreach (var tile in tiles) {
            tile.locked = false;
        }

        if (tiles.Count != grid.Size) {
            CreateTile();
        }

        if (CheckForGameOver()) {
            GameManager.Instance.GameOver();
        }
    }

    public bool CheckForGameOver()
    {
        if (tiles.Count != grid.Size) {
            return false;
        }

        foreach (var tile in tiles)
        {
            TileCell up = grid.GetAdjacentCell(tile.cell, Vector2Int.up);
            TileCell down = grid.GetAdjacentCell(tile.cell, Vector2Int.down);
            TileCell left = grid.GetAdjacentCell(tile.cell, Vector2Int.left);
            TileCell right = grid.GetAdjacentCell(tile.cell, Vector2Int.right);

            if (up != null && CanMerge(tile, up.tile)) {
                return false;
            }

            if (down != null && CanMerge(tile, down.tile)) {
                return false;
            }

            if (left != null && CanMerge(tile, left.tile)) {
                return false;
            }

            if (right != null && CanMerge(tile, right.tile)) {
                return false;
            }
        }

        return true;
    }
}