using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator
{
    public class Cell
    {
        public bool visited;
        public bool[] walls = new bool[] { true, true, true, true };
        public int heightLevel; // 0..2
    }

    static List<RectInt> roomRects = new List<RectInt>();

    public static Cell[,] Generate(int width, int height)
    {
        return Generate(width, height, new System.Random().Next());
    }

    public static Cell[,] Generate(int width, int height, int seed)
    {
        roomRects.Clear();
        var cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cells[x, y] = new Cell();

        System.Random rng = new System.Random(seed);
        var stack = new Stack<Vector2Int>();
        int startX = rng.Next(width);
        int startY = rng.Next(height);
        cells[startX, startY].visited = true;
        stack.Push(new Vector2Int(startX, startY));

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            int cx = current.x;
            int cy = current.y;
            var neighbors = new List<(int nx, int ny, int dir)>();

            if (cy + 1 < height && !cells[cx, cy + 1].visited) neighbors.Add((cx, cy + 1, 0));
            if (cx + 1 < width && !cells[cx + 1, cy].visited) neighbors.Add((cx + 1, cy, 1));
            if (cy - 1 >= 0 && !cells[cx, cy - 1].visited) neighbors.Add((cx, cy - 1, 2));
            if (cx - 1 >= 0 && !cells[cx - 1, cy].visited) neighbors.Add((cx - 1, cy, 3));

            if (neighbors.Count > 0)
            {
                stack.Push(current);
                var choice = neighbors[rng.Next(neighbors.Count)];
                int nx = choice.nx;
                int ny = choice.ny;
                int dir = choice.dir;
                cells[cx, cy].walls[dir] = false;
                int opposite = (dir + 2) % 4;
                cells[nx, ny].walls[opposite] = false;
                cells[nx, ny].visited = true;
                stack.Push(new Vector2Int(nx, ny));
            }
        }

        // Этап: создание полостей (комнат)
        CreateRooms(cells, rng);

        // Этап: назначение уровней высоты (менее равномерный алгоритм)
        AssignHeights(cells, rng, 3);

        return cells;
    }

    static void CreateRooms(Cell[,] cells, System.Random rng, int attempts = 20, int minSize = 2, int maxSize = 6)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        roomRects.Clear();

        for (int a = 0; a < attempts; a++)
        {
            int w = rng.Next(minSize, maxSize + 1);
            int h = rng.Next(minSize, maxSize + 1);

            if (w >= width - 2 || h >= height - 2) continue;

            int minX = 1;
            int maxX = width - w - 1; // inclusive
            if (maxX < minX) continue;
            int x = rng.Next(minX, maxX + 1);

            int minY = 1;
            int maxY = height - h - 1; // inclusive
            if (maxY < minY) continue;
            int y = rng.Next(minY, maxY + 1);

            var candidate = new RectInt(x, y, w, h);
            bool intersects = false;
            for (int i = 0; i < roomRects.Count; i++)
            {
                if (roomRects[i].Overlaps(candidate))
                {
                    intersects = true;
                    break;
                }
            }
            if (intersects) continue;

            CarveRoom(cells, x, y, w, h);
            roomRects.Add(candidate);

            int doors = rng.Next(1, 3);
            for (int d = 0; d < doors; d++)
            {
                int side = rng.Next(4);
                int dx, dy, cx, cy, dir;
                if (side == 0) { // top
                    cx = x + rng.Next(0, w);
                    cy = y + h - 1;
                    dx = 0; dy = 1; dir = 0;
                }
                else if (side == 1) { // right
                    cx = x + w - 1;
                    cy = y + rng.Next(0, h);
                    dx = 1; dy = 0; dir = 1;
                }
                else if (side == 2) { // bottom
                    cx = x + rng.Next(0, w);
                    cy = y;
                    dx = 0; dy = -1; dir = 2;
                }
                else { // left
                    cx = x;
                    cy = y + rng.Next(0, h);
                    dx = -1; dy = 0; dir = 3;
                }

                int nx = cx + dx;
                int ny = cy + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    cells[cx, cy].walls[dir] = false;
                    cells[nx, ny].walls[(dir + 2) % 4] = false;
                }
            }
        }
    }

    static void CarveRoom(Cell[,] cells, int x, int y, int w, int h)
    {
         for (int i = x; i < x + w; i++)
         {
             for (int j = y; j < y + h; j++)
             {
                 cells[i, j].visited = true;
                 if (i + 1 < x + w)
                 {
                     cells[i, j].walls[1] = false;
                     cells[i + 1, j].walls[3] = false;
                 }
                 if (j + 1 < y + h)
                 {
                     cells[i, j].walls[0] = false;
                     cells[i, j + 1].walls[2] = false;
                 }
             }
         }
     }

    static void AssignHeights(Cell[,] cells, System.Random rng, int levels = 3)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);

        // 1) Если есть комнаты, используем их центры как "якоря" высот.
        var anchors = new List<(Vector2 center, int level)>();
        if (roomRects.Count > 0)
        {
            for (int i = 0; i < roomRects.Count; i++)
            {
                var r = roomRects[i];
                Vector2 c = new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
                int lvl = rng.Next(levels);
                anchors.Add((c, lvl));
            }
        }
        else
        {
            // если комнат нет, создаём пару случайных якорей
            int count = Mathf.Max(3, levels);
            for (int i = 0; i < count; i++)
            {
                Vector2 c = new Vector2(rng.Next(0, width), rng.Next(0, height));
                int lvl = rng.Next(levels);
                anchors.Add((c, lvl));
            }
        }

        // 2) Для каждой клетки выбираем ближайший якорь и добавляем локальный шум,
        // чтобы уровни не получались слишком ровными.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (anchors.Count == 0)
                {
                    cells[x, y].heightLevel = 0;
                    continue;
                }

                float bestDist = float.MaxValue;
                int baseLevel = 0;
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                for (int i = 0; i < anchors.Count; i++)
                {
                    float d = Vector2.SqrMagnitude(p - anchors[i].center);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        baseLevel = anchors[i].level;
                    }
                }

                // небольшой шум вокруг базового уровня
                int delta = 0;
                int roll = rng.Next(100);
                if (roll < 15) delta = -1;    // немного клеток ниже
                else if (roll > 85) delta = 1; // немного выше

                int lvlFinal = Mathf.Clamp(baseLevel + delta, 0, levels - 1);
                cells[x, y].heightLevel = lvlFinal;
            }
        }

        // 3) Локальные "островки" высот — несколько кругов, где уровень сдвигается
        int islands = Mathf.Clamp(width * height / 200, 1, 8);
        for (int i = 0; i < islands; i++)
        {
            int cx = rng.Next(0, width);
            int cy = rng.Next(0, height);
            int radius = rng.Next(2, 6);
            int dir = rng.Next(2) == 0 ? -1 : 1;

            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    float sqr = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    if (sqr > radius * radius) continue;

                    int lvl = cells[x, y].heightLevel + dir;
                    cells[x, y].heightLevel = Mathf.Clamp(lvl, 0, levels - 1);
                }
            }
        }

        // 4) Финальное сглаживание: ограничиваем перепад между соседями до 1
        // несколько итераций, чтобы убрать прямые скачки 0 -> 2 за один шаг
        var dirs = new (int dx, int dy)[] { (1,0), (-1,0), (0,1), (0,-1) };
        int smoothPasses = 4;
        for (int pass = 0; pass < smoothPasses; pass++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int cur = cells[x, y].heightLevel;
                    for (int d = 0; d < dirs.Length; d++)
                    {
                        int nx = x + dirs[d].dx;
                        int ny = y + dirs[d].dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int n = cells[nx, ny].heightLevel;
                        if (Mathf.Abs(cur - n) > 1)
                        {
                            if (cur < n) n = cur + 1;
                            else n = cur - 1;
                            cells[nx, ny].heightLevel = Mathf.Clamp(n, 0, levels - 1);
                        }
                    }
                }
            }
        }
    }

    public static Texture2D RenderToTexture(Cell[,] cells, int cellSize, int wallThickness)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        int texW = width * cellSize + (width + 1) * wallThickness;
        int texH = height * cellSize + (height + 1) * wallThickness;
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        Color wallColor = Color.blue; // walls in blue

        // grayscale values for 3 levels
        float[] gray = new float[] { 0.25f, 0.6f, 0.95f };

        Color[] fill = new Color[texW * texH];
        for (int i = 0; i < fill.Length; i++) fill[i] = wallColor;
        tex.SetPixels(fill);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int baseX = x * (cellSize + wallThickness) + wallThickness;
                int baseY = y * (cellSize + wallThickness) + wallThickness;

                int lvl = Mathf.Clamp(cells[x, y].heightLevel, 0, gray.Length - 1);
                Color floorColor = new Color(gray[lvl], gray[lvl], gray[lvl]);

                for (int px = 0; px < cellSize; px++)
                    for (int py = 0; py < cellSize; py++)
                        tex.SetPixel(baseX + px, baseY + py, floorColor);

                if (!cells[x, y].walls[0])
                {
                    int ox = baseX;
                    int oy = baseY + cellSize;
                    for (int px = 0; px < cellSize; px++)
                        for (int py = 0; py < wallThickness; py++)
                            tex.SetPixel(ox + px, oy + py, floorColor);
                }
                if (!cells[x, y].walls[1])
                {
                    int ox = baseX + cellSize;
                    int oy = baseY;
                    for (int px = 0; px < wallThickness; px++)
                        for (int py = 0; py < cellSize; py++)
                            tex.SetPixel(ox + px, oy + py, floorColor);
                }
                if (!cells[x, y].walls[2])
                {
                    int ox = baseX;
                    int oy = baseY - wallThickness;
                    for (int px = 0; px < cellSize; px++)
                        for (int py = 0; py < wallThickness; py++)
                            tex.SetPixel(ox + px, oy + py, floorColor);
                }
                if (!cells[x, y].walls[3])
                {
                    int ox = baseX - wallThickness;
                    int oy = baseY;
                    for (int px = 0; px < wallThickness; px++)
                        for (int py = 0; py < cellSize; py++)
                            tex.SetPixel(ox + px, oy + py, floorColor);
                }
            }
        }

        tex.Apply();
        return tex;
    }
}
