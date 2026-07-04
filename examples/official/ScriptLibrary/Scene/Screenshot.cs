/* This content is licensed under the terms of the Creative Commons Attribution 4.0 International License.
 * When using this content, you must:
 *     Acknowledge that the content is from the Sansar Knowledge Base.
 *     Include our copyright notice: "© 2022 Sansar, Inc."
 *     Indicate that the content is licensed under the Creative Commons Attribution-Share Alike 4.0 International License.
 *     Include the URL for, or link to, the license summary at https://creativecommons.org/licenses/by-sa/4.0/deed.hi (and, if possible, to the complete license terms at https://creativecommons.org/licenses/by-sa/4.0/legalcode.
 * For example:
 * "This work uses content from the Sansar Knowledge Base. © 2022 Sansar, Inc. Licensed under the Creative Commons Attribution 4.0 International License (license summary available at https://creativecommons.org/licenses/by/4.0/ and complete license terms available at https://creativecommons.org/licenses/by/4.0/legalcode)."
 */

using Sansar;
using Sansar.Script;
using Sansar.Simulation;
using System;
using System.Linq;
using System.Collections.Generic;


namespace ScriptLibrary
{
    [Tooltip("Takes screenshots from a camera upon receipt of an event.")]
    [DisplayName("Take Screenshot")]
    public class Screenshot : LibraryBase
    {
        #region EditorProperties
        [Tooltip("Set the View Camera to take the screenshot from, if no camera set, it will use the current camera.\nTo set the camera, select the camera from the object panel and 'copy reference' from the context menu, then paste (ctrl-v) here.")]
        [DisplayName("Screenshot Camera")]
        public readonly CameraComponent Camera;

        [Tooltip("Event name to trigger the screenshot. Can be a comma separated list of event names.")]
        [DefaultValue("screenshot")]
        [DisplayName("-> Take Screenshot Event")]
        public readonly string TakeScreenshotEvent;

        [Tooltip("Event that is sent upon taking the screenshot. Occurs after the waiting period, if there is one.")]
        [DefaultValue("screenshot_taken")]
        [DisplayName("Screenshot Taken Event ->")]
        public readonly string ScreenshotTakenEvent;
        
        [Tooltip("Number of seconds to wait before taking picture. Will show a countdown and the view through the camera, allowing the user to pose.")]
        [DisplayName("Timer")]
        public readonly int Timer;

        [Tooltip("If true, wait for user to press a certain key before taking the picture. Overrides timer.")]
        [DisplayName("Wait for Keypress")]
        public readonly bool WaitForKeypress;

        [Tooltip("The message displayed to the user while they are getting ready to take a screenshot. Should instruct them to press the appropriate key.")]
        [DefaultValue("Pose your character, then press \"Enter\" to take a screenshot.")]
        [DisplayName("Confirmation Message")]
        public readonly string ConfirmationMessage;

        [Tooltip("The key pressed to confirm the taking of the screenshot, if WaitForKeypress is true. See the documentation for Sansar.Simulation.CommandData")]
        [DefaultValue("Confirm")]
        [DisplayName("Confirmation Command")]
        public readonly string ConfirmationCommand;

        [Tooltip("The maximum time to wait for the user to press a key before cancelling.")]
        [DefaultValue(10.0)]
        [DisplayName("Maximum Wait Time")]
        public readonly double MaxWaitTime;

        [Tooltip("Enable responding to events for this script. Can be a comma separated list of event names.")]
        [DefaultValue("screenshot_enable")]
        [DisplayName("-> Enable")]
        public readonly string EnableEvent;

        [Tooltip("Disable responding to events for this script. Can be a comma separated list of event names.")]
        [DefaultValue("screenshot_disable")]
        [DisplayName("-> Disable")]
        public readonly string DisableEvent;

