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
    [Tooltip("Allows users to spawn your custom reactions.")]
    [DisplayName("Reaction")]
    public class Reaction : LibraryBase
    {
        #region EditorProperties

        public readonly ThumbnailedClusterResource ReactionCluster;

        [Tooltip("The name of the reaction that users see when they go their reaction panel.")]
        [DefaultValue("Simple Reaction")]
        [DisplayName("Reaction Name")]
        public readonly string ReactionName;

        [Tooltip("The offset to where the reaction is spawned (in relation to the avatar's feet).")]
        [DefaultValue("<0,0,1.8>")]
        [DisplayName("Offset")]
        public readonly Vector Offset;

        [Tooltip("The duration that the ReactionCluster exists.")]
        [DefaultValue(3.0)]
        [DisplayName("Duration")]
        public double Duration;

        [Tooltip("The event triggered by the reaction")]
        [DefaultValue("reaction")]
        [DisplayName("Reaction ->")]
        public readonly string ReactionEvent;

        #endregion

        private string internalName;

        private SimpleData simpledata;

        private void OnReaction(ReactionData data)
        {
            if(data.Type == internalName)
            {
                Vector spawnPosition = ObjectPrivate.Position + Offset.Rotate(ObjectPrivate.Rotation);

                ScenePrivate.CreateClusterData clusterData = (ScenePrivate.CreateClusterData)WaitFor(ScenePrivate.CreateCluster,
                ReactionCluster.ClusterResource,
                data.Position + Offset.Rotate(data.Orientation),
                data.Orientation,
                Vector.Zero);

                if(clusterData.Success)
                {
                    StartCoroutine(() => {Wait(Duration); clusterData.ClusterReference.Destroy();});
                    SendToAll(ReactionEvent, simpledata);
                }
            }
        }

        protected override void SimpleInit()
        {

            bool initSuccess = true;

            if (ReactionCluster == null)
            {
                Log.Write(LogLevel.Error, "ReactionCluster is not set.");
                initSuccess = false;
            }

            internalName = String.Concat(ReactionName.Where(char.IsLetterOrDigit));

            if (internalName == "")
            {
                Log.Write(LogLevel.Error, "ReactionName invalid - must contain at least some alphanumeric characters.");
                initSuccess = false;
            }

            if (Duration <= 0.0f)
            {
                Log.Write(LogLevel.Warning, "Duration must be greater than 0. Setting to 0.1 seconds");
                Duration = 0.1;
            }

            if (!initSuccess) return;

            internalName = "Global." + internalName;

            ScenePrivate.Reactions.AddReaction(internalName, ReactionName, ReactionCluster);
            ScenePrivate.Reactions.SubscribeToReaction(OnReaction);

            simpledata = new SimpleData(this);
            simpledata.SourceObjectId = ObjectPrivate.ObjectId;
            simpledata.AgentInfo = null;
            simpledata.ObjectId = ObjectPrivate.ObjectId;
        }
    }
}
