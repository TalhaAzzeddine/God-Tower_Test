#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class HierarchySeparator
{
    private const string SeparatorName = "___SEPARATOR___";

    static HierarchySeparator()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    // =========================================================
    // ADD SEPARATOR
    // =========================================================

    [MenuItem("GameObject/Add Separator", false, 0)]
    private static void AddSeparator(MenuCommand menuCommand)
    {
        GameObject separator = new GameObject(SeparatorName);

        separator.transform.localPosition = Vector3.zero;
        separator.transform.localRotation = Quaternion.identity;
        separator.transform.localScale = Vector3.zero;

        GameObject parent = menuCommand.context as GameObject;

        if (parent != null)
        {
            Undo.SetTransformParent(
                separator.transform,
                parent.transform,
                "Add Separator"
            );
        }

        Undo.RegisterCreatedObjectUndo(
            separator,
            "Add Separator"
        );

        EditorSceneManager.MarkSceneDirty(separator.scene);

        EditorApplication.RepaintHierarchyWindow();
    }

    [MenuItem("GameObject/Add Separator", true)]
    private static bool ValidateAddSeparator()
    {
        return true;
    }

    // =========================================================
    // HIERARCHY DRAWING
    // =========================================================

    private static void OnHierarchyGUI(
        int instanceID,
        Rect selectionRect)
    {
        GameObject obj =
            EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (obj == null)
            return;

        if (obj.name != SeparatorName)
            return;

        // -----------------------------------------------------
        // Hide Unity's default label
        // -----------------------------------------------------

        Rect labelRect = new Rect(
            selectionRect.x,
            selectionRect.y,
            selectionRect.width,
            selectionRect.height
        );

        EditorGUI.DrawRect(
            labelRect,
            EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.76f, 0.76f, 0.76f)
        );

        // -----------------------------------------------------
        // Calculate hierarchy indentation
        // -----------------------------------------------------

        int depth = GetDepth(obj.transform);

        float indent = 18f + depth * 14f;

        float left = selectionRect.xMin + indent;
        float right = selectionRect.xMax - 8f;

        // -----------------------------------------------------
        // Draw ONE thin line
        // -----------------------------------------------------

        float lineHeight = 2f;

        Rect line = new Rect(
            left,
            selectionRect.center.y - lineHeight / 2f,
            right - left,
            lineHeight
        );

        Color lineColor = EditorGUIUtility.isProSkin
            ? new Color(0.20f, 0.45f, 0.70f)
            : new Color(0.15f, 0.35f, 0.60f);

        EditorGUI.DrawRect(line, lineColor);
    }

    // =========================================================
    // DEPTH
    // =========================================================

    private static int GetDepth(Transform transform)
    {
        int depth = 0;

        Transform parent = transform.parent;

        while (parent != null)
        {
            depth++;
            parent = parent.parent;
        }

        return depth;
    }
}

#endif