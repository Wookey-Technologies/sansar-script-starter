# Sansar Material Tint and Emissivity Guide

This comprehensive guide covers everything you need to know about setting material properties in Sansar, specifically focusing on tint (color) and emissivity (glow) effects for objects.

## Table of Contents
1. [Material System Overview](#material-system-overview)
2. [Accessing Materials](#accessing-materials)
3. [Material Properties](#material-properties)
4. [Setting Tint (Color)](#setting-tint-color)
5. [Setting Emissivity](#setting-emissivity)
6. [Animation and Interpolation](#animation-and-interpolation)
7. [Complete Examples](#complete-examples)
8. [Best Practices](#best-practices)

## Material System Overview

Sansar's material system allows you to modify visual properties of 3D objects at runtime through the `RenderMaterial` and `MaterialProperties` APIs.

### Key Components
- **MeshComponent**: Contains the 3D geometry and materials
- **RenderMaterial**: Individual material on a mesh
- **MaterialProperties**: Properties like tint, emissivity, brightness, etc.

### Prerequisites
- Object must have a **MeshComponent**
- Mesh must be **scriptable** (`mesh.IsScriptable == true`)
- Materials must support the properties you want to modify

## Accessing Materials

### Basic Material Access Pattern
```csharp
public class MaterialController : SceneObjectScript
{
    public override void Init()
    {
        // Get the mesh component
        if (!ObjectPrivate.TryGetFirstComponent(out MeshComponent mesh))
        {
            Log.Write(LogLevel.Error, "No MeshComponent found!");
            return;
        }

        // Check if mesh is scriptable
        if (!mesh.IsScriptable)
        {
            Log.Write(LogLevel.Error, "Mesh is not scriptable - cannot modify materials");
            return;
        }

        // Get all materials on the mesh
        var materials = mesh.GetRenderMaterials();
        foreach (var material in materials)
        {
            Log.Write(LogLevel.Info, $"Found material: {material.Name}");
            
            // Check material capabilities
            if (material.HasTint)
                Log.Write(LogLevel.Info, "Material supports tinting");
            if (material.HasEmissiveIntensity)
                Log.Write(LogLevel.Info, "Material supports emissive intensity");
        }
    }
}
```

### Accessing Materials on Spawned Objects
```csharp
// After spawning with ScenePrivate.CreateCluster
ScenePrivate.CreateCluster(clusterResource, position, rotation, Vector.Zero,
    (ScenePrivate.CreateClusterData data) =>
    {
        if (data.Success && data.ClusterReference != null)
        {
            // Access materials on spawned objects
            foreach (var objectPrivate in data.ClusterReference.GetObjectPrivates())
            {
                if (objectPrivate.TryGetFirstComponent(out MeshComponent mesh))
                {
                    if (mesh.IsScriptable)
                    {
                        var materials = mesh.GetRenderMaterials();
                        // Now you can modify materials
                        ModifyMaterials(materials);
                    }
                }
            }
        }
    });
```

## Material Properties

### Available Properties
```csharp
MaterialProperties props = material.GetProperties();

// Color and appearance
props.Tint;              // Color (RGBA)
props.EmissiveIntensity; // Float (glow intensity)
props.Brightness;        // Float (overall brightness)
props.Absorption;        // Float (light absorption)

// Animation and effects
props.FlipbookFrame;     // Float (for animated textures)

// Apply changes
material.SetProperties(props);
```

### Property Capabilities Check
```csharp
// Always check if material supports the property before using
if (material.HasTint)
{
    var props = material.GetProperties();
    props.Tint = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Red
    material.SetProperties(props);
}

if (material.HasEmissiveIntensity)
{
    var props = material.GetProperties();
    props.EmissiveIntensity = 5.0f;
    material.SetProperties(props);
}
```

## Setting Tint (Color)

### Basic Tint Setting
```csharp
private void SetObjectTint(MeshComponent mesh, Color newColor)
{
    if (!mesh.IsScriptable) return;

    foreach (var material in mesh.GetRenderMaterials())
    {
        if (material.HasTint)
        {
            var props = material.GetProperties();
            props.Tint = newColor;
            material.SetProperties(props);
        }
    }
}

// Usage examples
SetObjectTint(mesh, Color.Red);                                    // Solid red
SetObjectTint(mesh, new Color(0.5f, 0.8f, 0.2f, 1.0f));          // Custom RGB
SetObjectTint(mesh, new Color(1.0f, 1.0f, 1.0f, 0.5f));          // Semi-transparent white
```

### Color Creation Methods
```csharp
// Predefined colors
Color red = Color.Red;
Color blue = Color.Blue;
Color green = Color.Green;
Color white = Color.White;
Color black = Color.Black;

// Custom colors (R, G, B, A - all values 0.0 to 1.0)
Color orange = new Color(1.0f, 0.5f, 0.0f, 1.0f);
Color purple = new Color(0.5f, 0.0f, 0.8f, 1.0f);

// Random colors
Color randomColor = Color.Random();

// From string (if parsing user input)
if (Color.TryParse("(1.0,0.5,0.0,1.0)", out Color parsedColor))
{
    // Use parsedColor
}
```

### Advanced Tint Operations
```csharp
// Copy tint from one object to another
private void CopyTint(MeshComponent sourceMesh, MeshComponent targetMesh)
{
    var sourceMaterials = sourceMesh.GetRenderMaterials();
    var targetMaterials = targetMesh.GetRenderMaterials();

    for (int i = 0; i < Math.Min(sourceMaterials.Count, targetMaterials.Count); i++)
    {
        if (sourceMaterials[i].HasTint && targetMaterials[i].HasTint)
        {
            var sourceProps = sourceMaterials[i].GetProperties();
            var targetProps = targetMaterials[i].GetProperties();
            
            targetProps.Tint = sourceProps.Tint;
            targetMaterials[i].SetProperties(targetProps);
        }
    }
}

// Random tinting
private void RandomTintObject(MeshComponent mesh)
{
    var random = new System.Random();
    
    foreach (var material in mesh.GetRenderMaterials())
    {
        if (material.HasTint)
        {
            var props = material.GetProperties();
            props.Tint = new Color(
                (float)random.NextDouble(),  // Random red
                (float)random.NextDouble(),  // Random green
                (float)random.NextDouble(),  // Random blue
                1.0f                         // Full opacity
            );
            material.SetProperties(props);
        }
    }
}
```

## Setting Emissivity

### Basic Emissive Setting
```csharp
private void SetObjectEmissive(MeshComponent mesh, float intensity)
{
    if (!mesh.IsScriptable) return;

    foreach (var material in mesh.GetRenderMaterials())
    {
        if (material.HasEmissiveIntensity)
        {
            var props = material.GetProperties();
            props.EmissiveIntensity = intensity;
            material.SetProperties(props);
        }
    }
}

// Usage examples
SetObjectEmissive(mesh, 0.0f);   // No glow
SetObjectEmissive(mesh, 1.0f);   // Subtle glow
SetObjectEmissive(mesh, 5.0f);   // Bright glow
SetObjectEmissive(mesh, 10.0f);  // Very bright glow
```

### Emissive Intensity Guidelines
```csharp
// Recommended intensity ranges
private const float EMISSIVE_OFF = 0.0f;
private const float EMISSIVE_SUBTLE = 1.0f;
private const float EMISSIVE_MODERATE = 5.0f;
private const float EMISSIVE_BRIGHT = 10.0f;
private const float EMISSIVE_VERY_BRIGHT = 20.0f;
private const float EMISSIVE_MAXIMUM = 30.0f; // Avoid going higher

// Smart intensity adjustment
private void AdjustEmissiveIntensity(RenderMaterial material, float adjustment)
{
    if (!material.HasEmissiveIntensity) return;

    var props = material.GetProperties();
    float currentIntensity = props.EmissiveIntensity;
    
    if (currentIntensity > 20.0f)
    {
        // High intensity materials: add adjustment but cap at 30
        props.EmissiveIntensity = Math.Min(currentIntensity + adjustment, 30.0f);
    }
    else
    {
        // Low intensity materials: set to specific level
        props.EmissiveIntensity = Math.Max(adjustment, 0.0f);
    }
    
    material.SetProperties(props);
}
```

## Animation and Interpolation

### SetProperties with Animation
```csharp
// Immediate change (no animation)
material.SetProperties(props);

// Animated change
material.SetProperties(props, duration, interpolationMode);

// Animated change with callback
material.SetProperties(props, duration, interpolationMode, (result) =>
{
    if (result.Success)
    {
        Log.Write(LogLevel.Info, "Animation completed successfully");
    }
    else
    {
        Log.Write(LogLevel.Error, $"Animation failed: {result.Message}");
    }
});
```

### Interpolation Modes
```csharp
public enum InterpolationMode
{
    Linear,      // Constant rate of change
    EaseIn,      // Slow start, fast finish
    EaseOut,     // Fast start, slow finish
    Smoothstep,  // Smooth acceleration and deceleration
    Step         // Instant change halfway through duration
}

// Helper function to parse interpolation modes from strings
private InterpolationMode ParseInterpolationMode(string mode)
{
    switch (mode.ToLower())
    {
        case "linear": return InterpolationMode.Linear;
        case "easein": return InterpolationMode.EaseIn;
        case "easeout": return InterpolationMode.EaseOut;
        case "smoothstep": return InterpolationMode.Smoothstep;
        case "step": return InterpolationMode.Step;
        default:
            Log.Write(LogLevel.Warning, $"Unknown interpolation mode: {mode}");
            return InterpolationMode.Linear;
    }
}
```

### Complex Animation Examples
```csharp
// Pulse effect (bright -> dim -> bright)
private void CreatePulseEffect(RenderMaterial material, float duration)
{
    if (!material.HasEmissiveIntensity) return;

    var props = material.GetProperties();
    float originalIntensity = props.EmissiveIntensity;
    float targetIntensity = Math.Max(originalIntensity + 10.0f, 10.0f);
    
    // Phase 1: Increase intensity
    props.EmissiveIntensity = targetIntensity;
    material.SetProperties(props, duration / 2.0f, InterpolationMode.EaseOut, (result) =>
    {
        if (result.Success)
        {
            // Phase 2: Return to original
            var returnProps = material.GetProperties();
            returnProps.EmissiveIntensity = originalIntensity;
            material.SetProperties(returnProps, duration / 2.0f, InterpolationMode.EaseIn);
        }
    });
}

// Color fade effect
private void FadeToColor(RenderMaterial material, Color targetColor, float duration)
{
    if (!material.HasTint) return;

    var props = material.GetProperties();
    props.Tint = targetColor;
    material.SetProperties(props, duration, InterpolationMode.Smoothstep);
}

// Rainbow cycle effect
private void StartRainbowCycle(MeshComponent mesh, float cycleDuration)
{
    StartCoroutine(() => RainbowCycleCoroutine(mesh, cycleDuration));
}

private void RainbowCycleCoroutine(MeshComponent mesh, float cycleDuration)
{
    Color[] rainbowColors = {
        Color.Red,
        new Color(1.0f, 0.5f, 0.0f, 1.0f), // Orange
        new Color(1.0f, 1.0f, 0.0f, 1.0f), // Yellow
        Color.Green,
        new Color(0.0f, 0.5f, 1.0f, 1.0f), // Blue
        new Color(0.5f, 0.0f, 1.0f, 1.0f), // Purple
    };

    int colorIndex = 0;
    float stepDuration = cycleDuration / rainbowColors.Length;

    while (true)
    {
        foreach (var material in mesh.GetRenderMaterials())
        {
            if (material.HasTint)
            {
                var props = material.GetProperties();
                props.Tint = rainbowColors[colorIndex];
                material.SetProperties(props, stepDuration, InterpolationMode.Linear);
            }
        }

        colorIndex = (colorIndex + 1) % rainbowColors.Length;
        Wait(TimeSpan.FromSeconds(stepDuration));
    }
}
```

## Complete Examples

### Example 1: Interactive Color Changer
```csharp
public class InteractiveColorChanger : SceneObjectScript
{
    [DefaultValue("Click to Change Color")]
    public Interaction ColorInteraction;

    private MeshComponent mesh;
    private Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Purple };
    private int currentColorIndex = 0;

    public override void Init()
    {
        if (!ObjectPrivate.TryGetFirstComponent(out mesh))
        {
            Log.Write(LogLevel.Error, "No mesh component found");
            return;
        }

        if (!mesh.IsScriptable)
        {
            Log.Write(LogLevel.Error, "Mesh is not scriptable");
            return;
        }

        ColorInteraction.Subscribe(OnColorChange);
    }

    private void OnColorChange(InteractionData data)
    {
        Color newColor = colors[currentColorIndex];
        currentColorIndex = (currentColorIndex + 1) % colors.Length;

        foreach (var material in mesh.GetRenderMaterials())
        {
            if (material.HasTint)
            {
                var props = material.GetProperties();
                props.Tint = newColor;
                material.SetProperties(props, 1.0f, InterpolationMode.Smoothstep);
            }
        }

        AgentPrivate agent = ScenePrivate.FindAgent(data.AgentId);
        agent?.SendChat($"Changed color to {newColor}");
    }
}
```

### Example 2: Emissive Pulse on Spawn
```csharp
public class EmissivePulseSpawner : SceneObjectScript
{
    [Tooltip("Object to spawn")]
    public ClusterResource SpawnObject;

    [Tooltip("Pulse duration in seconds")]
    [DefaultValue(2.0f)]
    public float PulseDuration = 2.0f;

    public override void Init()
    {
        // Spawn object with pulse effect
        Vector spawnPos = ObjectPrivate.Position + Vector.Up * 2;
        
        ScenePrivate.CreateCluster(SpawnObject, spawnPos, Quaternion.Identity, Vector.Zero,
            (ScenePrivate.CreateClusterData data) =>
            {
                if (data.Success && data.ClusterReference != null)
                {
                    ApplyPulseEffect(data.ClusterReference);
                }
            });
    }

    private void ApplyPulseEffect(Cluster cluster)
    {
        foreach (var objectPrivate in cluster.GetObjectPrivates())
        {
            if (objectPrivate.TryGetFirstComponent(out MeshComponent mesh))
            {
                if (mesh.IsScriptable)
                {
                    foreach (var material in mesh.GetRenderMaterials())
                    {
                        if (material.HasEmissiveIntensity)
                        {
                            CreatePulseEffect(material, PulseDuration);
                        }
                    }
                }
            }
        }
    }

    private void CreatePulseEffect(RenderMaterial material, float duration)
    {
        var props = material.GetProperties();
        float originalIntensity = props.EmissiveIntensity;
        float pulseIntensity = Math.Max(originalIntensity + 15.0f, 15.0f);

        // Pulse up
        props.EmissiveIntensity = pulseIntensity;
        material.SetProperties(props, duration / 2.0f, InterpolationMode.EaseOut, (result) =>
        {
            if (result.Success)
            {
                // Pulse down
                var returnProps = material.GetProperties();
                returnProps.EmissiveIntensity = originalIntensity;
                material.SetProperties(returnProps, duration / 2.0f, InterpolationMode.EaseIn);
            }
        });
    }
}
```

### Example 3: Material Property Manager
```csharp
public class MaterialPropertyManager : SceneObjectScript
{
    public static void SetTint(Cluster cluster, Color tint)
    {
        foreach (var objectPrivate in cluster.GetObjectPrivates())
        {
            if (objectPrivate.TryGetFirstComponent(out MeshComponent mesh))
            {
                SetMeshTint(mesh, tint);
            }
        }
    }

    public static void SetEmissive(Cluster cluster, float intensity)
    {
        foreach (var objectPrivate in cluster.GetObjectPrivates())
        {
            if (objectPrivate.TryGetFirstComponent(out MeshComponent mesh))
            {
                SetMeshEmissive(mesh, intensity);
            }
        }
    }

    public static void SetMeshTint(MeshComponent mesh, Color tint)
    {
        if (!mesh.IsScriptable) return;

        foreach (var material in mesh.GetRenderMaterials())
        {
            if (material.HasTint)
            {
                var props = material.GetProperties();
                props.Tint = tint;
                material.SetProperties(props);
            }
        }
    }

    public static void SetMeshEmissive(MeshComponent mesh, float intensity)
    {
        if (!mesh.IsScriptable) return;

        foreach (var material in mesh.GetRenderMaterials())
        {
            if (material.HasEmissiveIntensity)
            {
                var props = material.GetProperties();
                props.EmissiveIntensity = intensity;
                material.SetProperties(props);
            }
        }
    }

    public static List<Color> GetMeshTints(MeshComponent mesh)
    {
        var tints = new List<Color>();
        
        if (mesh.IsScriptable)
        {
            foreach (var material in mesh.GetRenderMaterials())
            {
                if (material.HasTint)
                {
                    var props = material.GetProperties();
                    tints.Add(props.Tint);
                }
            }
        }
        
        return tints;
    }
}
```

## Best Practices

### Performance Considerations
```csharp
// Cache material references when possible
private List<RenderMaterial> cachedMaterials;

public override void Init()
{
    if (ObjectPrivate.TryGetFirstComponent(out MeshComponent mesh))
    {
        cachedMaterials = mesh.GetRenderMaterials().ToList();
    }
}

// Batch material changes
private void BatchUpdateMaterials(Color tint, float emissive)
{
    foreach (var material in cachedMaterials)
    {
        var props = material.GetProperties();
        bool changed = false;

        if (material.HasTint)
        {
            props.Tint = tint;
            changed = true;
        }

        if (material.HasEmissiveIntensity)
        {
            props.EmissiveIntensity = emissive;
            changed = true;
        }

        if (changed)
        {
            material.SetProperties(props);
        }
    }
}
```

### Error Handling
```csharp
private bool SafeSetMaterialProperty(RenderMaterial material, Action<MaterialProperties> modifier)
{
    try
    {
        var props = material.GetProperties();
        modifier(props);
        material.SetProperties(props);
        return true;
    }
    catch (Exception ex)
    {
        Log.Write(LogLevel.Error, $"Failed to set material property: {ex.Message}");
        return false;
    }
}

// Usage
SafeSetMaterialProperty(material, props => props.Tint = Color.Red);
SafeSetMaterialProperty(material, props => props.EmissiveIntensity = 5.0f);
```

### Validation
```csharp
private bool ValidateMaterialCapabilities(MeshComponent mesh, out string issues)
{
    issues = "";
    
    if (!mesh.IsScriptable)
    {
        issues = "Mesh is not scriptable";
        return false;
    }

    var materials = mesh.GetRenderMaterials();
    if (materials.Count == 0)
    {
        issues = "No materials found on mesh";
        return false;
    }

    bool hasTintable = materials.Any(m => m.HasTint);
    bool hasEmissive = materials.Any(m => m.HasEmissiveIntensity);

    if (!hasTintable && !hasEmissive)
    {
        issues = "No materials support tinting or emissive properties";
        return false;
    }

    return true;
}
```

This guide provides everything needed to effectively control material tint and emissivity in Sansar, from basic property setting to complex animated effects.
