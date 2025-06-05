using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class ScreenshotHelperWindow : EditorWindow
{
    Camera selectedCamera;
    int cameraIndex = 0;
    int width = 1920, height = 1080;
    Texture2D watermark;
    bool useWatermark = false;
    string savePath = "Screenshots";
    Texture2D lastScreenshot;

    [MenuItem("Tools/Screenshot Helper %#k")] // CTRL+SHIFT+K
    static void ShowWindow()
    {
        GetWindow<ScreenshotHelperWindow>("Screenshot Helper");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Screenshot Helper Tool", EditorStyles.boldLabel);

       
        Camera[] allCameras = FindObjectsOfType<Camera>();
        if (allCameras.Length == 0)
        {
            EditorGUILayout.HelpBox("No cameras found in the scene!", MessageType.Error);
            return;
        }

        string[] cameraNames = allCameras.Select(c => c.name).ToArray();
        cameraIndex = Mathf.Clamp(cameraIndex, 0, allCameras.Length - 1);
        cameraIndex = EditorGUILayout.Popup("Camera", cameraIndex, cameraNames);
        selectedCamera = allCameras[cameraIndex];

        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);

        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);

        if (width <= 0 || height <= 0)
            EditorGUILayout.HelpBox("Width and Height must be positive!", MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Watermark (Optional)", EditorStyles.boldLabel);

        useWatermark = EditorGUILayout.Toggle("Add Watermark", useWatermark);
        if (useWatermark)
            watermark = (Texture2D)EditorGUILayout.ObjectField("Watermark Texture", watermark, typeof(Texture2D), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Save Location", EditorStyles.boldLabel);
        EditorGUILayout.TextField("Save Path", savePath);
        if (GUILayout.Button("Change Save Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Save Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
                savePath = path;
        }

        EditorGUILayout.Space();
        GUI.enabled = width > 0 && height > 0 && selectedCamera != null && !string.IsNullOrEmpty(savePath);
        if (GUILayout.Button("Take Screenshot"))
        {
            TakeScreenshot();
        }
        GUI.enabled = true;

        if (lastScreenshot != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Screenshot Preview:", EditorStyles.boldLabel);
            float previewWidth = Mathf.Min(position.width - 40, 256);
            float previewHeight = previewWidth * lastScreenshot.height / (float)lastScreenshot.width;
            GUILayout.Label(lastScreenshot, GUILayout.Width(previewWidth), GUILayout.Height(previewHeight));
        }
    }

    void TakeScreenshot()
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        selectedCamera.targetTexture = rt;
        selectedCamera.Render();

        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        if (useWatermark && watermark != null)
        {
            AddWatermark(screenshot, watermark);
        }

        selectedCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        byte[] bytes = screenshot.EncodeToPNG();
        string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string folder = savePath;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        string fullPath = Path.Combine(folder, fileName);
        File.WriteAllBytes(fullPath, bytes);

        lastScreenshot = screenshot;

        EditorUtility.RevealInFinder(fullPath);

        Debug.Log("Screenshot saved to: " + fullPath);
    }

    void AddWatermark(Texture2D baseTex, Texture2D watermark)
    {
        int wmWidth = baseTex.width / 4;
        int wmHeight = baseTex.height / 4;
        int startX = baseTex.width - wmWidth - 10;
        int startY = 10;

        for (int y = 0; y < wmHeight; y++)
        {
            for (int x = 0; x < wmWidth; x++)
            {
                int baseX = startX + x;
                int baseY = startY + y;
                Color baseColor = baseTex.GetPixel(baseX, baseY);
                Color wmColor = watermark.GetPixelBilinear((float)x / wmWidth, (float)y / wmHeight);
                baseTex.SetPixel(baseX, baseY, Color.Lerp(baseColor, wmColor, wmColor.a));
            }
        }
        baseTex.Apply();
    }
}
