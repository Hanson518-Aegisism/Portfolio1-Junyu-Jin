using UnityEditor;
using UnityEngine;

/// <summary>
/// Polybrush sometimes keeps a BrushSettingsEditor alive after its BrushSettings
/// target becomes null, which spams SerializedObjectNotCreatableException.
/// Clean up those stale editors on load / playmode changes.
/// </summary>
[InitializeOnLoad]
internal static class PolybrushNullEditorCleanup
{
    static PolybrushNullEditorCleanup()
    {
        EditorApplication.delayCall += Cleanup;
        EditorApplication.playModeStateChanged += _ => EditorApplication.delayCall += Cleanup;
    }

    private static void Cleanup()
    {
        Editor[] editors = Resources.FindObjectsOfTypeAll<Editor>();
        for (int i = 0; i < editors.Length; i++)
        {
            Editor editor = editors[i];
            if (editor == null)
                continue;

            if (editor.GetType().FullName != "UnityEditor.Polybrush.BrushSettingsEditor")
                continue;

            if (editor.target != null)
                continue;

            Object.DestroyImmediate(editor);
        }
    }
}
