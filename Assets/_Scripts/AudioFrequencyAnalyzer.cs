using UnityEngine;
using System.Collections.Generic;
using System;

public static class AudioAnalyzer
{
    
    public static float EstimateBPM(AudioClip audioClip)
    {
        int sampleRate = audioClip.frequency;
        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        int windowSize = Mathf.FloorToInt(sampleRate * sampleWindow);
        List<float> energies = new List<float>();
    
        // 1. Calculate energy per window
        for (int i = 0; i < samples.Length - windowSize; i += windowSize)
        {
            float sum = 0f;
            for (int j = 0; j < windowSize; j++)
            {
                float sample = samples[i + j];
                sum += sample * sample;
            }
            energies.Add(sum);
        }

        // 2. Auto-correlate the energy signal
        int maxLag = Mathf.FloorToInt(energies.Count / 2f);
        float bestLag = 0;
        float bestCorrelation = 0;

        for (int lag = 20; lag < maxLag; lag++) // skip very small lags
        {
            float correlation = 0;
            for (int i = 0; i < energies.Count - lag; i++)
            {
                correlation += energies[i] * energies[i + lag];
            }

            if (correlation > bestCorrelation)
            {
                bestCorrelation = correlation;
                bestLag = lag;
            }
        }

        if (bestLag > 0)
        {
            float secondsPerBeat = sampleWindow * bestLag;
            float bpm = 60f / secondsPerBeat;
            Debug.Log($"Estimated BPM: {bpm}");
            return bpm;
        }

        Debug.LogWarning("Could not estimate BPM.");
        return 120f; // fallback
    }
    
    
    public struct NoteInfo
    {
        public float frequency;   // Frequency in Hz
        public float volume;      // Volume/amplitude (0-1)
        public float timeStamp;   // Time from start in seconds
        public float duration;    // Duration of the note in seconds
        public string noteName;   // Musical note name (e.g., "C4", "A#3")
    }

    private static readonly float[] noteFrequencies = {
        16.35f,  // C0
        17.32f,  // C#0
        18.35f,  // D0
        19.45f,  // D#0
        20.60f,  // E0
        21.83f,  // F0
        23.12f,  // F#0
        24.50f,  // G0
        25.96f,  // G#0
        27.50f,  // A0
        29.14f,  // A#0
        30.87f   // B0
    };

    private static readonly string[] noteNames = {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    };

    static float sampleWindow = 0.5f; // Duration of each chunk in seconds

    public static List<NoteInfo> ExtractNotesFromAudio(AudioClip audioClip, float lengthToExtract = 1.0f, float sensitivity = 0.1f, float minVolume = 0.05f)
    {
        
        //float bpm = EstimateBPM(audioClip);
        
        int sampleRate = audioClip.frequency;
        List<NoteInfo> notes = new List<NoteInfo>();
        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        // Window size for FFT (must be power of 2)
        int windowSize = Mathf.FloorToInt(sampleRate * sampleWindow);

        // Calculate how many samples to process based on lengthToExtract (0 to 1 range)
        int samplesToProcess = Mathf.Min(samples.Length, Mathf.FloorToInt(samples.Length * lengthToExtract));
        
        float[] window = new float[windowSize];
        
        // Variables to track min and max volume
        float minFoundVolume = float.MaxValue;
        float maxFoundVolume = float.MinValue;
        
        // Process audio in windows, but only up to the calculated limit
        for (int i = 0; i < samplesToProcess - windowSize; i += windowSize / 2)
        {
            // Get time for this window
            float timeStamp = (float)i / audioClip.frequency;

            // Apply Hanning window
            for (int j = 0; j < windowSize; j++)
            {
                if (i + j < samples.Length)
                {
                    window[j] = samples[i + j] * (float)(0.5 * (1 - Math.Cos(2 * Math.PI * j / (windowSize - 1))));
                }
                else
                {
                    window[j] = 0;
                }
            }

            // Perform FFT
            float[] spectrum = PerformFFT(window);

            // Find peaks in spectrum
            for (int j = 1; j < spectrum.Length - 1; j++)
            {
                if (spectrum[j] > sensitivity && 
                    spectrum[j] > spectrum[j - 1] && 
                    spectrum[j] > spectrum[j + 1] &&
                    spectrum[j] > minVolume)
                {
                    float frequency = (float)j * audioClip.frequency / windowSize;
                    string noteName = FrequencyToNoteName(frequency);
                    float volume = spectrum[j];
                    
                    // Update min and max volume
                    minFoundVolume = Mathf.Min(minFoundVolume, volume);
                    maxFoundVolume = Mathf.Max(maxFoundVolume, volume);
                    
                    NoteInfo noteInfo = new NoteInfo
                    {
                        frequency = frequency,
                        volume = volume,
                        timeStamp = timeStamp,
                        duration = 0f, // Will be calculated later
                        noteName = noteName
                    };

                    notes.Add(noteInfo);
                    Debug.Log("Detected Note: " + noteName + " at " + timeStamp + "s, Frequency: " + frequency + "Hz, Volume: " + volume);
                }
            }
        }
        
        // Log the min and max volumes found
        if (notes.Count > 0)
        {
            Debug.Log($"Audio volume range - Minimum: {minFoundVolume:F4}, Maximum: {maxFoundVolume:F4}");
        }
        else
        {
            Debug.Log("No notes detected, cannot determine volume range.");
        }

        return notes;
    }

