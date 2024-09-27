using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script, used to update the road in real time.
/// </summary>
[CustomEditor(typeof(EasyRoadPlus))]
public class EasyRoadPlusEditor : Editor
{
    EasyRoadPlus creator;

    void OnSceneGUI()
    {
        if (creator.liveUpdate && Event.current.type == EventType.Repaint)
        {
            creator.UpdateMesh();
        }
    }

    void OnEnable()
    {
        creator = (EasyRoadPlus)target;
    }
}