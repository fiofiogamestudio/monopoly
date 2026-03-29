using System.Collections;
using UnityEngine;

public class DataLoader
{
    public static T LoadJson<T>(string path)
    {
        TextAsset jsonText = Resources.Load<TextAsset>(path);
        T temp = JsonUtility.FromJson<T>(jsonText.text);
        return temp;
    }

    public static void SaveText(string content, string path)
    {

    }

    public static string LoadText(string path, bool ignoreError = false)
    {
        try
        {
            return Resources.Load<TextAsset>(path).text;
        }
        catch (System.Exception e)
        {
            if (!ignoreError)
                Debug.LogError($"LoadText Path: {path} Error: {e.Message}");
            return "";
        }
    }
}

public class DataDebuger
{
}