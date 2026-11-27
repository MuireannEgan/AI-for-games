using UnityEngine;
using System.Collections.Generic;

public class MonsterMovement : MonoBehaviour{


     // moevment speeds
    [Header("Movement Speeds")]
    public float patrolSpeed = 2f;
    public float investigateSpeed = 2.5f;
    public float chaseSpeed = 4.5f;
    public float searchSpeed = 2f;
    public float returnSpeed = 3f;

    // grid settings
    [Header ("Grid settings")]
    public int gridWidth = 50;
    public int gridDepth = 50;
    public float cellSize = 1f;

    [Header ("Ground detection")]
    public float raycastHeight = 20f;
    public LayerMask groundMask;

    [Header ("Obstacle detection")]
    public LayerMask obstacleMask;
    public float charachterHeight = 1.8f;
    public float charachterRadius = 0.3f;

    [Header ("Elevation settings")]
    public float maxStepHeight =0.5f;
    public float maxSlopeAngle = 50f;


    //Movement settings
    [Header("Movement settings")]
    public float rotationSpeed = 360f;
    public float waypointReachDistance = 0.15f;
    public float stoppingDistance =0.5f;
    public float repathDistance =1f;


  //patrol points
    [Header("Patrol Points")]
    public Transform[] patrolPoints;

    private int currentPatrolIndex;

    public bool HasPatrolPoints =>
        patrolPoints != null &&
        patrolPoints.Length > 0;

    //grid dataa

    private class GridNode{
        public Vector3 worldPosition;
        public int gridX;
        public int gridZ;
        public bool walkable;
        public int gCost;
        public int hCost;
        public GridNode parent;
        public int fCost => gCost + hCost;

        public GridNode(Vector3 position, int x, int z, bool canWalk){
            worldPosition = position;
            gridX = x;
            gridZ = z;
            walkable = canWalk;
        }
    }

    private GridNode[,] grid;
    private Vector3 gridBottomLeft;

    //path data

    private List<Vector3> currentPath = new List<Vector3>();
    private int currentWaypointIndex;
    private Vector3 lastRequestedDestination;
    private Vector3 finalDestination;
    private float currentSpeed;
    private bool isMoving;
    private bool hasDestination;
    public bool HasReachedDestination => hasDestination && !isMoving;

    private void Start(){
        GenerateGrid();
    }

    private void Update(){
        UpdateMovement();
    }

