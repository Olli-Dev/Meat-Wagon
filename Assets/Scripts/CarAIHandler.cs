using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CarAIHandler : MonoBehaviour
{
    public enum AIMode { followPlayer, followWaypoints, followMouse };

    [Header("AI settings")]
    public AIMode aiMode;
    public float maxSpeed = 16;
    public bool isAvoidingCars = true;
    [Range(0.0f, 1.0f)]
    public float skillLevel = 1.0f;

    // Local variables
    Vector3 targetPosition = Vector3.zero;
    Transform targetTransform = null;
    float originalMaximumSpeed = 0;

    // Stuck handling
    bool isRunningStuckCheck = false;
    bool isFirstTemporaryWaypoint = false;
    int stuckCheckCounter = 0;
    List<Vector2> temporaryWaypoints = new List<Vector2>();
    float angleToTarget = 0;

    // Avoidance
    Vector2 avoidanceVectorLerped = Vector3.zero;

    // Waypoints
    WaypointNode currentWaypoint = null;
    WaypointNode previousWaypoint = null;
    WaypointNode[] allWaypoints;

    // Colliders
    PolygonCollider2D polygonCollider2D;

    //Components
    TopDownCarController topDownCarController;
    AStarLite aStarLite;

    private void Awake()
    {
        topDownCarController = GetComponent<TopDownCarController>();
        allWaypoints = FindObjectsOfType<WaypointNode>();
        aStarLite = GetComponent<AStarLite>();

        polygonCollider2D = GetComponentInChildren<PolygonCollider2D>();

        originalMaximumSpeed = maxSpeed;
    }
    // Start is called before the first frame update
    private void Start()
    {
        SetMaxSpeedBasedOnSkillLevel(maxSpeed);
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        Vector2 inputVector = Vector2.zero;

        switch (aiMode)
        {
            case AIMode.followPlayer:
                FollowPlayer();
                break;

            case AIMode.followWaypoints:
                if (temporaryWaypoints.Count == 0)
                    FollowWaypoints();
                else FollowTemporaryWayPoints();
                break;

            case AIMode.followMouse:
                FollowMousePosition();
                break;
        }

        inputVector.x = TurnTowardsTarget();
        inputVector.y = ApplyThrottleOrBreak(inputVector.x);

        // If the AI is applying throttle but not managing to get any speed then lets run our stuck check
        if (topDownCarController.GetVelocityMagnitude() < 0.5f && Mathf.Abs(inputVector.y) > 0.01f && !isRunningStuckCheck)
            StartCoroutine(StuckCheckCO());

        // Handle specal case where the car has reversed for a while then it should check if it is still stuck. If it is not then it will drive forward again.
        if (stuckCheckCounter >= 4 && !isRunningStuckCheck)
            StartCoroutine(StuckCheckCO());

        // Send the inputs to the car controller
        topDownCarController.SetInputVector(inputVector);
    }

    public Transform GetClosestEscapePoint(Transform[] escapePoint)
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        for (int i = 0; i < escapePoint.Length; i++)
        {
            Vector3 directionToTarget = escapePoint[i].position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = escapePoint[i];
            }
        }

        return bestTarget;
    }
    private void FollowPlayer()
    {
        if (targetTransform == null)
        {
            targetTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (targetTransform != null)
            targetPosition = targetTransform.position;
    }
    private void FollowWaypoints()
    {
        // Pick the closeset waypoint if we don't have a waypoint set
        if (currentWaypoint == null)
        {
            currentWaypoint = FindClosestWaypoint();
            previousWaypoint = currentWaypoint;

        }
        

        // Set the target on the waypoints position
        if (currentWaypoint != null)
        {
            targetPosition = currentWaypoint.transform.position;

            // Store how close we are to the target
            float distanceToWayPoint = (targetPosition - transform.position).magnitude;

            // Navigate towards nearest point on line
            if(distanceToWayPoint > 20)
            {
                Vector3 nearestPointOnTheWayPointLine = FindNearestPointOnLine(previousWaypoint.transform.position, currentWaypoint.transform.position, transform.position);

                float segments = distanceToWayPoint / 20.0f;


                targetPosition = (targetPosition + nearestPointOnTheWayPointLine * segments) / (segments + 1);

                Debug.DrawLine(transform.position, targetPosition, Color.cyan);
            }

            // Check if we are close enough to consider that we have reached the waypoint
            if (distanceToWayPoint <= currentWaypoint.minDistanceToReachWaypoint)
            {
                if (currentWaypoint.maxSpeed > 0)
                    SetMaxSpeedBasedOnSkillLevel(currentWaypoint.maxSpeed);
                else SetMaxSpeedBasedOnSkillLevel(1000);

                // Store the current waypoint as previous before we assign a new current one
                previousWaypoint = currentWaypoint;

                // If we are close enough then flollow to the next waypoint, but if there are multiple waypoints, pick a random one
                currentWaypoint = currentWaypoint.nextWaypointNode[UnityEngine.Random.Range(0, currentWaypoint.nextWaypointNode.Length)];
            }
        }
    }

    // AI follows waypoints
    private void FollowTemporaryWayPoints()
    {
        // Set the target position of for the AI
        targetPosition = temporaryWaypoints[0];

        // Store how close we are to the target
        float distanceToWayPoint = (targetPosition - transform.position).magnitude;

        // Drive a bit slower than usual
        SetMaxSpeedBasedOnSkillLevel(5);

        // Check if we are close enough to consider that we have reached the waypoint
        float minDistanceToReachWaypoint = 1.5f;

        if (!isFirstTemporaryWaypoint)
            minDistanceToReachWaypoint = 3.0f;

        if(distanceToWayPoint <= minDistanceToReachWaypoint)
        {
            temporaryWaypoints.RemoveAt(0);
            isFirstTemporaryWaypoint = false;
        }
    }

    // AI follows the mouse position
    private void FollowMousePosition()
    {
        // Take the mouse in screen space and convert it to world space
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Set the target position of for the AI to follow
        targetPosition = worldPosition;
    }

    // Find the closest Waypoint to the AI
    private WaypointNode FindClosestWaypoint()
    {
        return allWaypoints
            .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
            .FirstOrDefault();
    }

    private float TurnTowardsTarget()
    {
        Vector2 vectorToTarget = targetPosition - transform.position;
        vectorToTarget.Normalize();

        // Apply avoidance to steering
        if(isAvoidingCars)
            AvoidCars(vectorToTarget, out vectorToTarget);

        // Calculate an angle towards the target
        angleToTarget = Vector2.SignedAngle(transform.up, vectorToTarget);
        angleToTarget *= -1; // makes it inverted

        // We want the car to turn as much as possible if the angle is greater than 45 degrees and we want it to smooth out so if the angel is small we want the AI to make smaller turns
        float steerAmount = angleToTarget / 45.0f;

        // Clamp steering to between -1 and 1
        steerAmount = Mathf.Clamp(steerAmount, -1.0f, 1.0f);

        return steerAmount;
    }
    private float ApplyThrottleOrBreak(float inputX)
    {
        if (topDownCarController.GetVelocityMagnitude() > maxSpeed)
            return 0; // Merkir at so accelerar bilurin ikki meira

        // Apply throttle forward based on how much the car wants to turn. If it's a sharp turn this will cause the car to apply less speed forward
        float reduceSpeedDueToCornering = Mathf.Abs(inputX) / 1.0f;

        // Apply throttle based on cornering and skill
        float throttle = 1.05f - reduceSpeedDueToCornering * skillLevel;

        // Handle throttle differently when we are following temp waypoints
        if (temporaryWaypoints.Count() != 0)
        {
            // If the angle is larger to reach the target, then it is better to reverse
            if (angleToTarget > 70)
                throttle = throttle * -1;
            else if (angleToTarget < -70)
                throttle = throttle * -1;
            // If we are still stuck after a number of attempts then just reverse
            else if (stuckCheckCounter > 3)
                throttle = throttle * -1;
        }

        // Apply throttle based on cornering and skill.
        return throttle;
    }

    private void SetMaxSpeedBasedOnSkillLevel(float newSpeed)
    {
        maxSpeed = Mathf.Clamp(newSpeed, 0, originalMaximumSpeed);

        float skillbasedMaximumSpeed = Mathf.Clamp(skillLevel, 0.3f, 1.0f);
        maxSpeed = maxSpeed * skillbasedMaximumSpeed;
    }

    // Find the nearest point on a line
    private Vector2 FindNearestPointOnLine(Vector2 lineStartPosition, Vector2 lineEndPosition, Vector2 point)
    {
        // Get heading as a vector
        Vector2 lineHeadingVector = (lineEndPosition - lineStartPosition);

        // Store the max distance
        float maxDistance = lineHeadingVector.magnitude;
        lineHeadingVector.Normalize();

        // Do projection from the start position to the point
        Vector2 lineVectorStartToPoint = point - lineStartPosition;
        float dotProduct = Vector2.Dot(lineVectorStartToPoint, lineHeadingVector);

        // Clamp the dot product to maxDistance
        dotProduct = Mathf.Clamp(dotProduct, 0f, maxDistance);

        return lineStartPosition + lineHeadingVector * dotProduct;
    }

    // Checks for cars ahead of the car.
    private bool IsCarsInFrontOfAICar(out Vector3 position, out Vector3 othercarRightVector)
    {
        // Disable the cars own collider to avoid having the AI car detect itself
        polygonCollider2D.enabled = false;

        // Perform the curcle cast in front of the car with a slight offset forward and only in the Car layer
        /* Details for myself about this raycast:
         * 1. The circle cast will start at the cars position and we move it a little bit forward, just 0.5 units, so that the
         * circle covers the front of the car
         * 2. We set the size of the radius to 1.2f, so that it covers the area of the car. Depends on the size of the car.
         * 3. We move the raycast line so that it "shoots" up from the car. 
         * 4. The distance we shoot the rayCast is 12 units.
         * 5. Because of performance reasons we will only do this to the layer "Car"
         */
        RaycastHit2D raycastHit2D = Physics2D.CircleCast(transform.position + transform.up * 0.5f, 
            0.7f, transform.up, 12, 1 << LayerMask.NameToLayer("Car"));

        // Enable the colliders again so the car can collide and other cars can detect it
        polygonCollider2D.enabled = true;

        if (raycastHit2D.collider != null)
        {
            // Draw a red line showing how long the detecton is, make it red since we have detected another car
            Debug.DrawRay(transform.position, transform.up * 12, Color.red);

            position = raycastHit2D.collider.transform.position;
            othercarRightVector = raycastHit2D.collider.transform.right;

            return true;
        }
        else
        {
            Debug.DrawRay(transform.position, transform.up * 12, Color.black);
        }

        // No car was detected but we still need assign out values so lets just return zero
        position = Vector3.zero;
        othercarRightVector = Vector3.zero;

        return false;
    }

    private void AvoidCars(Vector2 vectorToTarget, out Vector2 newVectorToTarget)
    {
        if(IsCarsInFrontOfAICar(out Vector3 position, out Vector3 othercarRightVector))
        {
            Vector2 avoidanceVector = Vector2.zero;

            // Calculate the reflecing vector if we would hit the other car
            avoidanceVector = Vector2.Reflect((othercarRightVector - transform.position).normalized, othercarRightVector);

            float distanceToTarget = (targetPosition - transform.position).magnitude;

            // We want to be able to control how much desire the AI has to drive towards the waypoint vs avoiding the other cars. 
            // As we get closer to the waypoint the desire to reach the waypoint increases
            float driveToTargetInfluence = 6.0f / distanceToTarget;

            // Ensure that we limit the value to between 30% and 100% as we always want the AI to desire to reach the waypoint
            driveToTargetInfluence = Mathf.Clamp(driveToTargetInfluence, 0.30f, 1.0f);

            // The desire to avoid the car is simply the inverse to reach the waypoint
            float avoidanceInfluence = 1.0f - driveToTargetInfluence;

            // Reduce jittering a little bit by using lerp
            avoidanceVectorLerped = Vector2.Lerp(avoidanceVectorLerped, avoidanceVector, Time.fixedDeltaTime * 4);

            // Calculate a new vector to the target based on the avoidance vector and the desire to reach the waypoint
            newVectorToTarget = vectorToTarget * driveToTargetInfluence + avoidanceVector * avoidanceInfluence;
            newVectorToTarget.Normalize();

            // Draw the vector which indicates the avoidance vector in green
            Debug.DrawRay(transform.position, avoidanceVector * 10, Color.green);

            // Draw the vector that the car will actually take in yellow
            Debug.DrawRay(transform.position, avoidanceVector * 10, Color.yellow);

            return;
        }

        // We need to assign a default value if we didn't hit any cars before we exit the function
        newVectorToTarget = vectorToTarget;
    }

    IEnumerator StuckCheckCO()
    {
        Vector3 initialStuckPosition = transform.position;

        isRunningStuckCheck = true;

        yield return new WaitForSeconds(0.7f);

        // If we have not moved for a second then we are stuck
        if((transform.position - initialStuckPosition).sqrMagnitude < 3)
        {
            // Get a path to the desired position
            temporaryWaypoints = aStarLite.FindPath(currentWaypoint.transform.position);

            // If there was no path found then it will be null so if that happens just make a new empty list
            if (temporaryWaypoints == null)
                temporaryWaypoints = new List<Vector2>();

            stuckCheckCounter++;

            isFirstTemporaryWaypoint = true;
        }
        else stuckCheckCounter = 0;

        isRunningStuckCheck = false;
    }


}
