using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-1)]
public class GameBoard : MonoBehaviour
{
    [SerializeField] private Tilemap currentState;
    [SerializeField] private Tilemap nextState;
    [SerializeField] private Tile aliveTile;
    [SerializeField] private Tile deadTile;
    [SerializeField] private Pattern pattern;
    [SerializeField] private float updateInterval = 0.05f;

    bool IsPaused;

    private readonly HashSet<Vector3Int> aliveCells = new();
    private readonly HashSet<Vector3Int> cellsToCheck = new();

    public int population { get; private set; }
    public int iterations { get; private set; }
    public float time { get; private set; }

    public void Start()
    { 
        SetPattern(pattern);
    }

    public void Event() {
        IsPaused = !IsPaused;
    }

    public void Update()
    {   
        if (IsPaused && Input.GetMouseButtonDown(0))
        {
            DetectGridCell();
        }
    }

    public void Generate() {
        if (IsPaused) {
            int size = UnityEngine.Random.Range(5, 10);
            for (int i = 0; i < size; i++) {
                Click((Vector3Int)(new Vector2Int(UnityEngine.Random.Range(-10, 10), UnityEngine.Random.Range(-10, 10))));
            }
        }
    }

    public void CleanField() {
        if (IsPaused) {
            Clear();
        }
    }

    private void DetectGridCell()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Click(currentState.WorldToCell(worldPosition));
    }


    private void Click(Vector3Int cell) {
        if (IsAlive(cell)) {
            currentState.SetTile(cell, deadTile);
            aliveCells.Remove(cell);
        } else {
            currentState.SetTile(cell, aliveTile);
            aliveCells.Add(cell);
        }
    }

    private void SetPattern(Pattern pattern)
    {
        Clear();

        Vector2Int center = pattern.GetCenter();

        for (int i = 0; i < pattern.cells.Length; i++)
        {
            Vector3Int cell = (Vector3Int)(pattern.cells[i] - center);
            currentState.SetTile(cell, aliveTile);
            aliveCells.Add(cell);
        }

        population = aliveCells.Count;
    }

    public void SetUpdateInterval(float ui) {
        updateInterval = ui;
    }

    private void Clear()
    {
        aliveCells.Clear();
        cellsToCheck.Clear();
        currentState.ClearAllTiles();
        nextState.ClearAllTiles();
        population = 0;
        IsPaused = true;
        iterations = 0;
        time = 0f;
    }

    private void OnEnable()
    {
        StartCoroutine(Simulate());
    }

    private IEnumerator Simulate()
    {
        while (enabled)
        {   
            var interval = new WaitForSeconds(updateInterval);
            if (!IsPaused) {
                UpdateState();

                population = aliveCells.Count;
                iterations++;
                time += updateInterval;
            }

            yield return interval;
        }
    }

    private void UpdateState()
    {
        cellsToCheck.Clear();

        // Gather cells to check
        foreach (Vector3Int cell in aliveCells)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    cellsToCheck.Add(cell + new Vector3Int(x, y));
                }
            }
        }

        // Transition cells to the next state
        foreach (Vector3Int cell in cellsToCheck)
        {
            int neighbors = CountNeighbors(cell);
            bool alive = IsAlive(cell);

            if (!alive && neighbors == 3)
            {
                nextState.SetTile(cell, aliveTile);
                aliveCells.Add(cell);
            }
            else if (alive && (neighbors < 2 || neighbors > 3))
            {
                nextState.SetTile(cell, deadTile);
                aliveCells.Remove(cell);
            }
            else // no change
            {
                nextState.SetTile(cell, currentState.GetTile(cell));
            }
        }

        // Swap current state with next state
        Tilemap temp = currentState;
        currentState = nextState;
        nextState = temp;
        nextState.ClearAllTiles();
    }

    private int CountNeighbors(Vector3Int cell)
    {
        int count = 0;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int neighbor = cell + new Vector3Int(x, y);

                if (x == 0 && y == 0) {
                    continue;
                } else if (IsAlive(neighbor)) {
                    count++;
                }
            }
        }

        return count;
    }

    private bool IsAlive(Vector3Int cell)
    {
        return currentState.GetTile(cell) == aliveTile;
    }

}