    //grid generation
    private void GenerateGrid(){
        gridWidth = Mathf.Max(1,gridWidth);
        gridDepth = Mathf.Max(1, gridDepth);
        cellSize = Mathf.Max(0.1f, cellSize);

        grid = new GridNode[gridWidth,gridDepth];
        Vector3 worldSize = new Vector3(gridWidth*cellSize, 0f, gridDepth*cellSize);
        gridBottomLeft = transform.position - worldSize/2f;

        for(int x = 0; x < gridWidth; x++){
            for(int z = 0; z < gridDepth; z++){
                Vector3 worldPoint = gridBottomLeft + new Vector3((x + 0.5f)*cellSize, 0f,(z+ 0.5f)*cellSize);
                RaycastHit groundHit;

                bool foundGround = Physics.Raycast(
                    worldPoint + Vector3.up * raycastHeight,
                    Vector3.down,
                    out groundHit,
                    raycastHeight*2f,
                    groundMask,
                    QueryTriggerInteraction.Ignore

                );

                if(!foundGround){
                    grid[x,z] = new GridNode(worldPoint, x, z, false);
                    continue;
                }

                Vector3 groundPosition = groundHit.point;
                float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
                bool acceptableSlope = slopeAngle <= maxSlopeAngle;
                Vector3 capsuleBottom = groundPosition + Vector3.up*charachterRadius;
                Vector3 capsuleTop = groundPosition + Vector3.up*Mathf.Max(charachterRadius, charachterHeight - charachterRadius);

                bool blocked = Physics.CheckCapsule(
                    capsuleBottom,
                    capsuleTop,
                    charachterRadius,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                bool walkable = acceptableSlope && !blocked;
                grid[x, z] = new GridNode(groundPosition, x, z, walkable);

            }
        }

        Debug.Log("nav grid generated");

    }


    //world pos to grid node
    private GridNode GetNodeFromWorldPosition(Vector3 position){
        float percentX = (position.x - gridBottomLeft.x)/(gridWidth*cellSize);
        float percentZ = (position.z - gridBottomLeft.z)/(gridDepth*cellSize);

        if(percentX < 0f || percentX >=1f || percentZ <0f || percentZ >=1f){
            return null;
        }

        int x = Mathf.FloorToInt(percentX*gridWidth);
        int z = Mathf.FloorToInt(percentZ*gridDepth);
        return grid[x,z];
    }

    //neighbour detection
    private List<GridNode> GetNeighbours(GridNode node){
        List<GridNode> neighbours = new List<GridNode>();

        for(int x = -1; x <= 1; x++){
            for(int z = -1; z <= 1; z++){
                if(x == 0 && z == 0)
                    continue;
                int checkX = node.gridX + x;
                int checkZ = node.gridZ + z;

                if(checkX < 0 || checkX >= gridWidth || checkZ < 0 || checkZ >= gridDepth)
                    continue;

                GridNode neighbour = grid[checkX,checkZ];

                if(!neighbour.walkable)
                    continue;

                float heightDifference = Mathf.Abs(node.worldPosition.y - neighbour.worldPosition.y);

                if(heightDifference > maxStepHeight)
                    continue;


                //prevent corner clipping
                if(x!=0 && z!= 0){
                    GridNode sideX = grid[node.gridX + x, node.gridZ];
                    GridNode sideZ = grid[node.gridX, node.gridZ + z];

                    if(!sideX.walkable || !sideZ.walkable)
                        continue;

                    if(Mathf.Abs(node.worldPosition.y - sideX.worldPosition.y) > maxStepHeight)
                        continue;
                    if(Mathf.Abs(node.worldPosition.y - sideZ.worldPosition.y) > maxStepHeight)
                        continue;

                }
                neighbours.Add(neighbour);
            }
        }

        return neighbours;
    }

    // A - pathfinding
    private void ResetGridCosts(){
        for(int x = 0; x < gridWidth; x++){
            for(int z = 0; z< gridDepth; z++){
                GridNode node = grid[x,z];

                node.gCost = int.MaxValue;
                node.hCost = 0;
                node.parent = null;
            }
        }
    }

    private List<Vector3> FindPath(Vector3 startPos, Vector3 endPos){
        GridNode startNode = GetNodeFromWorldPosition(startPos);
        GridNode targetNode = GetNodeFromWorldPosition(endPos);

        if(startNode == null || targetNode == null){
            Debug.LogWarning("monster destination outside grid");
            return null;
        }
        if(!startNode.walkable){
            Debug.LogWarning("Starting node is not walkable");
            return null;
        }

        if(!targetNode.walkable){
            targetNode = FindNearestWalkableNode(targetNode);

            if(targetNode == null){
                Debug.LogWarning("No nearby walkable destination");
                return null;
            }
        }

        ResetGridCosts();

        List<GridNode> openSet = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        openSet.Add(startNode);
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);

        while(openSet.Count > 0){
            GridNode currentNode = openSet[0];
            for(int i =1; i < openSet.Count; i++){
                if(openSet[i].fCost < currentNode.fCost ||
                    openSet[i].fCost == currentNode.fCost &&
                    openSet[i].hCost < currentNode.hCost ){
                    currentNode = openSet[i];

                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            //arrived at destination
            if(currentNode == targetNode){
                return RetracePath(startNode, targetNode);
            }

            List<GridNode> neighbours = GetNeighbours(currentNode);
            foreach(GridNode neighbour in neighbours){
                if(closedSet.Contains(neighbour))
                    continue;

                int newMovementCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if(newMovementCost < neighbour.gCost || !openSet.Contains(neighbour)){
                    neighbour.gCost = newMovementCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if(!openSet.Contains(neighbour)){
                        openSet.Add(neighbour);
                    }
                }

            }
        }
        Debug.LogWarning("No path found");
        return null;
    }

    //path cost calcuulation
    private int GetDistance(GridNode a, GridNode b){
        int distanceX = Mathf.Abs(a.gridX - b.gridX);
        int distanceZ = Mathf.Abs(a.gridZ - b.gridZ);
        int verticalCost = Mathf.RoundToInt(Mathf.Abs(a.worldPosition.y - b.worldPosition.y)*10f);
        int diagonal = Mathf.Min(distanceX, distanceZ);
        int straight = Mathf.Abs(distanceX - distanceZ);

        return(14*diagonal)+(10*straight)+verticalCost;
    }

    //path retracing
    private List<Vector3> RetracePath(GridNode startNode, GridNode endNode){
        List<Vector3> path = new List<Vector3>();
        GridNode currentNode = endNode;

        while(currentNode != startNode){
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;

            if(currentNode == null)
                return null;

        }

        path.Reverse();
        return path;

    }

    // movement destination

    public void MoveTo(Vector3 destination, float speed){
        if(grid == null)
            GenerateGrid();

        currentSpeed = Mathf.Max(0f, speed);

        //cut down on recalculating the path each time
        if(hasDestination && Vector3.Distance(destination, lastRequestedDestination)< repathDistance){
            return;
        }

        List<Vector3> newPath = FindPath( transform.position, destination);
        if(newPath == null){
            StopMovement();
            return;
        }

        currentPath = newPath;
        currentWaypointIndex = 0;
        lastRequestedDestination    = destination;
        finalDestination = destination;
        hasDestination = true;
        isMoving = currentPath.Count>0;
    }

    // Movement update
    private void UpdateMovement(){
        if(!isMoving || currentPath == null)
            return;

        if(currentWaypointIndex >= currentPath.Count){
            isMoving = false;
            return;
        }

        Vector3 targetWaypoint = currentPath[currentWaypointIndex];
        Vector3 direction = targetWaypoint - transform.position;

        //rotates to next waypoint
        Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z);

        if(horizontalDirection.sqrMagnitude > 0.001f){
            Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed*Time.deltaTime
            );
        }

        //move towards waypoint

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWaypoint,
            currentSpeed*Time.deltaTime
        );

