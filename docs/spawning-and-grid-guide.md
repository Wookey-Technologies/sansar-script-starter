# Sansar Object Spawning and Grid Layout Guide

This comprehensive guide covers everything you need to know about spawning objects in Sansar using `ScenePrivate.CreateCluster` and organizing them in grid layouts for game pieces and structured arrangements.

## Table of Contents
1. [ScenePrivate.CreateCluster Overview](#sceneprivatecreateCluster-overview)
2. [Method Signatures and Parameters](#method-signatures-and-parameters)
3. [Grid Mathematics and Positioning](#grid-mathematics-and-positioning)
4. [Complete Implementation Examples](#complete-implementation-examples)
5. [Advanced Spawning Patterns](#advanced-spawning-patterns)
6. [Error Handling and Best Practices](#error-handling-and-best-practices)
7. [Performance Optimization](#performance-optimization)
8. [Advanced Grid Patterns](#advanced-grid-patterns)

## ScenePrivate.CreateCluster Overview

`ScenePrivate.CreateCluster` is the primary method for spawning objects (clusters) in Sansar scenes. It creates 3D objects from ClusterResource assets at specified positions with rotation and velocity.

### Key Characteristics
- **Asynchronous operation**: Uses callback functions for completion handling
- **Resource-based**: Requires ClusterResource objects (3D models saved to inventory)
- **Physics-enabled**: Supports initial velocity and physics properties
- **Scene-level**: Only available in SceneObjectScript (not ObjectScript)

## Method Signatures and Parameters

### Basic Signature
```csharp
ScenePrivate.CreateCluster(
    ClusterResource clusterResource,    // The 3D object to spawn
    Vector position,                    // World position (X, Y, Z)
    Quaternion rotation,                // Orientation
    Vector velocity,                    // Initial velocity (can be Vector.Zero)
    Action<ScenePrivate.CreateClusterData> callback  // Completion callback
)
```

### Parameter Details

#### ClusterResource
```csharp
// From script properties
[Tooltip("The object to spawn")]
public ClusterResource MyGamePiece;

// From lists (for multiple objects)
public readonly List<ClusterResource> GamePieces;
```

#### Vector (Position)
```csharp
Vector position = new Vector(x, y, z);        // Custom position
Vector position = Vector.Zero;                // Origin (0,0,0)
Vector position = ObjectPrivate.Position;     // Current object position
Vector position = agent.Position;             // Agent position
```

#### Quaternion (Rotation)
```csharp
Quaternion rotation = Quaternion.Identity;              // No rotation
Quaternion rotation = ObjectPrivate.Rotation;           // Current object rotation
Quaternion rotation = Quaternion.FromEulerAngles(       // Euler angles
    0, Math.PI / 2, 0);  // 90 degrees around Y-axis
```

#### Vector (Velocity)
```csharp
Vector velocity = Vector.Zero;                 // Stationary
Vector velocity = new Vector(0, 5, 0);        // Upward velocity
Vector velocity = Vector.Up * 10;             // Upward at 10 units/sec
```

### Callback Function Structure
```csharp
(ScenePrivate.CreateClusterData data) =>
{
    if (data.Success && data.ClusterReference != null)
    {
        // Object created successfully
        Cluster spawnedObject = data.ClusterReference;
        
        // Store reference for later use
        spawnedObjects.Add(spawnedObject);
        
        // Access individual objects in the cluster
        foreach (var objectPrivate in spawnedObject.GetObjectPrivates())
        {
            // Modify components, materials, etc.
        }
    }
    else
    {
        // Handle creation failure
        Log.Write(LogLevel.Error, $"Failed to create object: {data.Message}");
    }
}
```

## Grid Mathematics and Positioning

### Basic Grid System
```csharp
public class GridSpawner : SceneObjectScript
{
    [Tooltip("Size of each grid cell")]
    [DefaultValue(1.0f)]
    public float GridSize = 1.0f;
    
    [Tooltip("Grid origin point")]
    public Vector GridOrigin = Vector.Zero;
    
    // Convert grid coordinates to world position
    private Vector GridToWorld(int gridX, int gridY, int gridZ = 0)
    {
        return new Vector(
            GridOrigin.X + (gridX * GridSize),
            GridOrigin.Y + (gridY * GridSize),
            GridOrigin.Z + (gridZ * GridSize)
        );
    }
    
    // Convert world position to grid coordinates
    private Vector WorldToGrid(Vector worldPos)
    {
        return new Vector(
            Math.Round((worldPos.X - GridOrigin.X) / GridSize),
            Math.Round((worldPos.Y - GridOrigin.Y) / GridSize),
            Math.Round((worldPos.Z - GridOrigin.Z) / GridSize)
        );
    }
    
    // Snap position to nearest grid point
    private Vector SnapToGrid(Vector position)
    {
        int gridX = (int)Math.Round((position.X - GridOrigin.X) / GridSize);
        int gridY = (int)Math.Round((position.Y - GridOrigin.Y) / GridSize);
        int gridZ = (int)Math.Round((position.Z - GridOrigin.Z) / GridSize);
        return GridToWorld(gridX, gridY, gridZ);
    }
    
    // Generate unique grid key for tracking
    private string GetGridKey(int x, int y, int z)
    {
        return $"{x},{y},{z}";
    }
}
```

### Grid Occupancy Tracking
```csharp
// Track what's placed where
private Dictionary<string, Cluster> placedObjects = new Dictionary<string, Cluster>();
private Dictionary<string, int> objectTypes = new Dictionary<string, int>();

// Check if grid position is occupied
private bool IsGridOccupied(int x, int y, int z)
{
    string key = GetGridKey(x, y, z);
    return placedObjects.ContainsKey(key) && 
           placedObjects[key] != null && 
           placedObjects[key].IsValid;
}

// Place object at grid position
private void PlaceAtGrid(int x, int y, int z, ClusterResource resource, int typeIndex)
{
    string gridKey = GetGridKey(x, y, z);
    
    if (IsGridOccupied(x, y, z))
    {
        Log.Write(LogLevel.Warning, $"Grid position {gridKey} already occupied");
        return;
    }
    
    Vector worldPos = GridToWorld(x, y, z);
    
    ScenePrivate.CreateCluster(resource, worldPos, Quaternion.Identity, Vector.Zero, 
        (ScenePrivate.CreateClusterData data) =>
        {
            if (data.Success && data.ClusterReference != null)
            {
                placedObjects[gridKey] = data.ClusterReference;
                objectTypes[gridKey] = typeIndex;
                Log.Write(LogLevel.Info, $"Placed object at grid {gridKey}");
            }
        });
}
```

## Complete Implementation Examples

### Example 1: Chess Board Setup
```csharp
public class ChessBoard : SceneObjectScript
{
    [Tooltip("Chess piece resources")]
    public readonly List<ClusterResource> ChessPieces;
    
    private const float SQUARE_SIZE = 1.0f;
    private Dictionary<string, Cluster> boardPieces = new Dictionary<string, Cluster>();
    
    public override void Init()
    {
        SetupChessBoard();
    }
    
    private void SetupChessBoard()
    {
        // Place white pieces
        PlacePieceRow(0, 0, new int[] { 0, 1, 2, 3, 4, 2, 1, 0 }); // Back row
        PlacePieceRow(1, 0, new int[] { 5, 5, 5, 5, 5, 5, 5, 5 }); // Pawns
        
        // Place black pieces
        PlacePieceRow(6, 0, new int[] { 5, 5, 5, 5, 5, 5, 5, 5 }); // Pawns
        PlacePieceRow(7, 0, new int[] { 0, 1, 2, 3, 4, 2, 1, 0 }); // Back row
    }
    
    private void PlacePieceRow(int row, int startCol, int[] pieceTypes)
    {
        for (int col = 0; col < pieceTypes.Length; col++)
        {
            int pieceType = pieceTypes[col];
            if (pieceType < ChessPieces.Count && ChessPieces[pieceType] != null)
            {
                Vector position = new Vector(col * SQUARE_SIZE, 0, row * SQUARE_SIZE);
                Quaternion rotation = (row > 3) ? 
                    Quaternion.FromEulerAngles(0, Math.PI, 0) : // Rotate black pieces
                    Quaternion.Identity;
                
                PlaceChessPiece(position, rotation, pieceType, row, col);
            }
        }
    }
    
    private void PlaceChessPiece(Vector position, Quaternion rotation, int pieceType, int row, int col)
    {
        ScenePrivate.CreateCluster(ChessPieces[pieceType], position, rotation, Vector.Zero,
            (ScenePrivate.CreateClusterData data) =>
            {
                if (data.Success && data.ClusterReference != null)
                {
                    string key = $"{row},{col}";
                    boardPieces[key] = data.ClusterReference;
                    Log.Write(LogLevel.Info, $"Placed {GetPieceName(pieceType)} at {key}");
                }
                else
                {
                    Log.Write(LogLevel.Error, $"Failed to place piece: {data.Message}");
                }
            });
    }
    
    private string GetPieceName(int type)
    {
        string[] names = { "Rook", "Knight", "Bishop", "Queen", "King", "Pawn" };
        return (type < names.Length) ? names[type] : "Unknown";
    }
}
```

### Example 2: Tetris Grid System
```csharp
public class TetrisGrid : SceneObjectScript
{
    [Tooltip("Tetris block pieces")]
    public readonly List<ClusterResource> TetrisBlocks;
    
    private const int GRID_WIDTH = 10;
    private const int GRID_HEIGHT = 20;
    private const float BLOCK_SIZE = 1.0f;
    
    private Dictionary<string, Cluster> gridBlocks = new Dictionary<string, Cluster>();
    private int[,] gridState = new int[GRID_WIDTH, GRID_HEIGHT]; // 0 = empty, >0 = block type
    
    public override void Init()
    {
        InitializeGrid();
    }
    
    private void InitializeGrid()
    {
        // Initialize empty grid
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                gridState[x, y] = 0; // Empty
            }
        }
    }
    
    // Place a Tetris piece (multiple blocks)
    public void PlaceTetrisPiece(int[,] shape, int startX, int startY, int blockType)
    {
        int shapeHeight = shape.GetLength(0);
        int shapeWidth = shape.GetLength(1);
        
        // Check if placement is valid
        if (!CanPlacePiece(shape, startX, startY))
        {
            Log.Write(LogLevel.Warning, "Cannot place piece - collision detected");
            return;
        }
        
        // Place each block of the piece
        for (int y = 0; y < shapeHeight; y++)
        {
            for (int x = 0; x < shapeWidth; x++)
            {
                if (shape[y, x] == 1) // Block exists in shape
                {
                    int gridX = startX + x;
                    int gridY = startY + y;
                    PlaceBlock(gridX, gridY, blockType);
                }
            }
        }
    }
    
    private bool CanPlacePiece(int[,] shape, int startX, int startY)
    {
        int shapeHeight = shape.GetLength(0);
        int shapeWidth = shape.GetLength(1);
        
        for (int y = 0; y < shapeHeight; y++)
        {
            for (int x = 0; x < shapeWidth; x++)
            {
                if (shape[y, x] == 1)
                {
                    int gridX = startX + x;
                    int gridY = startY + y;
                    
                    // Check bounds
                    if (gridX < 0 || gridX >= GRID_WIDTH || 
                        gridY < 0 || gridY >= GRID_HEIGHT)
                        return false;
                    
                    // Check collision
                    if (gridState[gridX, gridY] != 0)
                        return false;
                }
            }
        }
        return true;
    }
    
    private void PlaceBlock(int gridX, int gridY, int blockType)
    {
        if (blockType >= TetrisBlocks.Count || TetrisBlocks[blockType] == null)
        {
            Log.Write(LogLevel.Error, $"Invalid block type: {blockType}");
            return;
        }
        
        Vector worldPos = new Vector(
            gridX * BLOCK_SIZE,
            gridY * BLOCK_SIZE,
            0
        );
        
        string gridKey = $"{gridX},{gridY}";
        
        ScenePrivate.CreateCluster(TetrisBlocks[blockType], worldPos, Quaternion.Identity, Vector.Zero,
            (ScenePrivate.CreateClusterData data) =>
            {
                if (data.Success && data.ClusterReference != null)
                {
                    gridBlocks[gridKey] = data.ClusterReference;
                    gridState[gridX, gridY] = blockType + 1; // +1 because 0 = empty
                    
                    Log.Write(LogLevel.Info, $"Placed block type {blockType} at ({gridX}, {gridY})");
                }
                else
                {
                    Log.Write(LogLevel.Error, $"Failed to place block: {data.Message}");
                }
            });
    }
    
    // Clear completed lines
    public void ClearCompletedLines()
    {
        for (int y = GRID_HEIGHT - 1; y >= 0; y--)
        {
            if (IsLineFull(y))
            {
                ClearLine(y);
                DropLinesAbove(y);
                y++; // Check this line again after dropping
            }
        }
    }
    
    private bool IsLineFull(int line)
    {
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            if (gridState[x, line] == 0) return false;
        }
        return true;
    }
    
    private void ClearLine(int line)
    {
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            string gridKey = $"{x},{line}";
            if (gridBlocks.ContainsKey(gridKey) && gridBlocks[gridKey] != null)
            {
                gridBlocks[gridKey].Destroy();
                gridBlocks.Remove(gridKey);
            }
            gridState[x, line] = 0;
        }
    }
    
    private void DropLinesAbove(int clearedLine)
    {
        for (int y = clearedLine + 1; y < GRID_HEIGHT; y++)
        {
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                if (gridState[x, y] != 0)
                {
                    // Move block down
                    string oldKey = $"{x},{y}";
                    string newKey = $"{x},{y-1}";
                    
                    if (gridBlocks.ContainsKey(oldKey))
                    {
                        gridBlocks[newKey] = gridBlocks[oldKey];
                        gridBlocks.Remove(oldKey);
                        
                        // Update visual position
                        Vector newPos = new Vector(x * BLOCK_SIZE, (y-1) * BLOCK_SIZE, 0);
                        // Note: Moving existing objects requires different approach
                        // This is simplified - actual implementation would need object movement
                    }
                    
                    gridState[x, y-1] = gridState[x, y];
                    gridState[x, y] = 0;
                }
            }
        }
    }
}
```

## Advanced Spawning Patterns

### WaitFor vs Callback Patterns

Sansar provides two approaches for handling `CreateCluster` operations: asynchronous callbacks and synchronous `WaitFor` patterns.

#### Asynchronous Callback Pattern (Default)
```csharp
// Non-blocking - continues execution immediately
ScenePrivate.CreateCluster(resource, position, rotation, velocity,
    (ScenePrivate.CreateClusterData data) =>
    {
        if (data.Success && data.ClusterReference != null)
        {
            // Handle successful creation
            ProcessSpawnedObject(data.ClusterReference);
        }
        else
        {
            Log.Write(LogLevel.Error, $"Spawn failed: {data.Message}");
        }
    });

// Code here executes immediately, before object is created
Log.Write("Spawn request submitted");
```

#### Synchronous WaitFor Pattern
```csharp
// Blocks until operation completes - used in Dispenser.cs
private void SpawnWithWaitFor()
{
    try
    {
        ScenePrivate.CreateClusterData result = (ScenePrivate.CreateClusterData)WaitFor(
            ScenePrivate.CreateCluster,
            resource,
            position,
            rotation,
            velocity);

        if (result.Success && result.ClusterReference != null)
        {
            // Handle successful creation
            ProcessSpawnedObject(result.ClusterReference);
        }
        else
        {
            Log.Write(LogLevel.Error, $"Spawn failed: {result.Message}");
        }
    }
    catch (Exception ex)
    {
        Log.Write(LogLevel.Error, $"Exception during spawn: {ex.Message}");
    }
    
    // Code here executes only after object creation completes
    Log.Write("Spawn operation completed");
}
```

#### When to Use Each Pattern
- **Callback Pattern**: Use for UI responsiveness, multiple simultaneous spawns, or when you need the script to continue other work
- **WaitFor Pattern**: Use in coroutines when you need to ensure spawning completes before continuing, or when implementing sequential operations

### Throttle Exception Handling

Sansar limits the rate of object creation to prevent performance issues. Handle throttling gracefully:

```csharp
private void SpawnWithThrottleHandling(ClusterResource resource, Vector position)
{
    try
    {
        ScenePrivate.CreateCluster(resource, position, Quaternion.Identity, Vector.Zero,
            (ScenePrivate.CreateClusterData data) =>
            {
                if (data.Success && data.ClusterReference != null)
                {
                    HandleSuccessfulSpawn(data.ClusterReference);
                }
                else
                {
                    Log.Write(LogLevel.Warning, $"Spawn failed: {data.Message}");
                }
            });
    }
    catch (ThrottleException)
    {
        // Throttled - no object created
        Log.Write(LogLevel.Warning, "Spawn throttled - will retry later");
        
        // Implement retry logic
        StartCoroutine(() => RetrySpawn(resource, position));
    }
    catch (Exception ex)
    {
        Log.Write(LogLevel.Error, $"Unexpected error during spawn: {ex.Message}");
    }
}

private void RetrySpawn(ClusterResource resource, Vector position)
{
    Wait(0.5); // Wait before retry
    SpawnWithThrottleHandling(resource, position);
}
```

### Variance and Randomization

Add natural variation to spawned objects for more realistic placement:

```csharp
public class VarianceSpawner : SceneObjectScript
{
    [Tooltip("Position variance in each axis")]
    public Vector PositionVariance = new Vector(0.1f, 0, 0.1f);
    
    [Tooltip("Rotation variance in degrees")]
    public Vector RotationVariance = new Vector(0, 15, 0);
    
    [Tooltip("Velocity variance")]
    public Vector VelocityVariance = new Vector(1, 0, 1);
    
    private Random random = new Random();
    
    // Generate random value between -1 and 1
    private float RandomNegOneToOne()
    {
        return (float)(random.NextDouble() * 2.0 - 1.0);
    }
    
    private void SpawnWithVariance(ClusterResource resource, Vector basePosition, Quaternion baseRotation)
    {
        // Apply position variance
        Vector position = basePosition;
        if (PositionVariance.LengthSquared() > 0.0f)
        {
            position += new Vector(
                PositionVariance.X * RandomNegOneToOne(),
                PositionVariance.Y * RandomNegOneToOne(),
                PositionVariance.Z * RandomNegOneToOne()
            );
        }
        
        // Apply rotation variance
        Quaternion rotation = baseRotation;
        if (RotationVariance.LengthSquared() > 0.0f)
        {
            Vector rotationEuler = new Vector(
                RotationVariance.X * RandomNegOneToOne(),
                RotationVariance.Y * RandomNegOneToOne(),
                RotationVariance.Z * RandomNegOneToOne()
            ) * Mathf.RadiansPerDegree;
            
            Quaternion varianceRotation = Quaternion.FromEulerAngles(rotationEuler);
            rotation = baseRotation * varianceRotation;
        }
        
        // Apply velocity variance
        Vector velocity = Vector.Zero;
        if (VelocityVariance.LengthSquared() > 0.0f)
        {
            velocity = new Vector(
                VelocityVariance.X * RandomNegOneToOne(),
                VelocityVariance.Y * RandomNegOneToOne(),
                VelocityVariance.Z * RandomNegOneToOne()
            );
        }
        
        ScenePrivate.CreateCluster(resource, position, rotation, velocity,
            (ScenePrivate.CreateClusterData data) =>
            {
                if (data.Success && data.ClusterReference != null)
                {
                    Log.Write(LogLevel.Info, $"Spawned with variance at {position}");
                }
            });
    }
}
```

### Motion Inheritance

Spawn objects that inherit motion from their parent or spawner:

```csharp
public class MotionInheritanceSpawner : SceneObjectScript
{
    [Tooltip("Inherit parent object's motion")]
    [DefaultValue(true)]
    public bool InheritMotion = true;
    
    [Tooltip("Base velocity to add")]
    public Vector BaseVelocity = Vector.Zero;
    
    private RigidBodyComponent rigidBody;
    
    public override void Init()
    {
        // Get this object's rigid body for motion inheritance
        ObjectPrivate.TryGetFirstComponent(out rigidBody);
    }
    
    private void SpawnWithMotionInheritance(ClusterResource resource, Vector position, Quaternion rotation)
    {
        Vector velocity = BaseVelocity;
        Vector angularVelocity = Vector.Zero;
        
        // Inherit linear and angular velocity from parent
        if (InheritMotion && rigidBody != null)
        {
            velocity += rigidBody.GetLinearVelocity();
            angularVelocity = rigidBody.GetAngularVelocity();
        }
        
        ScenePrivate.CreateCluster(resource, position, rotation, velocity,
            (ScenePrivate.CreateClusterData data) =>
            {
                if (data.Success && data.ClusterReference != null)
                {
                    // Apply inherited angular velocity if needed
                    if (InheritMotion && angularVelocity.LengthSquared() > 0)
                    {
                        var spawnedObjects = data.ClusterReference.GetObjectPrivates();
                        foreach (var obj in spawnedObjects)
                        {
                            RigidBodyComponent spawnedRB;
                            if (obj.TryGetFirstComponent(out spawnedRB))
                            {
                                if (spawnedRB.GetMotionType() != RigidBodyMotionType.MotionTypeStatic)
                                {
                                    spawnedRB.SetAngularVelocity(angularVelocity);
                                }
                            }
                        }
                    }
                    
                    Log.Write(LogLevel.Info, $"Spawned with inherited motion: {velocity}");
                }
            });
    }
}
```

### Advanced Grid Spawning with Patterns

Combine advanced spawning techniques with grid systems:

```csharp
private void SpawnGridWithAdvancedPatterns(int gridX, int gridY, ClusterResource resource)
{
    Vector basePosition = GridToWorld(gridX, gridY);
    
    // Add slight randomization for natural look
    Vector position = basePosition + new Vector(
        PositionVariance.X * RandomNegOneToOne(),
        0,
        PositionVariance.Z * RandomNegOneToOne()
    );
    
    // Random rotation for variety
    float randomY = RotationVariance.Y * RandomNegOneToOne() * Mathf.RadiansPerDegree;
    Quaternion rotation = Quaternion.FromEulerAngles(0, randomY, 0);
    
    try
    {
        ScenePrivate.CreateCluster(resource, position, rotation, Vector.Zero,
            (ScenePrivate.CreateClusterData data) =>
            {
                if (data.Success && data.ClusterReference != null)
                {
                    string gridKey = GetGridKey(gridX, gridY, 0);
                    placedObjects[gridKey] = data.ClusterReference;
                    Log.Write(LogLevel.Info, $"Placed varied object at grid ({gridX}, {gridY})");
                }
                else
                {
                    Log.Write(LogLevel.Warning, $"Failed to place at grid ({gridX}, {gridY}): {data.Message}");
                }
            });
    }
    catch (ThrottleException)
    {
        Log.Write(LogLevel.Warning, $"Throttled at grid ({gridX}, {gridY}) - will retry");
        StartCoroutine(() => RetryGridSpawn(gridX, gridY, resource));
    }
}

private void RetryGridSpawn(int gridX, int gridY, ClusterResource resource)
{
    Wait(0.2); // Brief delay before retry
    SpawnGridWithAdvancedPatterns(gridX, gridY, resource);
}
```

## Error Handling and Best Practices

### Robust Error Handling
```csharp
private void SafeCreateCluster(ClusterResource resource, Vector position, Quaternion rotation)
{
    if (resource == null)
    {
        Log.Write(LogLevel.Error, "ClusterResource is null - cannot spawn object");
        return;
    }
    
    try
    {
        ScenePrivate.CreateCluster(resource, position, rotation, Vector.Zero,
            (ScenePrivate.CreateClusterData data) =>
            {
                if (data.Success && data.ClusterReference != null && data.ClusterReference.IsValid)
                {
                    // Success - object created
                    HandleSuccessfulSpawn(data.ClusterReference, position);
                }
                else
                {
                    // Failed to create
                    string errorMsg = data.Message ?? "Unknown error";
                    Log.Write(LogLevel.Error, $"Failed to create cluster at {position}: {errorMsg}");
                    HandleFailedSpawn(position, errorMsg);
                }
            });
    }
    catch (Exception ex)
    {
        Log.Write(LogLevel.Error, $"Exception during CreateCluster: {ex.Message}");
    }
}

private void HandleSuccessfulSpawn(Cluster cluster, Vector position)
{
    // Store reference
    spawnedObjects.Add(cluster);
    
    // Log success
    Log.Write(LogLevel.Info, $"Successfully spawned object at {position}");
    
    // Trigger any post-spawn logic
    OnObjectSpawned?.Invoke(cluster);
}

private void HandleFailedSpawn(Vector position, string error)
{
    // Log failure
    Log.Write(LogLevel.Warning, $"Spawn failed at {position}: {error}");
    
    // Trigger failure handling
    OnSpawnFailed?.Invoke(position, error);
    
    // Optionally retry or use fallback
    if (retryCount < maxRetries)
    {
        StartCoroutine(() => RetrySpawn(position));
    }
}
```

## Performance Optimization

### Batch Spawning Patterns

For large grid setups or multiple object creation, use batching to prevent performance spikes:

```csharp
public struct SpawnRequest
{
    public ClusterResource Resource;
    public Vector Position;
    public Quaternion Rotation;
    public Vector Velocity;
}

// Optimized batch spawning with configurable parameters
private void SpawnObjectsInBatches(List<SpawnRequest> requests, int batchSize = 5, float delayBetweenBatches = 0.1f)
{
    if (requests == null || requests.Count == 0)
    {
        Log.Write(LogLevel.Warning, "No spawn requests to process");
        return;
    }
    
    Log.Write(LogLevel.Info, $"Starting batch spawn of {requests.Count} objects");
    StartCoroutine(() => SpawnBatchCoroutine(requests, batchSize, delayBetweenBatches));
}

private void SpawnBatchCoroutine(List<SpawnRequest> requests, int batchSize, float delay)
{
    int totalBatches = (int)Math.Ceiling((double)requests.Count / batchSize);
    int currentBatch = 0;
    
    for (int i = 0; i < requests.Count; i += batchSize)
    {
        currentBatch++;
        int endIndex = Math.Min(i + batchSize, requests.Count);
        
        Log.Write(LogLevel.Info, $"Processing batch {currentBatch}/{totalBatches}");
        
        // Spawn current batch
        List<SpawnRequest> currentBatchRequests = new List<SpawnRequest>();
        for (int j = i; j < endIndex; j++)
        {
            currentBatchRequests.Add(requests[j]);
        }
        
        // Process batch with throttle protection
        ProcessBatchWithThrottleProtection(currentBatchRequests);
        
        // Wait before next batch (except for last batch)
        if (endIndex < requests.Count)
        {
            Wait(delay);
        }
    }
    
    Log.Write(LogLevel.Info, "Batch spawning completed");
}

private void ProcessBatchWithThrottleProtection(List<SpawnRequest> batch)
{
    foreach (var request in batch)
    {
        try
        {
            ScenePrivate.CreateCluster(request.Resource, request.Position, request.Rotation, request.Velocity,
                (ScenePrivate.CreateClusterData data) =>
                {
                    if (data.Success && data.ClusterReference != null)
                    {
                        HandleSuccessfulBatchSpawn(data.ClusterReference);
                    }
                    else
                    {
                        Log.Write(LogLevel.Warning, $"Batch spawn failed: {data.Message}");
                    }
                });
        }
        catch (ThrottleException)
        {
            Log.Write(LogLevel.Warning, "Throttle hit during batch - pausing");
            Wait(0.5); // Longer pause on throttle
        }
        catch (Exception ex)
        {
            Log.Write(LogLevel.Error, $"Exception during batch spawn: {ex.Message}");
        }
    }
}
```

### Optimized Grid Creation

For large grids, use staged creation to maintain performance:

```csharp
public class OptimizedGridCreator : SceneObjectScript
{
    [Tooltip("Create grid in stages")]
    [DefaultValue(true)]
    public bool StagedCreation = true;
    
    [Tooltip("Objects per stage")]
    [DefaultValue(10)]
    public int ObjectsPerStage = 10;
    
    [Tooltip("Delay between stages (seconds)")]
    [DefaultValue(0.1f)]
    public float StageDelay = 0.1f;
    
    // Create large grid efficiently
    private void CreateLargeGrid(int width, int height, ClusterResource resource)
    {
        List<SpawnRequest> allRequests = new List<SpawnRequest>();
        
        // Pre-calculate all spawn positions
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector position = GridToWorld(x, y);
                allRequests.Add(new SpawnRequest
                {
                    Resource = resource,
                    Position = position,
                    Rotation = Quaternion.Identity,
                    Velocity = Vector.Zero
                });
            }
        }
        
        if (StagedCreation)
        {
            SpawnObjectsInBatches(allRequests, ObjectsPerStage, StageDelay);
        }
        else
        {
            // All at once (may cause performance issues)
            ProcessBatchWithThrottleProtection(allRequests);
        }
    }
    
    // Spiral pattern for visual appeal during creation
    private void CreateGridInSpiral(int width, int height, ClusterResource resource)
    {
        List<SpawnRequest> spiralRequests = new List<SpawnRequest>();
        
        int centerX = width / 2;
        int centerY = height / 2;
        int radius = 0;
        
        // Add center first
        Vector centerPos = GridToWorld(centerX, centerY);
        spiralRequests.Add(new SpawnRequest
        {
            Resource = resource,
            Position = centerPos,
            Rotation = Quaternion.Identity,
            Velocity = Vector.Zero
        });
        
        // Add in expanding spiral
        while (spiralRequests.Count < width * height)
        {
            radius++;
            
            // Add positions at current radius
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    // Only positions at exact radius distance
                    if (Math.Abs(x - centerX) == radius || Math.Abs(y - centerY) == radius)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Vector position = GridToWorld(x, y);
                            spiralRequests.Add(new SpawnRequest
                            {
                                Resource = resource,
                                Position = position,
                                Rotation = Quaternion.Identity,
                                Velocity = Vector.Zero
                            });
                        }
                    }
                }
            }
        }
        
        SpawnObjectsInBatches(spiralRequests, ObjectsPerStage, StageDelay);
    }
}
```

### Memory-Efficient Object Tracking

Use efficient data structures for large grids:

```csharp
// Memory-efficient tracking for large grids
public class EfficientGridTracker
{
    // Use Dictionary for sparse grids (many empty cells)
    private Dictionary<string, Cluster> sparseGrid = new Dictionary<string, Cluster>();
    
    // Use 2D array for dense grids (most cells filled)
    private Cluster[,] denseGrid;
    private bool useDenseMode = false;
    
    private int gridWidth, gridHeight;
    private float fillRatio = 0.0f;
    
    public void InitializeGrid(int width, int height)
    {
        gridWidth = width;
        gridHeight = height;
        
        // Switch to dense mode if grid becomes >50% filled
        int totalCells = width * height;
        if (totalCells > 0 && sparseGrid.Count > totalCells * 0.5f)
        {
            SwitchToDenseMode();
        }
    }
    
    private void SwitchToDenseMode()
    {
        if (useDenseMode) return;
        
        Log.Write(LogLevel.Info, "Switching to dense grid mode for better performance");
        denseGrid = new Cluster[gridWidth, gridHeight];
        
        // Transfer existing data
        foreach (var kvp in sparseGrid)
        {
            var coords = ParseGridKey(kvp.Key);
            if (coords.x >= 0 && coords.x < gridWidth && coords.y >= 0 && coords.y < gridHeight)
            {
                denseGrid[coords.x, coords.y] = kvp.Value;
            }
        }
        
        sparseGrid.Clear();
        useDenseMode = true;
    }
    
    public void SetGridObject(int x, int y, Cluster cluster)
    {
        if (useDenseMode)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                denseGrid[x, y] = cluster;
            }
        }
        else
        {
            string key = $"{x},{y}";
            if (cluster != null)
            {
                sparseGrid[key] = cluster;
            }
            else
            {
                sparseGrid.Remove(key);
            }
        }
    }
    
    public Cluster GetGridObject(int x, int y)
    {
        if (useDenseMode)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                return denseGrid[x, y];
            }
        }
        else
        {
            string key = $"{x},{y}";
            sparseGrid.TryGetValue(key, out Cluster cluster);
            return cluster;
        }
        return null;
    }
    
    private (int x, int y) ParseGridKey(string key)
    {
        string[] parts = key.Split(',');
        if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
        {
            return (x, y);
        }
        return (-1, -1);
    }
}
```

### Memory Management
```csharp
// Clean up spawned objects
private List<Cluster> spawnedObjects = new List<Cluster>();

public void CleanupSpawnedObjects()
{
    foreach (var cluster in spawnedObjects)
    {
        if (cluster != null && cluster.IsValid)
        {
            cluster.Destroy();
        }
    }
    spawnedObjects.Clear();
    placedObjects.Clear();
    
    Log.Write(LogLevel.Info, "Cleaned up all spawned objects");
}

// Automatic cleanup on script shutdown
public override void OnDestroy()
{
    CleanupSpawnedObjects();
    base.OnDestroy();
}
```

## Advanced Grid Patterns

### Hexagonal Grid
```csharp
private Vector HexGridToWorld(int q, int r)
{
    float x = GridSize * (3.0f/2.0f * q);
    float z = GridSize * (Math.Sqrt(3.0f)/2.0f * q + Math.Sqrt(3.0f) * r);
    return new Vector(x, 0, z);
}
```

### Circular Grid Pattern
```csharp
private void SpawnInCircle(ClusterResource resource, Vector center, float radius, int count)
{
    for (int i = 0; i < count; i++)
    {
        float angle = (2.0f * Math.PI * i) / count;
        Vector position = center + new Vector(
            Math.Cos(angle) * radius,
            0,
            Math.Sin(angle) * radius
        );
        
        Quaternion rotation = Quaternion.FromEulerAngles(0, angle + Math.PI/2, 0);
        SafeCreateCluster(resource, position, rotation);
    }
}
```

### 3D Grid (Voxel-style)
```csharp
private void Create3DGrid(int width, int height, int depth)
{
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector position = new Vector(x * GridSize, y * GridSize, z * GridSize);
                // Spawn based on some pattern or data
                if (ShouldSpawnAt(x, y, z))
                {
                    SafeCreateCluster(GetBlockType(x, y, z), position, Quaternion.Identity);
                }
            }
        }
    }
}
```

This guide provides everything needed to implement sophisticated object spawning and grid-based layouts in Sansar, from basic positioning to complex game systems like Tetris or chess.
