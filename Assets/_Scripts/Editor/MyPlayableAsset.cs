using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class MyPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    public ClipCaps clipCaps => ClipCaps.Blending;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return Playable.Create(graph);
    }
}