        //HAS WAYPOINT BEEN REACHED>?
        if(Vector3.Distance(transform.position, targetWaypoint) <= waypointReachDistance){
            currentWaypointIndex++;

            if(currentWaypointIndex >= currentPath.Count){
                isMoving = false;
            }
        }
    }


    //stop moving

    public void StopMovement(){
        isMoving = false;
        hasDestination = false;
        currentPath.Clear();
        currentWaypointIndex = 0;
    }


    //patrol movement

    public void MoveToNextPatrolPoint(){
        if (patrolPoints == null){
            return;
        }

        if (patrolPoints.Length == 0){
            Debug.LogError("No patrol points in array");
            return;
        }

        for (int i = 0; i < patrolPoints.Length; i++){
            int index = currentPatrolIndex;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

            if (patrolPoints[index] == null){
                Debug.LogError("Patrol point is null : "+ index );
                continue;
            }

            Debug.Log("dog moving to patrol point: " + patrolPoints[index].name);

            MoveTo(patrolPoints[index].position, patrolSpeed);
            return;
        }

        Debug.LogError("No valid patrol points");
        StopMovement();
    }


    //nearest patrol point
    public void MoveToNearestPatrolPoint(){
        if (!HasPatrolPoints){
            StopMovement();
            return;
        }

        int nearestIndex = -1;
        float shortestDistance = Mathf.Infinity;

        for (int i = 0; i < patrolPoints.Length; i++){
            if (patrolPoints[i] == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                patrolPoints[i].position
            );

            if(distance <shortestDistance){
                shortestDistance = distance;
                nearestIndex = i;
            }
        }

        if(nearestIndex == -1){
            StopMovement();
            return;
        }


        currentPatrolIndex = nearestIndex;

        MoveTo(patrolPoints[nearestIndex].position, returnSpeed);
    }

    //visual debug - green = walkable, red = blocked, cyan = path to be taken
    [Header("debug")]
    public bool showGridGizmos = false;

    private void OnDrawGizmosSelected(){
        if(!showGridGizmos)
            return;

        if(grid != null){
            for(int x = 0; x<gridWidth; x++){
                for(int z=0; z < gridDepth; z++){
                    GridNode node = grid[x,z];

                    Gizmos.color = node.walkable
                        ?Color.green
                        :Color.red;

                    Gizmos.DrawCube(node.worldPosition, Vector3.one*cellSize*0.2f);
                }
            }
        }

        if(currentPath !=null){
            Gizmos.color = Color.cyan;
            for(int i = currentWaypointIndex; i < currentPath.Count; i++){
                Gizmos.DrawSphere(currentPath[i], 0.12f);

                if(i<currentPath.Count -1){
                    Gizmos.DrawLine(currentPath[i], currentPath[i+1]);
                }
            }
        }
    }

    private GridNode FindNearestWalkableNode(GridNode blockedNode){

        GridNode nearestNode = null;
        float nearestDistance = float.PositiveInfinity;

        for(int x = -2; x <= 2; x++){
            for(int z = -2; z <= 2; z++){

                Vector3 position = blockedNode.worldPosition +
                    new Vector3(x * cellSize, 0, z * cellSize);

                GridNode candidate = GetNodeFromWorldPosition(position);

                if(candidate == null || !candidate.walkable)
                    continue;

                float distance = Vector3.Distance(
                    blockedNode.worldPosition,
                    candidate.worldPosition
                );

                if(distance < nearestDistance){
                    nearestDistance = distance;
                    nearestNode = candidate;
                }
            }
        }

        return nearestNode;
    }
}