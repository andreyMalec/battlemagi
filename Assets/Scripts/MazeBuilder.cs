using UnityEngine;

public class MazeBuilder : MonoBehaviour {
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject rampPrefab; // префаб для наклонного перехода
    public float wallHeight = 2f;
    public float wallThickness = 0.2f;
    public bool useSeed = false;
    public int seed = 0;
    public bool clearBeforeBuild = true;
    public float heightStep = 0.5f;

    [ContextMenu("Build Maze")]
    public void BuildMaze() {
        var cells = useSeed ? MazeGenerator.Generate(width, height, seed) : MazeGenerator.Generate(width, height);
        BuildFromCells(cells);
    }

    public void BuildFromCells(MazeGenerator.Cell[,] cells) {
        if (clearBeforeBuild) ClearGenerated();
        var container = new GameObject("Maze_" + name);
        container.transform.SetParent(transform, false);

        int w = cells.GetLength(0);
        int h = cells.GetLength(1);

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                float zPos = cells[x, y].heightLevel * heightStep;
                Vector3 center = new Vector3(x * cellSize, zPos, y * cellSize);

                Instantiate(floorPrefab, center, Quaternion.identity, container.transform);

                // top wall (north)
                if (cells[x, y].walls[0]) {
                    Vector3 pos = center + wallPrefab.transform.localPosition +
                                  new Vector3(0f, 0f, cellSize * 0.5f);
                    var wobj = Instantiate(wallPrefab, pos, Quaternion.identity, container.transform);
                    wobj.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);
                }

                // right wall (east)
                if (cells[x, y].walls[1]) {
                    Vector3 pos = center + wallPrefab.transform.localPosition + new Vector3(2.5f, 0f, 1.5f) +
                                  new Vector3(cellSize * 0.5f, 0f, 0f);
                    var wobj = Instantiate(wallPrefab, pos, Quaternion.Euler(0f, 90f, 0f), container.transform);
                    wobj.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);
                }

                // bottom border
                if (y == 0 && cells[x, y].walls[2]) {
                    Vector3 pos = center + wallPrefab.transform.localPosition + new Vector3(4f, 0f, 0f) +
                                  new Vector3(0f, 0f, -cellSize * 0.5f);
                    var wobj = Instantiate(wallPrefab, pos, Quaternion.Euler(0f, 180f, 0f), container.transform);
                    wobj.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);
                }

                // left border
                if (x == 0 && cells[x, y].walls[3]) {
                    Vector3 pos = center + wallPrefab.transform.localPosition + new Vector3(2.5f, 0f, -1.5f) +
                                  new Vector3(-cellSize * 0.5f, 0f, 0f);
                    var wobj = Instantiate(wallPrefab, pos, Quaternion.Euler(0f, -90f, 0f), container.transform);
                    wobj.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);
                }
            }
        }

        // генерация наклонов между высотами
        if (rampPrefab != null) {
            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h; y++) {
                    int h0 = cells[x, y].heightLevel;
                    float baseY = h0 * heightStep;
                    Vector3 center = new Vector3(x * cellSize, baseY, y * cellSize);

                    // север (top, внутренняя сторона)
                    if (y + 1 < h && !cells[x, y].walls[0]) {
                        int h1 = cells[x, y + 1].heightLevel;
                        int diff = h1 - h0;
                        if (diff == 1) {
                            float yMid = (h0 + h1) * 0.5f * heightStep;
                            Vector3 pos = center + wallPrefab.transform.localPosition +
                                          new Vector3(0f, 0f, cellSize * 0.5f);
                            pos.y = yMid;
                            Instantiate(rampPrefab, pos, Quaternion.identity, container.transform);
                        }
                    }

                    // юг (bottom) — связь с клеткой (x, y-1), если между ними нет стены
                    if (y - 1 >= 0) {
                        bool openSouth = !cells[x, y - 1].walls[0]; // у южной клетки открытый north
                        if (openSouth) {
                            int h1 = cells[x, y - 1].heightLevel;
                            int diff = h1 - h0;
                            if (diff == 1 ) {
                                float yMid = (h0 + h1) * 0.5f * heightStep;
                                Vector3 pos = center + wallPrefab.transform.localPosition + new Vector3(4f, 0f, -1f) +
                                              new Vector3(0f, 0f, -cellSize * 0.5f);
                                pos.y = yMid;
                                Instantiate(rampPrefab, pos, Quaternion.Euler(0f, 180f, 0f), container.transform);
                            }
                        }
                    }

                    // восток (right, внутренняя сторона)
                    if (x + 1 < w && !cells[x, y].walls[1]) {
                        int h1 = cells[x + 1, y].heightLevel;
                        int diff = h1 - h0;
                        if (diff == 1 ) {
                            float yMid = (h0 + h1) * 0.5f * heightStep;
                            Vector3 pos = center + wallPrefab.transform.localPosition + new Vector3(2.5f, 0f, 1.5f) +
                                          new Vector3(cellSize * 0.5f, 0f, 0f);
                            pos.y = yMid;
                            Instantiate(rampPrefab, pos, Quaternion.Euler(0f, 90f, 0f), container.transform);
                        }
                    }

                    // запад (left) — связь с клеткой (x-1, y), если между ними нет стены
                    if (x - 1 >= 0) {
                        bool openWest = !cells[x - 1, y].walls[1]; // у западной клетки открытый east
                        if (openWest) {
                            int h1 = cells[x - 1, y].heightLevel;
                            int diff = h1 - h0;
                            if (diff == 1 ) {
                                float yMid = (h0 + h1) * 0.5f * heightStep;
                                Vector3 pos = center + wallPrefab.transform.localPosition + new Vector3(1.5f, 0f, -2.5f) +
                                              new Vector3(-cellSize * 0.5f, 0f, 0f);
                                pos.y = yMid;
                                Instantiate(rampPrefab, pos, Quaternion.Euler(0f, -90f, 0f), container.transform);
                            }
                        }
                    }
                }
            }
        }
    }

    void ClearGenerated() {
        for (int i = transform.childCount - 1; i >= 0; i--) {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Maze_")) DestroyImmediate(child.gameObject);
        }
    }
}