using UnityEditor;
using UnityEngine;

/// <summary>Remove the oversized fixed preview and low-resolution preview flags.</summary>
[InitializeOnLoad]
public static class GameViewResolution
{
    static GameViewResolution()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode || SessionState.GetBool("Crew.NativeGameView", false)) return;
            UseNativePreview();
            SessionState.SetBool("Crew.NativeGameView", true);
        };
    }

    [MenuItem("Tools/Crew/Use Native Game View")]
    public static void UseNativePreview()
    {
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (type == null) return;
        foreach (Object window in Resources.FindObjectsOfTypeAll(type))
        {
            var serialized = new SerializedObject(window);
            var sizes = serialized.FindProperty("m_SelectedSizes");
            if (sizes != null && sizes.isArray)
                for (int i = 0; i < sizes.arraySize; i++) sizes.GetArrayElementAtIndex(i).intValue = 0;
            var lowResolution = serialized.FindProperty("m_LowResolutionForAspectRatios");
            if (lowResolution != null && lowResolution.isArray)
                for (int i = 0; i < lowResolution.arraySize; i++)
                {
                    var item = lowResolution.GetArrayElementAtIndex(i);
                    if (item.propertyType == SerializedPropertyType.Boolean) item.boolValue = false;
                    else if (item.propertyType == SerializedPropertyType.Integer) item.intValue = 0;
                }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ((EditorWindow)window).Repaint();
        }
    }
}