        [Tooltip(@"If StartEnabled is true then the script will respond to events when the scene is loaded
    If StartEnabled is false then the script will not respond to events until an (-> Enable) event is received.")]
        [DefaultValue(true)]
        [DisplayName("Start Enabled")]
        public readonly bool StartEnabled = true;
        #endregion

        private Action subscription = null;

        private SimpleData simpledata;

        private HashSet<SessionId> waiting;

        // Always call inside try/catch
        private void TakeScreenshot(AgentPrivate ap)
        {
            simpledata.AgentInfo = ap.AgentInfo;
            SendToAll(ScreenshotTakenEvent, simpledata);
            ap.Client.CaptureImage(Camera);
        }

        private void Countdown(SessionId sid)
        {
            AgentPrivate ap = ScenePrivate.FindAgent(sid);
            try
            {
                if(Camera != null)
                {
                    ap.Client.SetActiveCamera(Camera, ScriptCameraControlMode.AgentControlOnly);
                }
                for(int i = Timer; i > 0; --i)
                {
                    ap.Client.UI.HintText = i.ToString();
                    Wait(1.0);
                }
                ap.Client.UI.HintText = "";
                TakeScreenshot(ap);
                ap.Client.ResetCamera();
            }
            catch{}
        }

        private void KeypressWait(SessionId sid)
        {
            AgentPrivate ap = ScenePrivate.FindAgent(sid);
            try
            {
                if(Camera != null)
                {
                    ap.Client.SetActiveCamera(Camera, ScriptCameraControlMode.AgentControlOnly);
                }
                ap.Client.UI.HintText = ConfirmationMessage;
                waiting.Add(sid);
                double acc = 0.0;
                while(waiting.Contains(sid))
                {
                    Wait(0.1);
                    acc += 0.1;
                    if(acc >= MaxWaitTime)
                    {
                        ap.Client.UI.HintText = "";
                        ap.Client.ResetCamera();
                        if(waiting.Contains(sid))
                            waiting.Remove(sid);
                        return;
                    }
                }
                ap.Client.UI.HintText = "";
                TakeScreenshot(ap);
                ap.Client.ResetCamera();
            }
            catch
            {
                if(waiting.Contains(sid))
                    waiting.Remove(sid);
            }
        }

        private void OnCommand(CommandData data)
        {
            if(waiting.Contains(data.SessionId))
                waiting.Remove(data.SessionId);
        }

        private void Subscribe(ScriptEventData sed)
        {
            if (subscription == null)
            {
                subscription = SubscribeToAll(TakeScreenshotEvent, (ScriptEventData data) =>
                {
                    SessionId sid = data.Data.AsInterface<ISimpleData>().AgentInfo.SessionId;
                    if(sid != SessionId.Invalid)
                    {
                        if(WaitForKeypress)
                        {
                            StartCoroutine(() => {KeypressWait(sid);});
                        }
                        else if(Timer > 0)
                        {
                            StartCoroutine(() => {Countdown(sid);});
                        }
                        else
                        {
                            AgentPrivate ap = ScenePrivate.FindAgent(sid);
                            try
                            {
                                TakeScreenshot(ap);
                            }catch{}
                        }
                    }
                });

            }
        }

        private void Unsubscribe(ScriptEventData sed)
        {

            if (subscription != null)
            {
                subscription();
                subscription = null;
            }
        }

        private void OnAddUser(SessionId sid)
        {
            AgentPrivate ap = ScenePrivate.FindAgent(sid);
            try
            {
                ap.Client.SubscribeToCommand(ConfirmationCommand, OnCommand, null);
            }catch{}
        }

    // SendToAll(FirstSendEvent, simpledata);

        protected override void SimpleInit()
        {

            bool initSuccess = true;

            if (!initSuccess) return;

            waiting = new HashSet<SessionId>();

            simpledata = new SimpleData(this);
            simpledata.SourceObjectId = ObjectPrivate.ObjectId;
            simpledata.AgentInfo = null;
            simpledata.ObjectId = ObjectPrivate.ObjectId;

            if (StartEnabled) Subscribe(null);

            foreach(AgentPrivate ap in ScenePrivate.GetAgents())
            {
                AgentInfo ai;

                try{ ai = ap.AgentInfo; } catch { continue; }

                OnAddUser(ai.SessionId);
            }

            ScenePrivate.User.Subscribe(User.AddUser, (UserData ud) => {OnAddUser(ud.User);});

            SubscribeToAll(EnableEvent, Subscribe);
            SubscribeToAll(DisableEvent, Unsubscribe);

        }
    }
}
