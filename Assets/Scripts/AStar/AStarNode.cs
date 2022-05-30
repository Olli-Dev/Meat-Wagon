using System;
using System.Collections.Generic;
using UnityEngine;

public class AStarNode
{
    // The position on the grid
    public Vector2Int gridPosition;

    // List of the nodes neighbours
    public List<AStarNode>neighbours = new List<AStarNode>();

    // Is the node an obstacle
    public bool isObstacle = false;

    // Distance from start point to node
    // The Formula is: f(n) = g(n) + h(n)
    public int gCostDistanceFromStart = 0;

    // Distance from node to goal
    // The Formula is: f(n) = g(n) + h(n)
    public int hCostDistanceFromGoal = 0;

    // The total cost of movement to the grid position
    // The Formula is: f(n) = g(n) + h(n)
    public int fCostTotal = 0;

    // The order in which it was picked
    public int pickedOrder = 0;

    // State to check if the cost has already been calculated
    private bool isCostCalculated = false;

    public AStarNode(Vector2Int gridPosition_)
    {
        gridPosition = gridPosition_;
    }

    public void CalculateCostForNode(Vector2Int aiPosition, Vector2Int aiDestination)
    {
        if (isCostCalculated)
            return;

        gCostDistanceFromStart = Mathf.Abs(gridPosition.x - aiPosition.x) + Mathf.Abs(gridPosition.y - aiPosition.y);
        hCostDistanceFromGoal = Mathf.Abs(gridPosition.x - aiDestination.x) + Mathf.Abs(gridPosition.y - aiDestination.y);

        fCostTotal = gCostDistanceFromStart + hCostDistanceFromGoal;

        isCostCalculated = true;

    }

    public void Reset()
    {
        isCostCalculated = false;
        pickedOrder = 0;
        gCostDistanceFromStart = 0;
        hCostDistanceFromGoal = 0;
        fCostTotal = 0;
    }
}
