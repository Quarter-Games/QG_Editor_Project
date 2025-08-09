using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using System.IO;

public class TimelineCreator
{
    [MenuItem("Tools/Timeline/Create Test Timeline")]
    public static void CreateTimeline()
    {
        // Create new TimelineAsset
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

        // Add custom track
        var track = timeline.CreateTrack<MyTrack>(null, "Test Track");

        // Create a new clip on that track
        var clip = track.CreateClip<MyPlayableAsset>();
        clip.start = 0;
        clip.duration = 2.0;
        clip.displayName = "MyClip";

        // Set blend if you want (not required)
        clip.blendInDuration = 0.5;
        clip.blendOutDuration = 0.5;

        // Save the timeline asset to project
        string path = "Assets/Timelines/TestTimeline.playable";
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        AssetDatabase.CreateAsset(timeline, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Timeline created and saved at " + path);
    }
}