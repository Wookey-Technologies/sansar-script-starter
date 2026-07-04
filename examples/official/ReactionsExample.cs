/* This content is licensed under the terms of the Creative Commons Attribution 4.0 International License.
 * When using this content, you must:
 * �    Acknowledge that the content is from the Sansar Knowledge Base.
 * �    Include our copyright notice: "2022 Sansar, Inc."
 * �    Indicate that the content is licensed under the Creative Commons Attribution-Share Alike 4.0 International License.
 * �    Include the URL for, or link to, the license summary at https://creativecommons.org/licenses/by-sa/4.0/deed.hi (and, if possible, to the complete license terms at https://creativecommons.org/licenses/by-sa/4.0/legalcode.
 * For example:
 * "This work uses content from the Sansar Knowledge Base. � 2022 Sansar, Inc." Licensed under the Creative Commons Attribution 4.0 International License (license summary available at https://creativecommons.org/licenses/by/4.0/ and complete license terms available at https://creativecommons.org/licenses/by/4.0/legalcode)."
 */
 
using System;
using System.Collections.Generic;
using Sansar.Simulation;

namespace ReactionsExample
{
    /// <summary>
    /// Default handling of user reactions
    /// </summary>
    public class ReactionResponse : SceneObjectScript
    {
        public ThumbnailedClusterResource HeartCluster = null;
        public ThumbnailedClusterResource StarsCluster = null;
        public ThumbnailedClusterResource FireCluster = null;
        public ThumbnailedClusterResource LightningCluster = null;
        public ThumbnailedClusterResource ExclamationCluster = null;
        public ThumbnailedClusterResource WTFCluster = null;
        public ThumbnailedClusterResource PandaCluster = null;
        public ThumbnailedClusterResource ToastCluster = null;
        public ThumbnailedClusterResource BobaCluster = null;

        private Dictionary<string, ThumbnailedClusterResource> _clusterMap = new Dictionary<string, ThumbnailedClusterResource>();

        public override void Init()
        {
            _clusterMap.Add("Sansar.Heart", HeartCluster);
            _clusterMap.Add("Sansar.Stars", StarsCluster);
            _clusterMap.Add("Sansar.Fire", FireCluster);
            _clusterMap.Add("Sansar.Lightning", LightningCluster);
            _clusterMap.Add("Sansar.Exclamation", ExclamationCluster);
            _clusterMap.Add("Sansar.WTF", WTFCluster);
            _clusterMap.Add("Sansar.Panda", PandaCluster);
            _clusterMap.Add("Sansar.Toast", ToastCluster);
            _clusterMap.Add("Sansar.Boba", BobaCluster);

            ScenePrivate.Reactions.SubscribeToReaction(OnReaction);
        }

        private void OnReaction(ReactionData data)
        {
            ThumbnailedClusterResource cluster = null;
            if (_clusterMap.TryGetValue(data.Type, out cluster))
            {
                if (cluster != null)
                {
#pragma warning disable 618
                    ScenePrivate.CreateCluster(cluster.ClusterResource, data.Position + new Sansar.Vector(0.0f, 0.0f, 1.8f), data.Orientation, new Sansar.Vector());
#pragma warning restore
                }
            }
        }
    }
}