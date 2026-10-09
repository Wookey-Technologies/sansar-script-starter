/* (C) Linden Research, Inc. All Rights Reserved. */

using Sansar.Script;
using Sansar.Simulation;

public class LogExample : SceneObjectScript
{
    public override void Init()
    {
        // Basic log message
        Log.Write(LogLevel.Info, "LogExample initialized");

        // Log with format parameters
        var sampleValue = "example";
        Log.Write(LogLevel.Warning, "Sample value: {0}", sampleValue);

        // Simulate a recoverable error to show LogLevel.Error usage.
        var success = TryDoWork();
        if (!success)
        {
            Log.Write(LogLevel.Error, "The sample work loop reported a failure.");
        }
    }

    private bool TryDoWork()
    {
        // Placeholder for sample logic
        return false;
    }
}
