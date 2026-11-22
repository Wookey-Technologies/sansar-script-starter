/* This example is licensed under the Creative Commons Attribution 4.0 International License.
 * Copyright 2026 Sansar, Inc. https://creativecommons.org/licenses/by/4.0/
 */

using System;
using System.Collections.Generic;
using Sansar;
using Sansar.Script;
using Sansar.Simulation;

/// <summary>Demonstrates camera shake with chat commands on the default channel.</summary>
/// <remarks>Use /shake rumble, jitter, sway, impact, near, engine or stop.
/// Engine toggles a continuous rumble for the sender; stop clears this script's shakes for everyone.
/// An optional CameraComponent on this object fixes the view for current and future users.
/// Player comfort settings and VR limits can reduce or suppress the motion.</remarks>
public class CameraShakeExample : SceneObjectScript
{
    private CameraComponent camera;
    private readonly Dictionary<SessionId, CameraShakeHandle> engines =
        new Dictionary<SessionId, CameraShakeHandle>();

    /// <summary>Sets up chat commands and the optional fixed camera.</summary>
    public override void Init()
    {
        ObjectPrivate.TryGetFirstComponent(out camera);
        ScenePrivate.User.Subscribe(User.AddUser, OnUserAdded);
        ScenePrivate.User.Subscribe(User.RemoveUser, OnUserRemoved);
        ScenePrivate.Chat.Subscribe(Chat.DefaultChannel, OnChat, true);

        // Script reloads also need to set up users who are already in the world.
        foreach (AgentPrivate agent in ScenePrivate.GetAgents())
        {
            SetCamera(agent);
        }

        // The Debug local-source live test requires this marker from the real body.
        Log.Write("CameraShakeExample ready (real shake body): /shake rumble, jitter, sway, impact, near, engine, stop");
    }

    private void SetCamera(AgentPrivate agent)
    {
        if (camera != null && agent != null && agent.IsValid)
        {
            agent.Client.SetActiveCamera(camera, ScriptCameraControlMode.AgentControlOnly);
        }
    }

    private void OnUserAdded(UserData data)
    {
        SetCamera(ScenePrivate.FindAgent(data.User));
    }

    private void OnUserRemoved(UserData data)
    {
        // Leaving the world ends that player's shake; discard the old session's handle.
        engines.Remove(data.User);
    }

    private bool StopEngine(SessionId sessionId)
    {
        CameraShakeHandle engine;
        if (!engines.TryGetValue(sessionId, out engine))
        {
            return false;
        }

        engine.Stop(0.25f);
        engines.Remove(sessionId);
        return true;
    }

    private void OnChat(ChatData data)
    {
        string[] words = data.Message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2 || !words[0].Equals("/shake", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // SourceId identifies the sender, rather than the object running this script.
        AgentPrivate agent = ScenePrivate.FindAgent(data.SourceId);
        if (agent == null || !agent.IsValid)
        {
            return;
        }
        SessionId sessionId = agent.AgentInfo.SessionId;

        switch (words[1].ToLowerInvariant())
        {
            case "rumble":
                // Presets are mutable: edit before starting. Starts snapshot the settings.
                CameraShakeSettings rumble = CameraShakeSettings.Earthquake();
                rumble.Direction = new Vector(1, 0.5f, 0);
                agent.Client.ShakeCamera(rumble);
                break;

            case "jitter":
                agent.Client.ShakeCamera(new CameraShakeSettings
                {
                    Pattern = CameraShakePattern.Jitter,
                    Magnitude = 0.25f,
                    Duration = 1.0f
                    // Frequency 0 uses Jitter's default of 20 Hz.
                });
                break;

            case "sway":
                // Long side-to-side motion for the fixed-camera pixel-displacement test.
                agent.Client.ShakeCamera(new CameraShakeSettings
                {
                    Pattern = CameraShakePattern.Sway,
                    Magnitude = 0.6f,
                    Frequency = 2.0f,
                    Duration = 6.0f,
                    Direction = new Vector(1, 0, 0),
                    FadeOut = 0.3f
                });
                break;

            case "impact":
                // Landing: one downward kick. Duration 0 would still give just one kick.
                agent.Client.ShakeCamera(CameraShakeSettings.Impact(new Vector(0, -1, 0)));
                break;

            case "near":
                // Only players present now receive this explosion. Their camera distance
                // from this object's world position controls falloff to zero at 20 meters.
                ScenePrivate.ShakeCameraAtPosition(ObjectPrivate.Position, 20.0f,
                    CameraShakeSettings.Explosion());
                break;

            case "engine":
                // Send again to stop; separate sessions retain separate handles.
                if (!StopEngine(sessionId))
                {
                    engines[sessionId] = agent.Client.ShakeCamera(CameraShakeSettings.EngineRumble());
                }
                break;

            case "stop":
                StopEngine(sessionId);
                // Both stop-all scopes affect only shakes requested by this script.
                agent.Client.StopAllCameraShakes();
                ScenePrivate.StopAllCameraShakes();
                engines.Clear();
                break;
        }
    }
}
