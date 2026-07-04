/* This content is licensed under the terms of the Creative Commons Attribution 4.0 International License.
 * When using this content, you must:
 * �    Acknowledge that the content is from the Sansar Knowledge Base.
 * �    Include our copyright notice: "2022 Sansar, Inc."
 * �    Indicate that the content is licensed under the Creative Commons Attribution-Share Alike 4.0 International License.
 * �    Include the URL for, or link to, the license summary at https://creativecommons.org/licenses/by-sa/4.0/deed.hi (and, if possible, to the complete license terms at https://creativecommons.org/licenses/by-sa/4.0/legalcode.
 * For example:
 * "This work uses content from the Sansar Knowledge Base. � 2022 Sansar, Inc." Licensed under the Creative Commons Attribution 4.0 International License (license summary available at https://creativecommons.org/licenses/by/4.0/ and complete license terms available at https://creativecommons.org/licenses/by/4.0/legalcode)."
 */

using Sansar.Script;
using Sansar.Simulation;
using System;



// A simple interaction script that makes the object clickable while showing some data on hover.
public class InteractionScreenshot : SceneObjectScript
{
    #region EditorProperties
    // This interaction will have a default prompt of "Click Me!"
    // The prompt can be changed in the object properties
    [DefaultValue("Click Me!")]
    public Interaction MyInteraction;

    [Tooltip("To set the camera, select the camera from the object panel and copy it (ctrl-c) then (ctrl-v) here!")]
    [DisplayName("Target Camera")]
    public readonly CameraComponent TargetCamera;
    #endregion

    // Track how many clicks
    public override void Init()
    {
        // Save the initial prompt. It may have been changed at edit time, and will be changed more when interacted with.
        string basePrompt = MyInteraction.GetPrompt();

        // Subscribe to interaction to receive the interaction events
        MyInteraction.Subscribe( (InteractionData idata) =>
        {
            AgentPrivate agent = ScenePrivate.FindAgent(idata.AgentId);
            agent.Client.CaptureImage(TargetCamera);
        });
    }


}