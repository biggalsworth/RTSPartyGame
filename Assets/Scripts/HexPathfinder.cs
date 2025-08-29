using System.Collections.Generic;
using UnityEngine;
    
public static class HexPathfinder
{
    public static List<Vector2> FindPath(Vector2 start, Vector2 goal)
    {
        // Priority queue for open tiles
        PriorityQueue<Vector2> openSet = new PriorityQueue<Vector2>();
        openSet.Enqueue(start, 0);

        // Track where each tile came from
        Dictionary<Vector2, Vector2> cameFrom = new Dictionary<Vector2, Vector2>();

        // Cost from start to each tile
        Dictionary<Vector2, int> gScore = new Dictionary<Vector2, int>
        {
            [start] = 0
        };

        // Estimated total cost from start to goal through each tile
        Dictionary<Vector2, int> fScore = new Dictionary<Vector2, int>
        {
            [start] = HexManager.instance.HexDistance(start, goal)
        };

        Vector2 closestReachable = start;
        int closestDistance = HexManager.instance.HexDistance(start, goal);


        while (openSet.Count > 0)
        {
            Vector2 current = openSet.Dequeue();

            int currentDistance = HexManager.instance.HexDistance(current, goal);
            if (currentDistance < closestDistance)
            {
                closestReachable = current;
                closestDistance = currentDistance;
            }


            if (current == goal)
                return ReconstructPath(cameFrom, current);
            

            foreach (Vector2 neighbor in HexManager.instance.GetNeighbors(current))
            {
                if (!HexManager.instance.Hexes.ContainsKey(neighbor))
                    continue;

                TileClass tile = HexManager.instance.Hexes[neighbor];
                int traverseCost = tile.TraverseCost();

                if (traverseCost < 0)
                    continue; // Skip impassable terrain


                int tentativeGScore = gScore[current] + traverseCost;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    int estimatedCost = tentativeGScore + HexManager.instance.HexDistance(neighbor, goal);
                    fScore[neighbor] = estimatedCost;
                    openSet.Enqueue(neighbor, estimatedCost);
                }
                
            }
        }

        // Goal unreachable — return path to closest reachable tile
        if (closestReachable != start)
        {
            Debug.LogWarning("Goal unreachable. Returning path to closest reachable tile: " + closestReachable);
            return ReconstructPath(cameFrom, closestReachable);
        }

        return null; // No path found at all
    }

    private static List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        List<Vector2> path = new List<Vector2> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }
}

public class PriorityQueue<T>
{
    private List<(T item, int priority)> elements = new List<(T, int)>();

    public int Count => elements.Count;

    public void Enqueue(T item, int priority)
    {
        elements.Add((item, priority));
    }

    public T Dequeue()
    {
        int bestIndex = 0;
        for (int i = 1; i < elements.Count; i++)
        {
            if (elements[i].priority < elements[bestIndex].priority)
                bestIndex = i;
        }

        T bestItem = elements[bestIndex].item;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}


