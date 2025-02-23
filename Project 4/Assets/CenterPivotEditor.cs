using UnityEngine;
using UnityEditor;

public class CenterPivotEditor : Editor
{
    [MenuItem("Tools/Center Pivot of Selected GameObject")]
    static void CenterPivot()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogError("No GameObject selected.");
            return;
        }

        Vector3 center = Vector3.zero;
        int childCount = 0;

        foreach (Transform child in selected.transform)
        {
            center += child.localPosition;
            childCount++;
        }

        if (childCount == 0)
        {
            Debug.LogError("Selected GameObject has no child objects.");
            return;
        }

        center /= childCount;


        selected.transform.localPosition += center;


        foreach (Transform child in selected.transform)
        {
            child.localPosition -= center;
        }

        Debug.Log("Pivot centered for GameObject: " + selected.name);
    }
}
