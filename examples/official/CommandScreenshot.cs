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

public class CommandExampleScreenShot : SceneObjectScript
{
    #region ScriptParameters
    [Tooltip(@"If the command has been subscribed to by the 'Subscribe Command', this will log the action to the script console.")]
    [DefaultValue("Screenshot")]
    [DisplayName("Action Command")]
    public readonly string ActionCommand;
    #endregion ScriptParameters

    IEventSubscription screenshotSubscription;
    public override void Init()
    {
        screenshotSubscription = ScenePrivate.User.Subscribe("Screenshot", OnScreenshot, true);
    }

    void OnScreenshot(UserData userData)
    {
        AgentPrivate agent = ScenePrivate.FindAgent(userData.User);
        agent.Client.CaptureImage(agent.Client.GetActiveCamera());
    }
}