    public static List<NoteInfo> ExtractNotesWithDuration(AudioClip audioClip, float lengthToExtract = 1.0f, float sensitivity = 0.1f, float minVolume = 0.05f, float minNoteDuration = 0.1f)
    {
        List<NoteInfo> rawNotes = ExtractNotesFromAudio(audioClip, lengthToExtract, sensitivity, minVolume);
        List<NoteInfo> notesWithDuration = new List<NoteInfo>();
        
        if (rawNotes.Count == 0) return notesWithDuration;

        // Group consecutive detections of the same note
        NoteInfo currentNote = rawNotes[0];
        float lastTimeStamp = currentNote.timeStamp;

        for (int i = 1; i < rawNotes.Count; i++)
        {
            var note = rawNotes[i];
            
            // If it's the same note and the time difference is small enough
            if (note.noteName == currentNote.noteName && 
                (note.timeStamp - lastTimeStamp) < 0.1f) // 100ms threshold for same note
            {
                lastTimeStamp = note.timeStamp;
            }
            else
            {
                // Calculate duration and add the note if it's long enough
                float duration = lastTimeStamp - currentNote.timeStamp;
                if (duration >= minNoteDuration)
                {
                    var noteWithDuration = currentNote;
                    noteWithDuration.duration = duration;
                    notesWithDuration.Add(noteWithDuration);
                }

                // Start tracking new note
                currentNote = note;
                lastTimeStamp = note.timeStamp;
            }
        }

        // Add the last note
        float finalDuration = lastTimeStamp - currentNote.timeStamp;
        if (finalDuration >= minNoteDuration)
        {
            var finalNote = currentNote;
            finalNote.duration = finalDuration;
            notesWithDuration.Add(finalNote);
        }

        return notesWithDuration;
    }

    private static float[] PerformFFT(float[] samples)
    {
        int n = samples.Length;
        float[] spectrum = new float[n / 2];

        // Simple implementation of magnitude spectrum calculation
        for (int k = 0; k < n / 2; k++)
        {
            float real = 0;
            float imag = 0;

            for (int t = 0; t < n; t++)
            {
                float angle = 2 * Mathf.PI * t * k / n;
                real += samples[t] * Mathf.Cos(angle);
                imag -= samples[t] * Mathf.Sin(angle);
            }

            spectrum[k] = Mathf.Sqrt(real * real + imag * imag) / n;
        }

        return spectrum;
    }

    private static string FrequencyToNoteName(float frequency)
    {
        // Calculate number of half steps from A4 (440 Hz)
        float halfSteps = 12 * Mathf.Log(frequency / 440.0f, 2);
        int roundedHalfSteps = Mathf.RoundToInt(halfSteps);

        // Calculate octave and note
        int octave = 4 + (roundedHalfSteps + 9) / 12;
        int noteIndex = ((roundedHalfSteps + 9) % 12 + 12) % 12;

        return noteNames[noteIndex] + octave;
    }
}