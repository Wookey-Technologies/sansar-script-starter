/* This content is licensed under the terms of the Creative Commons Attribution 4.0 International License.
 * When using this content, you must:
 * •    Acknowledge that the content is from the Sansar Knowledge Base.
 * •    Include our copyright notice: "© 2025 Sansar, Inc."
 * •    Indicate that the content is licensed under the Creative Commons Attribution-Share Alike 4.0 International License.
 * •    Include the URL for, or link to, the license summary at https://creativecommons.org/licenses/by-sa/4.0/deed.hi (and, if possible, to the complete license terms at https://creativecommons.org/licenses/by-sa/4.0/legalcode.
 * For example:
 * "This work uses content from the Sansar Knowledge Base. © 2018 Linden Research, Inc. Licensed under the Creative Commons Attribution 4.0 International License (license summary available at https://creativecommons.org/licenses/by/4.0/ and complete license terms available at https://creativecommons.org/licenses/by/4.0/legalcode)."
 */

// Attach to a trigger volume to scale agents that enter. Agents return to normal size on exit.

using Sansar.Simulation;
using Sansar.Script;
using System;

class AgentScaleExample : SceneObjectScript
{
    [DisplayName("Scale Factor")]
    [DefaultValue(2.0f)]
    public readonly float scaleFactor = 2.0f;

    [DisplayName("Transition Time")]
    [DefaultValue(1.0f)]
    public readonly float transitionTime = 1.0f;

    private RigidBodyComponent RigidBody = null;

    public override void Init()
    {
        if (ObjectPrivate.TryGetFirstComponent(out RigidBody) && RigidBody.IsTriggerVolume())
        {
            RigidBody.Subscribe(CollisionEventType.Trigger, OnTrigger);
        }
        else
        {
            Log.Write(LogLevel.Error, "Could not start " + GetType().Name + " because no trigger volume was found.");
        }
    }

    private void OnTrigger(CollisionData data)
    {
        try
        {
            AgentPrivate agent = ScenePrivate.FindAgent(data.HitComponentId.ObjectId);
            if (data.Phase == CollisionEventPhase.TriggerExit)
            {
                // Smoothly return to normal size
                agent.SetScaleFactorAnimated(1.0f, transitionTime);
            }
            else if (data.Phase == CollisionEventPhase.TriggerEnter)
            {
                // Smoothly scale to the configured size
                agent.SetScaleFactorAnimated(scaleFactor, transitionTime);
            }
        }
        catch (NullReferenceException nre) { Log.Write(LogLevel.Info, "NullReferenceException setting agent scale factor (maybe the user left): " + nre.Message); }
        catch (Exception e) { Log.Write(LogLevel.Error, "Exception setting agent scale factor: " + e.Message); }
    }
}
