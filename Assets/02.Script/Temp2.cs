using System.Collections.Generic;
using System.Linq;
using EnumTypes;
using EventLibrary;
using UnityEngine;

public class Temp2
{
    private Dictionary<Vector2, TileNode> _tileGrid = new Dictionary<Vector2, TileNode>();
    private Dictionary<int, Dictionary<Vector2, TileNode>> _PathTileList = new Dictionary<int, Dictionary<Vector2, TileNode>>();
    private Dictionary<Vector2, TileNode> _startPoint = new Dictionary<Vector2, TileNode>();
    private Dictionary<Vector2, TileNode> _endPoint = new Dictionary<Vector2, TileNode>();

    private float CellSize;

    public void SetTileGridEvent(bool isRegister)
    {
        if (isRegister) AddEvents();
        else RemoveEvents();
    }

    private void AddEvents()
    {
        EventManager<StageEvent>.StartListening(StageEvent.ResetTileGrid, ResetTileGrid);
        EventManager<StageEvent>.StartListening<Vector2, TileNode>(StageEvent.SetPathTileGridAdd, AddTileGrid);
        EventManager<StageEvent>.StartListening<float>(StageEvent.SetPathEndPoint, SetStartAndEndPoint);
        EventManager<StageEvent>.StartListening(StageEvent.SortPathTileGrid, SortTileGrid);
    }

    private void RemoveEvents()
    {
        EventManager<StageEvent>.StopListening(StageEvent.ResetTileGrid, ResetTileGrid);
        EventManager<StageEvent>.StopListening<Vector2, TileNode>(StageEvent.SetPathTileGridAdd, AddTileGrid);
        EventManager<StageEvent>.StopListening<float>(StageEvent.SetPathEndPoint, SetStartAndEndPoint);
        EventManager<StageEvent>.StopListening(StageEvent.SortPathTileGrid, SortTileGrid);
    }

    private void ResetTileGrid()
    {
        _startPoint.Clear();
        _endPoint.Clear();
        _tileGrid.Clear();
        _PathTileList.Clear();
    }

    private void AddTileGrid(Vector2 tilePosition, TileNode tileNode)
    {
        if (_tileGrid.ContainsKey(tilePosition)) _tileGrid[tilePosition] = tileNode;
        else _tileGrid.Add(tilePosition, tileNode);
    }

    private void SetStartAndEndPoint(float cellSize)
    {
        CellSize = cellSize;

        foreach (var tile in _tileGrid)
        {
            if (tile.Value.GetTileInfo.RoadShape == RoadShape.Start)
            {
                _startPoint.Add(tile.Key, tile.Value);
                continue;
            }

            if (tile.Value.GetTileInfo.RoadShape == RoadShape.End)
            {
                _endPoint.Add(tile.Key, tile.Value);
            }
        }
    }

    private void SortTileGrid()
    {
        foreach(var startPoint in  _startPoint)
        {
            foreach(var endPoint in _endPoint)
            {
                var path = FindPath(startPoint.Key, endPoint.Key);
                if(path != null)
                {
                    // 정답 확인 이벤트 발생
                    EventManager<StageEvent>.TriggerEvent(StageEvent.MissionSuccess);
                    ResetTileGrid();
                    return;
                }

                // 미션 실패 확인 이벤트 발생
                EventManager<StageEvent>.TriggerEvent(StageEvent.CheckMissionFail);
            }
        }
    }

    private Dictionary<Vector2, TileNode> FindPath(Vector2 start, Vector2 end)
    {
        var openSet = new List<Vector2> { start };
        var cameFrom = new Dictionary<Vector2, Vector2>();
        var gScore = new Dictionary<Vector2, float> { [start] = 0 };
        var fScore = new Dictionary<Vector2, float> { [start] = Heuristic(start, end) };

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(n => fScore.ContainsKey(n) ? fScore[n] : float.MaxValue).First();

            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                float tentativeGScore = gScore[current] + Vector2.Distance(current, neighbor);

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // 경로가 없는 경우
    }

