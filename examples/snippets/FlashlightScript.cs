// © 2019 Linden Research, Inc.
//
// A flashlight: pick up the object and press the "PrimaryAction" button (F by
// default) to toggle its light on and off. The object needs a grabbable
// RigidBodyComponent and a LightComponent with "Scriptable" set to "On".

using Sansar;
using Sansar.Script;
using Sansar.Simulation;

public class FlashlightScript : SceneObjectScript
{
    [Tooltip("The color of the flashlight beam when on")]
    [DefaultValue(1.0f, 1.0f, 0.9f, 1.0f)]
    public Color LightColor;

    [DefaultValue(10.0f)]
    [Range(0.0f, 50.0f)]
    public float LightIntensity;

    LightComponent _light = null;
    RigidBodyComponent _rb = null;
    IEventSubscription _commandSubscription = null;
    bool _lightOn = false;

    public override void Init()
    {
        if (!ObjectPrivate.TryGetFirstComponent(out _rb))
        {
            Log.Write(LogLevel.Error, "FlashlightScript can't find a RigidBodyComponent!");
            return;
        }

        if (!ObjectPrivate.TryGetFirstComponent(out _light) || !_light.IsScriptable)
        {
            Log.Write(LogLevel.Error, "FlashlightScript needs a scriptable LightComponent!");
            return;
        }

        // Start with the light off
        SetLight(false);

        _rb.SubscribeToHeldObject(HeldObjectEventType.Grab, (HeldObjectData holdData) =>
        {
            try
            {
                AgentPrivate agent = ScenePrivate.FindAgent(holdData.HeldObjectInfo.SessionId);

                if (agent != null && agent.IsValid)
                {
                    _commandSubscription = agent.Client.SubscribeToCommand("PrimaryAction", CommandAction.Pressed, (CommandData command) =>
                    {
                        SetLight(!_lightOn);
                    },
                    (canceledData) => { });
                }
            }
            catch
            {
                // The agent can log out between FindAgent and Client access;
                // an unhandled exception here would kill the script.
            }
        });

        _rb.SubscribeToHeldObject(HeldObjectEventType.Release, (HeldObjectData holdData) =>
        {
            if (_commandSubscription != null)
            {
                _commandSubscription.Unsubscribe();
                _commandSubscription = null;
            }

            SetLight(false);
        });
    }

    void SetLight(bool on)
    {
        _lightOn = on;

        try
        {
            if (on)
                _light.SetColorAndIntensity(LightColor, LightIntensity);
            else
                _light.SetColorAndIntensity(Color.Black, 0.0f);
        }
        catch
        {
            // The light can become invalid if the object is being destroyed.
        }
    }
}
