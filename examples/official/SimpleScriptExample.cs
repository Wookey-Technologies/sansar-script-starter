/* (C) Linden Research, Inc. All Rights Reserved. */

using Sansar.Script;
using Sansar.Simulation;

// Updated from deprecated SimpleScript to SceneObjectScript
public class SimpleScriptExample : SceneObjectScript
{
    public override void Init()
    {
        // SceneObjectScript uses standard event subscription instead of RegisterSimpleScriptEvent
        Log.Write(LogLevel.Info, "SimpleScriptExample initialized");
    }
}
