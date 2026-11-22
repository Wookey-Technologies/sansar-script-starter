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
 * Listens for a "/time" command that prints the scene's time of day, sets the clock to an hour such as
 * "/time 18.5" for half past six in the evening, or changes how the clock runs with "/time off",
 * "/time fixed" or "/time cycle".
 */
using Sansar.Simulation;
using Sansar.Script;
using System;

// To get full access to the Scene API a script must extend from SceneObjectScript
public class TimeOfDayExample : SceneObjectScript
{
    public override void Init()
    {
        // Set up a callback to listen to the default chat channel for all events
        ScenePrivate.Chat.Subscribe(Chat.DefaultChannel, OnChat, true);
    }

    private void OnChat(ChatData data)
    {
        // Parse the chat message into an array of strings and look for a "/time" command
        var cmds = data.Message.Split(new Char[] { ' ' });
        if (cmds[0] == "/time")
        {
            if (cmds.Length > 1)
            {
                // A number sets the scene clock in hours, from 0 at midnight up to 24
                float hours;
                if (float.TryParse(cmds[1], out hours))
                {
                    ScenePrivate.Chat.MessageAllUsers("Setting the time of day to: " + hours);
                    ScenePrivate.SetTimeOfDay(hours);
                }
                // A mode name changes how the clock runs
                else if (cmds[1] == "off")
                {
                    ScenePrivate.Chat.MessageAllUsers("Turning the time of day off.");
                    ScenePrivate.SetTimeOfDayMode(TimeOfDayMode.Off);
                }
                else if (cmds[1] == "fixed")
                {
                    ScenePrivate.Chat.MessageAllUsers("Holding the time of day where it is.");
                    ScenePrivate.SetTimeOfDayMode(TimeOfDayMode.Fixed);
                }
                else if (cmds[1] == "cycle")
                {
                    ScenePrivate.Chat.MessageAllUsers("Starting the day and night cycle.");
                    ScenePrivate.SetTimeOfDayMode(TimeOfDayMode.Cycle);
                }
            }
            // If no additional parameter was specified, print out the scene's time of day and mode
            else
            {
                float hours = ScenePrivate.GetTimeOfDay();
                int hour = (int)hours;
                int minute = (int)((hours - hour) * 60.0f);
                ScenePrivate.Chat.MessageAllUsers("The time of day is " + hour.ToString("00") + ":" + minute.ToString("00") + " (" + ScenePrivate.GetTimeOfDayMode() + ")");
            }
        }
    }
}
