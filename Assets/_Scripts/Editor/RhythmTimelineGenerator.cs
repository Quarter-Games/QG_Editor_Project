using UnityEngine;
using UnityEditor;

using UnityEngine.Timeline;
using System.IO;
using System.Collections.Generic;
using System.Linq;


public class RhythmTimelineEditorWindow : EditorWindow
{
    private AudioClip audioClip;
    private int laneCount = 4;
    private float minVolumeThreshold = 0.1f;
    float lengthToExecute = 1f;

    [MenuItem("Tools/Rhythm/Generate Timeline Window")]
    public static void ShowWindow()
    {
        GetWindow<RhythmTimelineEditorWindow>("Rhythm Timeline Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Rhythm Timeline Generator", EditorStyles.boldLabel);

        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);

        // Display audio information if clip is available
        if (audioClip != null)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Audio Length (seconds)", audioClip.length);
            EditorGUI.EndDisabledGroup();

            // Calculate number of notes (only if we have analyzed the audio)
            if (GUILayout.Button("Preview Analysis"))
            {
                var notes = AudioAnalyzer.ExtractNotesFromAudio(audioClip, minVolumeThreshold);
                EditorUtility.DisplayDialog("Audio Analysis", 
                    $"Audio Length: {audioClip.length:F2} seconds\nNotes Detected: {notes.Count}", "OK");
            }
            
            lengthToExecute = EditorGUILayout.Slider("Length to execute", lengthToExecute, 0f, 1f);
        }

        
        laneCount = EditorGUILayout.IntField("Number of Lanes", laneCount);
        minVolumeThreshold = EditorGUILayout.Slider("Min Volume Threshold", minVolumeThreshold, 0f, 1f);

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Timeline Asset"))
        {
            if (audioClip == null)
            {
                EditorUtility.DisplayDialog("Missing Input", "Please select an AudioClip.", "OK");
                return;
            }

            GenerateTimeline(audioClip, laneCount, minVolumeThreshold, lengthToExecute);
        }
    }
    
    private void GenerateTimeline(AudioClip audioClip, int laneCount, float minVolume, float lengthToExecute)
    {
        AssetDatabase.Refresh();
        // Step 1: Analyze the audio clip to extract notes
        var notesFromAudio = AudioAnalyzer.ExtractNotesWithDuration(audioClip, lengthToExecute);
        
        // Display audio clip length and number of notes found
        Debug.Log($"Audio clip length: {audioClip.length} seconds");
        Debug.Log($"Number of notes found: {notesFromAudio.Count}");
        
        if (notesFromAudio.Count == 0)
        {
            Debug.LogWarning("No notes found in the audio clip.");
            return;
        }
        
        
        // Step 2: Create TimelineAsset
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        
        // First, group notes by their base note name (removing octave numbers)
        var noteGroups = notesFromAudio
            .GroupBy(n => n.noteName.Substring(0, n.noteName.Length - 1))
            .OrderBy(g => g.Key)
            .ToList();

        // Get unique note names count
        int uniqueNoteNames = noteGroups.Count;
        Debug.Log($"Found {uniqueNoteNames} unique note types: {string.Join(", ", noteGroups.Select(g => g.Key))}");

        // Create a mapping of note names to lanes
        Dictionary<string, int> noteToLaneMap = new Dictionary<string, int>();
        for (int i = 0; i < noteGroups.Count; i++)
        {
            // If we have more note types than lanes, we need to combine some notes into the same lane
            int targetLane = laneCount <= uniqueNoteNames ? i % laneCount : i;
            noteToLaneMap[noteGroups[i].Key] = targetLane;
        }

        // Create a list to store notes with their assigned lanes
        var notesWithLanes = new List<(AudioAnalyzer.NoteInfo note, int lane)>();

        // Assign lanes to notes based on their note names
        foreach (var note in notesFromAudio)
        {
            string baseNoteName = note.noteName.Substring(0, note.noteName.Length - 1);
            int assignedLane = noteToLaneMap[baseNoteName];
            notesWithLanes.Add((note, assignedLane));
        }
        
        // Debug output to verify distribution
        foreach (var lane in noteToLaneMap)
        {
            int noteCount = notesWithLanes.Count(n => n.lane == lane.Value);
            Debug.Log($"Lane {lane.Value}: Note {lane.Key} - {noteCount} notes");
        }
        
        // For creating tracks:
        for (int i = 0; i < laneCount; i++)
        {
            
            var track = timeline.CreateTrack<NoteTrack>(null, $"Lane {i + 1}");
            track.name = $"Lane {i + 1}";
            
            var notesForThisLane = notesWithLanes.FindAll(n=> n.lane == i).Select(n => n.note).ToList();
            var sortedNotes = notesForThisLane.OrderBy(n => n.timeStamp).ToList();
            Debug.Log("number of notes for lane " + i + ": " + notesForThisLane.Count);
            // Create clips for each note in this lane
            foreach (var noteInfo in sortedNotes)
            {
                var clip = track.CreateClip<NotePlayableAsset>();
                clip.displayName = noteInfo.noteName;
                clip.start = noteInfo.timeStamp;
                clip.duration = noteInfo.duration > 0 ? noteInfo.duration : 0.2f; // Default duration if not specified
                clip.blendInDuration = 0.5;
                clip.blendOutDuration = 0.5;
                var noteAsset = clip.asset as NotePlayableAsset;
                if (noteAsset != null)
                {
                    noteAsset.laneIndex = i;
                    noteAsset.frequency = noteInfo.frequency;
                    noteAsset.noteName = noteInfo.noteName;
                    noteAsset.volume = noteInfo.volume; 
                }
                
            }
        }
        
        // Step 3: Create a directory for the timeline asset if it doesn't exist
        
        Directory.CreateDirectory(Path.GetDirectoryName("Assets/Timelines"));

      
       
        
        // Add the audio track and bind the audio clip
        var audioTrack = timeline.CreateTrack<AudioTrack>(null, "Audio");
        var audioClipOnTrack = audioTrack.CreateClip<AudioPlayableAsset>();
        audioClipOnTrack.start = 0;
        audioClipOnTrack.duration = audioClip.length;
        var audioAsset = audioClipOnTrack.asset as AudioPlayableAsset;
        if (audioAsset != null)
        {
            audioAsset.clip = audioClip;
        }

        // Create and save the timeline asset first
        string timelineName = audioClip.name.Replace(" ", "_");
        string timelinePath = $"Assets/Timelines/{timelineName}_timeline.playable";

        string dir = Path.GetDirectoryName(timelinePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        // Check if the asset already exists
        if (AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath) != null)
        {
            Debug.LogWarning($"Timeline asset already exists at {timelinePath}. It will be overwritten.");
            AssetDatabase.DeleteAsset(timelinePath);
        }
        // Step 5: Save the Timeline asset
        AssetDatabase.CreateAsset(timeline, timelinePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Timeline created and saved to {timelinePath}");
    }
}