    private Dictionary<Vector2, TileNode> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        var totalPath = new Dictionary<Vector2, TileNode>();
        while (cameFrom.ContainsKey(current))
        {
            totalPath[current] = _tileGrid[current];
            current = cameFrom[current];
        }
        totalPath[current] = _tileGrid[current]; // Add the start node
        return totalPath;
    }

    private List<Vector2> GetNeighbors(Vector2 current)
    {
        List<Vector2> neighbors = new List<Vector2>();

        foreach (var direction in new[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left })
        {
            Vector2 neighborPos = current + direction * CellSize;
            bool a = _tileGrid.ContainsKey(neighborPos);
            if (a)
            {
                bool b = IsConnected(current, neighborPos);

                if (b)
                    neighbors.Add(neighborPos);
            }
        }

        return neighbors;
    }

    private float Heuristic(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b);
    }

    private bool IsConnected(Vector2 current, Vector2 neighbor)
    {
        TileNode currentNode = _tileGrid[current];
        TileNode neighborNode = _tileGrid[neighbor];

        // 현재 타일과 이웃 타일의 연결 방향을 가져옴
        List<Vector2Int> currentConnections = GetConnectedDirections(currentNode);
        List<Vector2Int> neighborConnections = GetConnectedDirections(neighborNode);

        // 현재 타일에서 이웃 타일로의 방향
        Vector2Int directionToNeighbor = Vector2Int.RoundToInt((neighbor - current)/CellSize);
        Vector2Int directionFromNeighbor = Vector2Int.RoundToInt((current - neighbor)/CellSize);

        return currentConnections.Contains(directionToNeighbor) && neighborConnections.Contains(directionFromNeighbor);
    }

    private List<Vector2Int> GetConnectedDirections(TileNode tileNode)
    {
        List<Vector2Int> connections = new List<Vector2Int>();

        // 각 RoadShape와 RotateValue에 따라 타일의 연결 방향을 계산합니다.
        switch (tileNode.GetTileInfo.RoadShape)
        {
            case RoadShape.Straight:
                if (tileNode.GetTileInfo.RotateValue % 2 == 0) // 0도, 180도
                {
                    connections.Add(Vector2Int.up);
                    connections.Add(Vector2Int.down);
                }
                else // 90도, 270도
                {
                    connections.Add(Vector2Int.left);
                    connections.Add(Vector2Int.right);
                }
                break;
            case RoadShape.L:
                switch (tileNode.GetTileInfo.RotateValue)
                {
                    case 0:
                        connections.Add(Vector2Int.up);
                        connections.Add(Vector2Int.left);
                        break;
                    case 1:
                        connections.Add(Vector2Int.right);
                        connections.Add(Vector2Int.up);
                        break;
                    case 2:
                        connections.Add(Vector2Int.right);
                        connections.Add(Vector2Int.down);
                        break;
                    case 3:
                        connections.Add(Vector2Int.down);
                        connections.Add(Vector2Int.left);
                        break;
                }
                break;
            case RoadShape.T:
                switch (tileNode.GetTileInfo.RotateValue)
                {
                    case 0:
                        connections.Add(Vector2Int.up);
                        connections.Add(Vector2Int.down);
                        connections.Add(Vector2Int.right);
                        break;
                    case 1:
                        connections.Add(Vector2Int.right);
                        connections.Add(Vector2Int.left);
                        connections.Add(Vector2Int.down);
                        break;
                    case 2:
                        connections.Add(Vector2Int.down);
                        connections.Add(Vector2Int.left);
                        connections.Add(Vector2Int.up);
                        break;
                    case 3:
                        connections.Add(Vector2Int.left);
                        connections.Add(Vector2Int.up);
                        connections.Add(Vector2Int.right);
                        break;
                }
                break;
            case RoadShape.Cross:
                connections.Add(Vector2Int.up);
                connections.Add(Vector2Int.down);
                connections.Add(Vector2Int.left);
                connections.Add(Vector2Int.right);
                break;
            // Start와 End 타일의 경우, 특정 연결 상태만을 가질 수 있도록 처리
            case RoadShape.Start:
                if (tileNode.GetTileInfo.RotateValue == 0)
                {
                    connections.Add(Vector2Int.down);
                }
                else if (tileNode.GetTileInfo.RotateValue == 1)
                {
                    connections.Add(Vector2Int.left);
                }
                else if (tileNode.GetTileInfo.RotateValue == 2)
                {
                    connections.Add(Vector2Int.up);
                }
                else if (tileNode.GetTileInfo.RotateValue == 3)
                {
                    connections.Add(Vector2Int.right);
                }
                break;
            case RoadShape.End:
                if (tileNode.GetTileInfo.RotateValue == 0)
                {
                    connections.Add(Vector2Int.up);
                }
                else if (tileNode.GetTileInfo.RotateValue == 1)
                {
                    connections.Add(Vector2Int.right);
                }
                else if (tileNode.GetTileInfo.RotateValue == 2)
                {
                    connections.Add(Vector2Int.down);
                }
                else if (tileNode.GetTileInfo.RotateValue == 3)
                {
                    connections.Add(Vector2Int.left);
                }
                break;
        }

        return connections;
    }
}
