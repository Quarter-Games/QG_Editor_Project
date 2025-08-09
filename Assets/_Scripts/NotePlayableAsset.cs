using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class NotePlayableAsset : PlayableAsset, ITimelineClipAsset
{
    public int laneIndex;
    public float frequency;
    public string noteName;
    public float volume = 1.0f; // Default volume, can be adjusted per note
    
    
    public ClipCaps clipCaps => ClipCaps.Blending;
    
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return Playable.Create(graph);
    }
}