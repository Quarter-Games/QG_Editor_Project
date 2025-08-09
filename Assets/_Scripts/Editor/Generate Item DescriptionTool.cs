using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

public class GenerateItemDescriptionWindow : EditorWindow
{
    private string prompt = "Generate a fantasy sword item description with stats.";
    private string generatedContent = "";
    private Vector2 scrollPosition;
    private bool isGenerating = false;

    [MenuItem("AI Tools/Generate Item Description")]
    public static void ShowWindow()
    {
        GetWindow<GenerateItemDescriptionWindow>("Item Description Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Item Description Generator", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("Enter your prompt:");
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(100));
        
        EditorGUI.BeginDisabledGroup(isGenerating);
        if (GUILayout.Button("Send", GUILayout.Height(30)))
        {
            isGenerating = true;
            GenerateItemDescription();
        }
        EditorGUI.EndDisabledGroup();
        
        if (isGenerating)
        {
            EditorGUILayout.HelpBox("Generating content...", MessageType.Info);
        }
        
        if (!string.IsNullOrEmpty(generatedContent))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Content:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(generatedContent, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = generatedContent;
            }
        }
    }

    private async void GenerateItemDescription()
    {
        string response = await SendPromptToLLM(prompt);
        generatedContent = response;
        isGenerating = false;
        Repaint();
    }

    private async Task<string> SendPromptToLLM(string prompt)
    {
        var client = new HttpClient();
        var json = JsonUtility.ToJson(new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        });
        
        // go to chat gpt page and create API KEY so you will be able to use this...
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "YOUR_API_KEY");
        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", new StringContent(json, Encoding.UTF8, "application/json"));
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}