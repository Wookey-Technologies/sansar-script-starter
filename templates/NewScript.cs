// NewScript.cs - a starting skeleton for a Sansar script.
//
// Import into Sansar with: Import -> Script, then drag the script onto an object.
// Compile-check locally first: powershell -File tools\check.ps1 templates\NewScript.cs

using Sansar;
using Sansar.Script;
using Sansar.Simulation;
using System;

public class NewScript : SceneObjectScript
{
    // Public fields become editable properties in the scene editor (max 20).
    [Tooltip("Text shown when a user hovers over this object")]
    [DefaultValue("Click me!")]
    public Interaction MyInteraction;

    [DefaultValue(5.0f)]
    [Range(0.0f, 60.0f)]
    public float IntervalSeconds;

    public override void Init()
    {
        // React to clicks on this object.
        MyInteraction.Subscribe((InteractionData data) =>
        {
            AgentPrivate agent = ScenePrivate.FindAgent(data.AgentId);
            if (agent != null)
            {
                try
                {
                    agent.SendChat("Hello from NewScript!");
                }
                catch
                {
                    // The agent may have left the scene; never let an exception
                    // escape - unhandled exceptions kill the script permanently.
                }
            }
        });

        // Periodic work happens in a coroutine - there is no Update() in Sansar.
        StartCoroutine(PeriodicUpdate);
    }

    void PeriodicUpdate()
    {
        while (true)
        {
            Wait(TimeSpan.FromSeconds(IntervalSeconds));

            // Do periodic work here. Log output is visible in the debug
            // console (Ctrl+D), to the scene owner only.
            Log.Write("NewScript periodic tick");
        }
    }
}
