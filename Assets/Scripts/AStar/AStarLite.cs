using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class AStarLite : MonoBehaviour
{
    int gridSizeX = 50;
    int gridSizeY = 30;

    public Vector2 testPath;

    float cellSize = 2;

    AStarNode[,] aStarNodes;

    AStarNode startNode;

    List<AStarNode> nodesToCheck = new List<AStarNode>();
    List<AStarNode> nodesChecked = new List<AStarNode>();

    List<Vector2> aiPath = new List<Vector2>();


    // Debug
    Vector3 startPositionDebug = new Vector3(1000, 0, 0);
    Vector3 destinationPositionDebug = new Vector3(1000, 0, 0);

    public bool isDebugActiveForCar = false;
    private AStarNode initialStartNode;

    // Start is called before the first frame update
    void Start()
    {
        CreateGrid();

        //FindPath(new Vector2(10, -7));
    }

    private void CreateGrid()
    {
        // Allocate space in the array for the nodes
        aStarNodes = new AStarNode[gridSizeX, gridSizeY];

        // Create the grid of notes
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
            {
                aStarNodes[x, y] = new AStarNode(new Vector2Int(x, y));

                Vector3 worldPosition = ConvertGridPositionToWorldPosition(aStarNodes[x,y]);

                // Check if the node is an obstacle
                Collider2D hitCollider2D = Physics2D.OverlapCircle(worldPosition, cellSize / 2.0f);

                if(hitCollider2D != null)
                {
                    // Ignore AI cars, they are not obstacles
                    if (hitCollider2D.transform.root.CompareTag("AI"))
                        continue;

                    // Ignore player cars, they are not obstacles
                    if (hitCollider2D.transform.root.CompareTag("Player"))
                        continue;

                    // Mark as obstacle
                    aStarNodes[x, y].isObstacle = true;
                }
            }
        // Loop through the grid again and populate neighbours
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
            {
                // Check neighbour to north, if we are on the edge then don't add it
                if(y - 1 >= 0)
                {
                    if (!aStarNodes[x, y - 1].isObstacle)
                        aStarNodes[x, y].neighbours.Add(aStarNodes[x, y - 1]);
                }

                // Check if neighbour to south, if we are on the edge then don't add it
                if (y + 1 <= gridSizeY - 1)
                {
                    if (!aStarNodes[x, y + 1].isObstacle)
                        aStarNodes[x, y].neighbours.Add(aStarNodes[x, y + 1]);
                }

                // Check neighbour to east, if we are on the edge then don't add it
                if (x - 1 >= 0)
                {
                    if (!aStarNodes[x -1, y].isObstacle)
                        aStarNodes[x, y].neighbours.Add(aStarNodes[x -1, y]);
                }

                // Check if neighbour to west, if we are on the edge then don't add it
                if (x + 1 <= gridSizeX - 1)
                {
                    if (!aStarNodes[x +1, y].isObstacle)
                        aStarNodes[x, y].neighbours.Add(aStarNodes[x +1, y]);
                }

            }
    }

    private void Reset()
    {
        nodesToCheck.Clear();
        nodesChecked.Clear();
        aiPath.Clear();

        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                aStarNodes[x, y].Reset();
    }

    public List<Vector2>FindPath(Vector2 destination)
    {
        if (aStarNodes == null)
            return null;

        // Reset the system so we can start from a fresh situation
        Reset();

        // Convert the destination from world to grid position
        Vector2Int destinationGridPoint = ConvertWorldToGridPoint(destination);
        Vector2Int currentPositionGridPoint = ConvertWorldToGridPoint(transform.position);

        // Set a debug position so we can show it while developing
        destinationPositionDebug = destination;

        // Start the algorithm by calculating the cost for the first node
        startNode = GetNodeFromPoint(currentPositionGridPoint);

        AStarNode initialStartNode = startNode;
        //If the start node is an obstacle then the routine will fail
        if (startNode.isObstacle)
        {
            bool isProblemResolvedByCheckingBackwards = false;

            //The car will most often be stuck as it has driven into something. So best way to solve it is to try find a solution backwards
            for(int i=1;i<5;i++)
            {
                Vector3 behindCarPosition = transform.position+ transform.up * -1 * i;

                currentPositionGridPoint = ConvertWorldToGridPoint(behindCarPosition);

                startNode = GetNodeFromPoint(currentPositionGridPoint);

                if (startNode != null)
                {
                    if (!startNode.isObstacle)
                    {
                        isProblemResolvedByCheckingBackwards = true;
                        break;
                    }
                }

            }

            if (!isProblemResolvedByCheckingBackwards)
            {
                startNode = initialStartNode;

                //Check if any of the neighbours is better if it is the pick that instead.
                foreach (AStarNode astarNode in startNode.neighbours)
                {
                    if (!astarNode.isObstacle)
                    {
                        startNode = astarNode;
                        break;
                    }
                }
            }
        }

        // Store the start grid position so we have use it while developing
        startPositionDebug = ConvertGridPositionToWorldPosition(startNode);

        // Set the current node to the start node
        AStarNode currentNode = startNode;

        bool isDoneFindingPath = false;
        int pickedOrder = 1;

        while (!isDoneFindingPath)
        {
            // Remove the current node from the list of nodes that should be checked
            nodesToCheck.Remove(currentNode);

            // Set the pick order
            currentNode.pickedOrder = pickedOrder;

            pickedOrder++;

            // Add the current node to the checked list
            nodesChecked.Add(currentNode);

            // Woop woop - We found the destination
            if (currentNode.gridPosition == destinationGridPoint)
            {
                isDoneFindingPath = true;
                break;
            }

            // Calculate cost for all nodes
            CalculateCostForNodeAndNeighbours(currentNode, currentPositionGridPoint, destinationGridPoint);

            // Check if the neighbour nodes should be considered
            foreach (AStarNode neighbourNode in currentNode.neighbours)
            {
                // Skip any node that has already been checked
                if (nodesChecked.Contains(neighbourNode))
                    continue;

                // Skip any node that is already on the list
                if (nodesToCheck.Contains(neighbourNode))
                    continue;

                // Add the node to the list that we should check
                nodesToCheck.Add(neighbourNode);
            }

            // Sort the list so that the items with the lowest Total Cost(f cost) and if the have the same value then lets pick the one with the lowest cost to reach the goal
            nodesToCheck = nodesToCheck.OrderBy(x => x.fCostTotal).ThenBy(x => x.hCostDistanceFromGoal).ToList();

            // Pick the node with the lowest cost as the next node
            if(nodesToCheck.Count == 0)
            {
                Debug.LogWarning($"No nodes left in next node to check, we have no solution");
                return null;
            }
            else
            {
                currentNode = nodesToCheck[0];
            }  
        }

        aiPath = CreatePathForAI(currentPositionGridPoint);

        return aiPath;
    }

    List<Vector2>CreatePathForAI(Vector2Int currentPositionGridPoint)
    {
        List<Vector2> resultAIPath = new List<Vector2>();
        List<AStarNode> aiPath = new List<AStarNode>();

        // Reverse the nodes to check as the last added node will be the AI destination
        nodesChecked.Reverse();

        bool isPathCreated = false;

        AStarNode currentNode = nodesChecked[0];

        aiPath.Add(currentNode);

        int attempts = 0;

        while (!isPathCreated)
        {

            // Go backwards with the lowest creation order
            currentNode.neighbours = currentNode.neighbours.OrderBy(x => x.pickedOrder).ToList();

            // Pick the neighbour with the lowest cost if it is not already in the list
            foreach (AStarNode aStarNode in currentNode.neighbours)
            {
                if(!aiPath.Contains(aStarNode) && nodesChecked.Contains(aStarNode))
                {
                    aiPath.Add(aStarNode);
                    currentNode = aStarNode;

                    break;
                }
            }

            if (currentNode == startNode)
                isPathCreated = true;

            if (attempts > 1000)
            {
                Debug.LogWarning("CreatedPathForAI failed after too many attempts");
                break;
            }

            attempts++;
        }

        foreach (AStarNode aStarNode in aiPath)
        {
            resultAIPath.Add(ConvertGridPositionToWorldPosition(aStarNode));
        }

        // Flip the result
        resultAIPath.Reverse();

        return resultAIPath;
    }

    private void CalculateCostForNodeAndNeighbours(AStarNode aStarNode, Vector2Int aiPosition, Vector2Int aiDestination)
    {
        aStarNode.CalculateCostForNode(aiPosition, aiDestination);

        foreach (AStarNode neighbourNode in aStarNode.neighbours)
        {
            neighbourNode.CalculateCostForNode(aiPosition, aiDestination);
        }
    }

    AStarNode GetNodeFromPoint(Vector2Int gridPoint)
    {
        if (gridPoint.x < 0)
            return null;

        if (gridPoint.x > gridSizeX -1)
            return null;

        if (gridPoint.y < 0)
            return null;

        if (gridPoint.y > gridSizeY -1)
            return null;

        return aStarNodes[gridPoint.x, gridPoint.y];
    }


    private Vector2Int ConvertWorldToGridPoint(Vector2 position)
    {
        // Calculate grid point
        Vector2Int gridPoint = new Vector2Int(Mathf.RoundToInt(position.x / cellSize + gridSizeX / 2.0f), Mathf.RoundToInt(position.y / cellSize + gridSizeY / 2.0f));
        return gridPoint;
    }

    private Vector3 ConvertGridPositionToWorldPosition(AStarNode aStarNode)
    {
        return new Vector3(aStarNode.gridPosition.x * cellSize - (gridSizeX * cellSize) / 2.0f, aStarNode.gridPosition.y * cellSize - (gridSizeY * cellSize) / 2.0f, 0);
    }

    private void OnDrawGizmos()
    {
        if (aStarNodes == null)
            return;

        if (!isDebugActiveForCar)
            return;

        // Draw the grid so we can see what it looks like - only for debug purpose
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
            {
                if (aStarNodes[x, y].isObstacle)
                    Gizmos.color = Color.red;
                else Gizmos.color = Color.green;

                Gizmos.DrawWireCube(ConvertGridPositionToWorldPosition(aStarNodes[x, y]), new Vector3(cellSize, cellSize, cellSize));
            }

        // Draw the nodes that we have checked
        foreach (AStarNode checkedNode in nodesChecked)
        {
            Gizmos.color = Color.green;
            //Gizmos.DrawSphere(ConvertGridPositionToWorldPosition(checkedNode), 1.0f);

#if UNITY_EDITOR

            Vector3 labelPosition = ConvertGridPositionToWorldPosition(checkedNode);
            labelPosition.z = -1;

            GUIStyle style = new GUIStyle();

            style.normal.textColor = Color.green;
            Handles.Label(labelPosition + new Vector3(-0.6f, 1f, 0), $"{checkedNode.hCostDistanceFromGoal}", style);

            style.normal.textColor = Color.red;
            Handles.Label(labelPosition + new Vector3(0.5f, 1f, 0), $"{checkedNode.gCostDistanceFromStart}", style);

            style.normal.textColor = Color.yellow;
            Handles.Label(labelPosition + new Vector3(0.5f, -0.5f, 0), $"{checkedNode.pickedOrder}", style);

            style.normal.textColor = Color.white;
            Handles.Label(labelPosition + new Vector3(0, 0.2f, 0), $"{checkedNode.fCostTotal}", style);
#endif
        }

        // Draw the nodes that we should check
        foreach (AStarNode toCheckNode in nodesToCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(ConvertGridPositionToWorldPosition(toCheckNode), 1.0f);
        }

        Vector3 lastAIPoint = Vector3.zero;
        bool isFirstStep = true;

        Gizmos.color = Color.black;
        foreach(Vector2 point in aiPath)
        {
            if (!isFirstStep)
                Gizmos.DrawLine(lastAIPoint, point);

            lastAIPoint = point;

            isFirstStep = false;
        }

        // Draw start position
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(startPositionDebug, 1f);

        // Draw end position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(destinationPositionDebug, 1f);
    }

}
