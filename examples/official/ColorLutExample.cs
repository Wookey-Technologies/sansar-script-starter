/* This content is licensed under the terms of the Creative Commons Attribution 4.0 International License.
 * When using this content, you must:
 * •    Acknowledge that the content is from the Sansar Knowledge Base.
 * •    Include our copyright notice: "© 2022 Sansar, Inc."
 * •    Indicate that the content is licensed under the Creative Commons Attribution-Share Alike 4.0 International License.
 * •    Include the URL for, or link to, the license summary at https://creativecommons.org/licenses/by-sa/4.0/deed.hi (and, if possible, to the complete license terms at https://creativecommons.org/licenses/by-sa/4.0/legalcode.
 * For example:
 * "This work uses content from the Sansar Knowledge Base. © 2018 Linden Research, Inc. Licensed under the Creative Commons Attribution 4.0 International License (license summary available at https://creativecommons.org/licenses/by/4.0/ and complete license terms available at https://creativecommons.org/licenses/by/4.0/legalcode)."
 */

/* Register for the event that occurs when chat messages happen.
 * Listens for a "/lut" command that grades the whole scene with a LUT from the script properties,
 * "/lut all" or "/lut me" for the sender alone, and "/lut off" to go back to the scene settings LUT.
 * The LUT textures are chosen from the inventory in the scene editor, like a sound or a cluster.
 */
using Sansar.Simulation;
using Sansar.Script;
using System;

// To get full access to the Scene API a script must extend from SceneObjectScript
public class ColorLutExample : SceneObjectScript
{
    [DisplayName("Scene LUT")]
    [Tooltip("The colour grade applied to everyone with /lut all")]
    public TextureResource SceneLut;

    [DisplayName("Personal LUT")]
    [Tooltip("The colour grade applied to the sender alone with /lut me")]
    public TextureResource PersonalLut;

    public override void Init()
    {
        // Set up a callback to listen to the default chat channel for all events
        ScenePrivate.Chat.Subscribe(Chat.DefaultChannel, OnChat, true);
    }

    private void OnChat(ChatData data)
    {
        // Parse the chat message into an array of strings and look for a "/lut" command
        var cmds = data.Message.Split(new Char[] { ' ' });
        if (cmds[0] != "/lut" || cmds.Length < 2)
        {
            return;
        }

        AgentPrivate sender = ScenePrivate.FindAgent(data.SourceId);

        if (cmds[1] == "all")
        {
            // Everyone without a personal override sees this LUT, including users who join later
            ScenePrivate.Chat.MessageAllUsers("Grading the scene for everyone.");
            ScenePrivate.SetColorLut(SceneLut);
        }
        else if (cmds[1] == "me" && sender != null)
        {
            // Only the sender sees this LUT; it wins over the scene-wide one until cleared or they leave
            sender.SendChat("Grading the scene for you alone.");
            sender.SetColorLut(PersonalLut);
        }
        else if (cmds[1] == "off")
        {
            // Back to the LUT chosen in the scene settings for everyone, personal overrides included
            ScenePrivate.Chat.MessageAllUsers("Back to the scene settings LUT.");
            ScenePrivate.ClearColorLutOverrides();
            ScenePrivate.ClearColorLut();
        }
    }
}
