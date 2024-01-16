using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EasySplinePath2D)), CanEditMultipleObjects]
public class EasySplinePath2DEditor : Editor
{
    EasySplinePath2D creator;
    SplinePath2D SplinePath2D
    {
        get
        {
            return creator.path;
        }
    }

    float segmentSelectDistanceThreshold = 0.2f;
    int selectedSegmentIndex = -1;
    int selectedNode = -1;
    int clickedNode = -1;
    bool draggin = false;

    float anchorDiameter = 0.145f;
    float buttonDiameter = 0.064f;
    float controlDiameter = 0.07f;
    float addDiameter = 0.05f;

    float anchorSelectOffset = 0.145f;
    float buttonSelectOffset = 0.064f;
	float controlSelectOffset = 0.7f;
    float scaleSelected = 1.5f;
    float addOffset = 0.05f; 

    List<int> selectedNodes = new List<int>();
    List<int> selectedNodesAux = new List<int>();

    Texture[] texturas;
    bool dragSelect = false;
    bool dragCurve = false;
    bool curveDraggable = false;
    Vector2 dragStartPos;
    Vector2 dragPos;
    Color selectionColor = new Color(0.7f, 1f, 0.5f, 0.3f);
    Color selectionBorderColor = new Color(0.2f, 0.3f, 0f, 1f);
    bool deselect = false;
    int movingSegment = -1;
    bool pressingC = false;
    bool pressingV = false;

    float anchorIconSize = 1;
    float controlIconSize = 0.4f;
    float buttonIconSize = 0.2f;
    float addIconSize = 0.15f;

    delegate void OnSceneFunction();

    Vector2[] botones =
    {
        new Vector2(),
        new Vector2(),
        new Vector2(),
        new Vector2(),
        new Vector2(),
        new Vector2(),
        new Vector2(),
    };

    enum IconType
    {
        NODE,                   //1
        NODE_AUTO,              //2
        NODE_CONTROL,           //3
        NODE_CONTROL_FREE,      //4
        CONTROL,                //5
        ADD,                    //6
        CURVE,                  //7
        FLAT,                   //8
        RECTO,                  //9
        RECTO_U,                //10
        AUTO,                   //11
        AUTO_U,                 //12
        LOCK,                   //13
        LOCK_U,                 //14
        FREE,                   //15
        FREE_U,                 //16
        CORNER,                 //17
        CORNER_U,               //18
        REMOVE,                 //19
		NODE_SELECTED,          //20
    }

    public override void OnInspectorGUI()
    {
		//EditorUtility.SetDirty(creator);
        EditorGUI.BeginChangeCheck();

        float diplayScale = EditorGUILayout.FloatField("Display Scale", creator.displayScale);
        if (diplayScale != creator.displayScale)
        {
            Undo.RecordObject(creator, "Set Display Scale");
			creator.displayScale = diplayScale;
        }

        bool isClosed = EditorGUILayout.Toggle("Close Loop", SplinePath2D.IsClosed);
        if (isClosed != SplinePath2D.IsClosed)
        {
            Undo.RecordObject(creator, "Toggle Closed");
            SplinePath2D.IsClosed = isClosed;
        }

        bool allwaysVisible = EditorGUILayout.Toggle("Always Visible", creator.allwaysVisible);

        if (allwaysVisible != creator.allwaysVisible)
        {
            Undo.RecordObject(creator, "Set Always Visible");
            creator.allwaysVisible = allwaysVisible;
        }

        bool hideControlPoints = EditorGUILayout.Toggle("Hide Handles", creator.hideControlPoints);
        if (hideControlPoints != creator.hideControlPoints)
        {
            Undo.RecordObject(creator, "Set Hide Handles");
            creator.hideControlPoints = hideControlPoints;
        }

		GUIStyle style = new GUIStyle();
        GUILayout.Space(10);

		GUILayout.Label("Default Node Type");
        GUILayout.BeginHorizontal();
        style.fixedWidth = 50;
        style.fixedHeight = 50;

		if (GUILayout.Button(texturas[creator.defaultControlType == SplinePath2D.AnchorStatus.AUTO ? (int)IconType.AUTO : (int)IconType.AUTO_U], style))
		{
            if (creator.defaultControlType != SplinePath2D.AnchorStatus.AUTO)
                Undo.RecordObject(creator, "Set Node Type");
            
			creator.defaultControlType = SplinePath2D.AnchorStatus.AUTO;
		}
        if (GUILayout.Button(texturas[ creator.defaultControlType == SplinePath2D.AnchorStatus.CORNER? (int)IconType.RECTO : (int)IconType.RECTO_U], style))
        {
            if (creator.defaultControlType != SplinePath2D.AnchorStatus.CORNER)
                Undo.RecordObject(creator, "Set Node Type");
            
            creator.defaultControlType = SplinePath2D.AnchorStatus.CORNER;
        }
        if (GUILayout.Button(texturas[creator.defaultControlType == SplinePath2D.AnchorStatus.LOCK ? (int)IconType.LOCK : (int)IconType.LOCK_U], style))
        {
            if (creator.defaultControlType != SplinePath2D.AnchorStatus.LOCK)
                Undo.RecordObject(creator, "Set Node Type");
            
            creator.defaultControlType = SplinePath2D.AnchorStatus.LOCK;
        }
        if (GUILayout.Button(texturas[creator.defaultControlType == SplinePath2D.AnchorStatus.FREE ? (int)IconType.FREE : (int)IconType.FREE_U], style))
        {
            if (creator.defaultControlType != SplinePath2D.AnchorStatus.FREE)
                Undo.RecordObject(creator, "Set Node Type");
            
            creator.defaultControlType = SplinePath2D.AnchorStatus.FREE;
        }

        GUILayout.EndHorizontal();

        if (selectedNode != -1 || selectedNodes.Count > 0)
        {
            GUILayout.Space(10);

            style.fixedWidth = 50;
            style.fixedHeight = 50;
            GUILayout.Label("Change Selected Nodes Type");
            GUILayout.BeginHorizontal();

            bool iguales = true;
            SplinePath2D.AnchorStatus stat;
            if (selectedNodes.Count > 0)
            {
				iguales = AreAllTheSame();
                stat = SplinePath2D.GetAnchorStatus(selectedNodes[0]);
            } else {
                stat = SplinePath2D.GetAnchorStatus(selectedNode);
            }

            if (GUILayout.Button(texturas[iguales && stat == SplinePath2D.AnchorStatus.AUTO? (int)IconType.AUTO : (int)IconType.AUTO_U], style))
			{
                if (stat != SplinePath2D.AnchorStatus.AUTO)
                    Undo.RecordObject(creator, "Set Node Type Multi");
                
				SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.AUTO, true);
			}
            if (GUILayout.Button(texturas[iguales && stat == SplinePath2D.AnchorStatus.CORNER ? (int)IconType.RECTO : (int)IconType.RECTO_U], style))
            {
                if (stat != SplinePath2D.AnchorStatus.CORNER)
                    Undo.RecordObject(creator, "Set Node Type Multi");
                
                SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.CORNER, true);
            }
            if (GUILayout.Button(texturas[iguales && stat == SplinePath2D.AnchorStatus.LOCK ? (int)IconType.LOCK : (int)IconType.LOCK_U], style))
            {
                if (stat != SplinePath2D.AnchorStatus.LOCK)
                    Undo.RecordObject(creator, "Set Node Type Multi");
                
                SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.LOCK, true);
            }
            if (GUILayout.Button(texturas[iguales && stat == SplinePath2D.AnchorStatus.FREE ? (int)IconType.FREE : (int)IconType.FREE_U], style))
            {
                if (stat != SplinePath2D.AnchorStatus.FREE)
                    Undo.RecordObject(creator, "Set Node Type Multi");
                
                SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.FREE, true);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        creator.lineColor = EditorGUILayout.ColorField("Line Color", creator.lineColor);
        creator.handleLineColor = EditorGUILayout.ColorField("Handle Line Color", creator.handleLineColor);
        creator.highlightColor = EditorGUILayout.ColorField("Highlight Color", creator.highlightColor);

        // SNAPPING
        GUILayout.Space(10);
        GUILayout.Label("Snapping:");
        float snapX = EditorGUILayout.FloatField("Snap Size X", creator.snapSizeX);
        if (snapX != creator.snapSizeX)
        {
            Undo.RecordObject(creator, "Set Snap Size X");
            creator.snapSizeX = snapX;
        }
        float snapY = EditorGUILayout.FloatField("Snap Size Y", creator.snapSizeY);
        if (snapY != creator.snapSizeY)
        {
            Undo.RecordObject(creator, "Set Snap Size Y");
            creator.snapSizeY = snapY;
        }
        if (GUILayout.Button("Snap Anchors to grid"))
        {
            Undo.RecordObject(creator, "Snap Anchors to Grid");
            if (selectedNodes.Count > 0)
            {
                for (int i = 0; i < selectedNodes.Count; i ++)
                {
                    Vector2 pos = GetClosestGridPoint(creator.GetPoint(selectedNodes[i]));
                    creator.MovePoint(i, pos);
                }
            } else {
				for (int i = 0; i < SplinePath2D.PointCount; i += 3)
				{
					Vector2 pos = GetClosestGridPoint(creator.GetPoint(i));
					creator.MovePoint(i, pos);
				}
            }

        }
        if (GUILayout.Button("Snap Handlers to grid"))
        {
            Undo.RecordObject(creator, "Snap Handlers to Grid");
            if (selectedNodes.Count > 0)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int i = 0; i < selectedNodes.Count; i++)
                    {
                        if (selectedNodes[i] > 0)
                        {
                            Vector2 pos = GetClosestGridPoint(creator.GetPoint(selectedNodes[i] - 1));
                            if (pos == creator.GetPoint(selectedNodes[i]))
                                pos = GetClosestGridPoint(creator.GetPoint(selectedNodes[i] - 1), true);
                            
							creator.MovePoint(selectedNodes[i] - 1, pos);
                        } else if (SplinePath2D.IsClosed) {
                            Vector2 pos = GetClosestGridPoint(creator.GetPoint(SplinePath2D.PointCount - 1));
                            if (pos == creator.GetPoint(selectedNodes[i]))
                                pos = GetClosestGridPoint(creator.GetPoint(SplinePath2D.PointCount - 1), true);

                            creator.MovePoint(SplinePath2D.PointCount - 1, pos);
                        }

                        if (selectedNodes[i] < SplinePath2D.PointCount - 1 || SplinePath2D.IsClosed)
                        {
                            Vector2 pos = GetClosestGridPoint(creator.GetPoint(selectedNodes[i] + 1));
                            if (pos == creator.GetPoint(selectedNodes[i]))
                                pos = GetClosestGridPoint(creator.GetPoint(selectedNodes[i] + 1), true);

                            creator.MovePoint(selectedNodes[i] + 1, pos);
                        }
                        
                    }
                }
            } else {
				for (int j = 0; j < 2; j++)
				{
					for (int i = 0; i < SplinePath2D.PointCount; i ++)
					{
						if (i % 3 != 0)
						{
							Vector2 pos = GetClosestGridPoint(creator.GetPoint(i));

                            if (pos == GetCorrespondentAnchorPos(i))
                                pos = GetClosestGridPoint(creator.GetPoint(i), true);
                            creator.MovePoint(i, pos);
						}
					}
				}
            }

        }

        GUILayout.Space(20);

        if (GUILayout.Button("Reset"))
        {
            Undo.RecordObject(creator, "Reset");
            selectedSegmentIndex = -1;
            creator.Reset();
            SceneView.RepaintAll();
        }

        GUILayout.Space(10);
        GUILayout.Label("Other Options:");

        bool autoUpdate = EditorGUILayout.Toggle("Auto Update", creator.autoUpdate);
        if (autoUpdate != creator.autoUpdate)
        {
            Undo.RecordObject(creator, "Set Auto Update");
            creator.autoUpdate = autoUpdate;
        }

        float spacing = EditorGUILayout.FloatField("Spacing", creator.spacing);
        if (spacing != creator.spacing)
        {
            Undo.RecordObject(creator, "Set Spacing");
            creator.displayScale = spacing;
        }

        float resolution = EditorGUILayout.FloatField("Spacing", creator.resolution);
        if (resolution != creator.resolution)
        {
            Undo.RecordObject(creator, "Set Resolution");
            creator.displayScale = resolution;
        }

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        base.OnInspectorGUI();
    }

    private Vector2 GetClosestGridPoint(Vector2 pos, bool invert = false)
    {
        float diffX = pos.x / creator.snapSizeX;
        float diffY = pos.y / creator.snapSizeY;
        int indxX = 0;
        int indxY = 0;

        if (diffX > 0)
        {
            if (invert)
            {
                indxX = (diffX - (int)diffX) > 0.5f? Mathf.FloorToInt(diffX) : Mathf.CeilToInt(diffX);
            } else {
				indxX = (diffX - (int)diffX) > 0.5f? Mathf.CeilToInt(diffX) : Mathf.FloorToInt(diffX);
            }
        } else {
            if (invert)
            {
                indxX = (diffX - (int)diffX) > -0.5f ? Mathf.FloorToInt(diffX) : Mathf.CeilToInt(diffX);
            } else {
				indxX = (diffX - (int)diffX) > -0.5f ? Mathf.CeilToInt(diffX) : Mathf.FloorToInt(diffX);
            }
        }

        if (diffY > 0)
        {
            if (invert)
            {
                indxY = (diffY - (int)diffY) > 0.5f ? Mathf.FloorToInt(diffY) : Mathf.CeilToInt(diffY);
            } else {
                indxY = (diffY - (int)diffY) > 0.5f ? Mathf.CeilToInt(diffY) : Mathf.FloorToInt(diffY);
            }
        } else {
            if (invert)
            {
                indxY = (diffY - (int)diffY) > -0.5f ? Mathf.FloorToInt(diffY) : Mathf.CeilToInt(diffY);
            } else {
				indxY = (diffY - (int)diffY) > -0.5f ? Mathf.CeilToInt(diffY) : Mathf.FloorToInt(diffY);
            }
			
        }
        return new Vector2(creator.snapSizeX * indxX, creator.snapSizeY * indxY);
    }

    private bool AreAllTheSame()
    {
        SplinePath2D.AnchorStatus stat = SplinePath2D.GetAnchorStatus(selectedNodes[0]);
        foreach (var i in selectedNodes)
        {
            if (SplinePath2D.GetAnchorStatus(i) != stat)
                return false;
        }
        return true;
    }

    private void OnSceneGUI()
    {
        Input();
        Draw();
    }

    int ClosestAnchor(Vector2 mousePos)
    {
        float minDistToAnchor = anchorSelectOffset * 0.5f;
        int closestAchorIndex = -1;

        for (int i = 0; i < SplinePath2D.PointCount; i += 3)
        {
            float dist = Vector2.Distance(mousePos, creator.GetPoint(i));
            if (dist < minDistToAnchor)
            {
                minDistToAnchor = dist;
                closestAchorIndex = i;
            }
        }
        return closestAchorIndex;
    }

    bool IsClickingButton(Vector2 mousePos)
    {
        for (int i = 0; i < botones.Length; i++)
        {
            Vector2 boton = botones[i];
            float off = buttonSelectOffset;
            if (i == botones.Length)
                off = addOffset;

            if (Vector2.Distance(mousePos, boton) < off * 0.5f)
				return true;
        }

        return false;
    }

    bool IsClickingNodeOrControl(Vector2 mousePos)
    {
        for (int i = 0; i < SplinePath2D.PointCount; i ++)
        {
            float minDist = (i%3 == 0)? anchorSelectOffset * 0.5f: controlSelectOffset * 0.5f;

			float dist = Vector2.Distance(mousePos, creator.GetPoint(i));
            if (dist < minDist)
			{
                 return true;
			}
        }

        return false;
    }

    void UpdateSelection()
    {
        // SELECCIONAR
        List<int> seleccion = new List<int>();

        for (int i = 0; i < SplinePath2D.PointCount; i += 3)
        {
            if ( (Mathf.Abs(dragStartPos.x - creator.GetPoint(i).x) <= Mathf.Abs(dragStartPos.x - dragPos.x) && Mathf.Abs(dragPos.x - creator.GetPoint(i).x) <= Mathf.Abs(dragStartPos.x - dragPos.x)) &&
                (Mathf.Abs(dragStartPos.y - creator.GetPoint(i).y) <= Mathf.Abs(dragStartPos.y - dragPos.y) && Mathf.Abs(dragPos.y - creator.GetPoint(i).y) <= Mathf.Abs(dragStartPos.y - dragPos.y)) )
            {
                if (!selectedNodes.Contains(i) || !selectedNodesAux.Contains(i))
                    seleccion.Add(i);
            } else {
                if (selectedNodesAux.Contains(i))
                    selectedNodesAux.Remove(i);

                if (seleccion.Contains(i))
                    seleccion.Remove(i);
            }
        }

        foreach (int i in seleccion)
        {
            if (!selectedNodesAux.Contains(i))
                selectedNodesAux.Add(i);
        }
    }

    void Input()
    {
        Event guiEvent = Event.current;
        Vector2 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;
        
        if (!pressingV && guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.V)
        {
            GUIUtility.hotControl = 0;
            Event.current.Use();
            pressingV = true;
        }

        if (pressingV && guiEvent.type == EventType.KeyUp && guiEvent.keyCode == KeyCode.V)
        {
            pressingV = false;
        }

        if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.C)
        {
            GUIUtility.hotControl = 0;
            Event.current.Use();
            pressingC = true;
        }

        if (guiEvent.type == EventType.KeyUp && guiEvent.keyCode == KeyCode.C)
        {
            pressingC = false;
        }

        if (guiEvent.type == EventType.MouseMove)
        {
			float minDistToSegment = 100000;
            int newSelectedSegmentIndex = -1;

            for (int i = 0; i < SplinePath2D.SegmentCount; i++)
            {
                Vector2[] points = creator.GetPointsInSegment(i);
                float dist = HandleUtility.DistancePointBezier(mousePos, points[0] + creator.offset, points[3] + creator.offset, points[1] + creator.offset, points[2] + creator.offset);

                if (dist < minDistToSegment)
                {
                    minDistToSegment = dist;
                    newSelectedSegmentIndex = i;
                }
            }

            if (minDistToSegment < segmentSelectDistanceThreshold && pressingC)
            {
                curveDraggable = true;

            } else {
                curveDraggable = false;
            }
            HandleUtility.Repaint();
            if (newSelectedSegmentIndex != selectedSegmentIndex)
            {
                selectedSegmentIndex = newSelectedSegmentIndex;
                HandleUtility.Repaint();
            }
        }

        if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.Delete)
        {
            GUIUtility.hotControl = 0;
            Event.current.Use();
            if (!draggin && !dragSelect)
            {
                selectedNodes.Sort();
                for (int i = selectedNodes.Count - 1; i >= 0; i--)
                {
                    selectedSegmentIndex = -1;
                    Undo.RecordObject(creator, "Delete Node");
                    SplinePath2D.DeleteSegment(selectedNodes[i]);
                }

                if (selectedNode != -1 && !selectedNodes.Contains(selectedNode))
                {
                    SplinePath2D.DeleteSegment(selectedNode);
                    selectedNode = -1;
                }
				selectedNodes.Clear();

				HandleUtility.Repaint();
            }
        }

        if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.Escape || guiEvent.type == EventType.MouseUp && guiEvent.button == 1)
        {
            deselect = true;
            selectedNode = -1;
            selectedNodes.Clear();
            HandleUtility.Repaint();
        }

        if (guiEvent.shift && guiEvent.control)
        {
            if (selectedSegmentIndex != -1)
            {
                Vector2[] segmentPoints = creator.GetPointsInSegment(selectedSegmentIndex);

                if (creator.defaultControlType == SplinePath2D.AnchorStatus.CORNER)
                {
					Handles.DrawBezier(segmentPoints[0], mousePos, segmentPoints[1], mousePos, Color.grey, null, 2);
					Handles.DrawBezier(segmentPoints[3], mousePos, segmentPoints[2], mousePos, Color.grey, null, 2);
                } else {
                    Vector2[] controlAux = SplinePath2D.CalculateAnchorControlPointsPositions(mousePos, selectedSegmentIndex);
                    Handles.DrawBezier(segmentPoints[0], mousePos, segmentPoints[1], controlAux[0], Color.grey, null, 2);
                    Handles.DrawBezier(segmentPoints[3], mousePos, segmentPoints[2], controlAux[1], Color.grey, null, 2);
                }

                HandleUtility.Repaint();
            }
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift && !guiEvent.alt && !guiEvent.control)
        {
            int closest = ClosestAnchor(mousePos);
            if (closest != -1)
            {
                if (!selectedNodes.Contains(closest))
                {
                    selectedNodes.Add(closest);
                    deselect = false;
                    if (selectedNode != -1)
                    {
                        selectedNodes.Add(selectedNode);
                        selectedNode = -1;
                    }
                }
                else
                {
                    selectedNodes.Remove(closest);
                }
            } else {
                dragSelect = true;
                dragStartPos = mousePos;
                dragPos = mousePos;
            }
        }

        if (guiEvent.type == EventType.MouseDrag)
        {
            draggin = true;
            if (dragSelect)
            {
                selectedSegmentIndex = -1;
				dragPos = mousePos;
                UpdateSelection();
                HandleUtility.Repaint();
            }

            if (dragCurve)
            {
                Undo.RecordObject(creator, "Move Curve");
                creator.MoveSegment(movingSegment, mousePos - dragPos, dragStartPos);
                dragPos = mousePos;
                HandleUtility.Repaint();
            }

            deselect = false;
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && !guiEvent.shift && !guiEvent.alt && !guiEvent.control)
        {
            int closestAchorIndex = ClosestAnchor(mousePos);

            if (closestAchorIndex == -1)
            {
                if (!IsClickingButton(mousePos) && !IsClickingNodeOrControl(mousePos))
                {
                    if (selectedSegmentIndex != -1 && curveDraggable)
                    {
                        dragCurve = true;
                        dragStartPos = mousePos;
                        dragPos = mousePos;
                        movingSegment = selectedSegmentIndex;
                    }

                    deselect = true;
                }
                clickedNode = -1;
            } else if (closestAchorIndex != clickedNode) {
                clickedNode = closestAchorIndex;
                dragSelect = false;
            }
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && !guiEvent.shift && !guiEvent.alt && !guiEvent.control)
        {
            if (!draggin)
            {
                if ( clickedNode != -1 )
                {
                    if (selectedNode != clickedNode)
                    {
						selectedNode = clickedNode;
                    } else {
                        selectedNode = -1;
                    }
                } else {
                    if ( clickedNode == -1 && !IsClickingButton(mousePos))
                    {
                        selectedNode = -1;
                    }
                }
            }
            HandleUtility.Repaint();
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.control && !guiEvent.shift)
        {
            int closest = ClosestAnchor(mousePos);
            if (closest != -1)
            {
                Undo.RecordObject(creator, "Delete Node");
                SplinePath2D.DeleteSegment(closest);
                selectedSegmentIndex = -1;
            }
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
        {
            draggin = false;
            dragSelect = false;

            if (deselect)
            {
				selectedNodes.Clear();
            } else {
                foreach (int i in selectedNodesAux)
                {
                    if (!selectedNodes.Contains(i))
                    {
                        selectedNodes.Add(i);
                    }
                }
                selectedNodesAux.Clear();
            }

			dragCurve = false;
            movingSegment = -1;
            Repaint();
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && guiEvent.shift && !guiEvent.alt && guiEvent.control)
        {
            if (selectedSegmentIndex != -1 && !IsClickingNodeOrControl(mousePos) && !IsClickingButton(mousePos) && selectedNodes.Count == 0)
            {
                Undo.RecordObject(creator, "Split segment");
                creator.SplitSegment(mousePos, selectedSegmentIndex);
                HandleUtility.Repaint();
            }
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && guiEvent.shift && !guiEvent.alt && !guiEvent.control)
        {
            if (!IsClickingNodeOrControl(mousePos) && !IsClickingButton(mousePos) && selectedNodes.Count == 0 && !SplinePath2D.IsClosed)
            {
                Undo.RecordObject(creator, "Add segment");
                if (pressingV)
                {
                    creator.AddSegment(GetClosestGridPoint(mousePos));
                } else {
                    creator.AddSegment(mousePos);
                }

                HandleUtility.Repaint();
            }
        }

        HandleUtility.AddDefaultControl(0);

    }

    void Draw()
    {
        // PROVISORIO
        float scale = creator.displayScale;

        anchorIconSize = creator.anchorIconSize * scale;
        controlIconSize = creator.controlIconSize * scale;
        buttonIconSize = creator.buttonIconSize * scale * scaleSelected;
        addIconSize = creator.addIconSize * scale;

        anchorDiameter = creator.anchorDiameter * scale;
        buttonDiameter = creator.buttonDiameter * scale * scaleSelected;
        controlDiameter = creator.controlDiameter * scale;
        addDiameter = addIconSize * 0.5f;

        anchorSelectOffset = anchorIconSize + 0.05f;
        buttonSelectOffset = buttonIconSize + 0.05f;
        controlSelectOffset = controlDiameter + 0.02f;
        addOffset = addIconSize;

        SplinePath2D.offset = creator.offset;

        for (int i = 0; i < SplinePath2D.SegmentCount; i++)
        {
            Vector2[] points = creator.GetPointsInSegment(i);
            if (!creator.hideControlPoints)
            {
                Handles.color = creator.handleLineColor;
                if (SplinePath2D.GetAnchorStatus(i * 3) != SplinePath2D.AnchorStatus.AUTO)
                {
                    Handles.DrawLine(points[1], points[0]);
                }

                int nextAnchor = (i + 1) * 3;
                if (SplinePath2D.IsClosed && i == SplinePath2D.SegmentCount - 1)
                    nextAnchor = 0;

                if (SplinePath2D.GetAnchorStatus(nextAnchor) != SplinePath2D.AnchorStatus.AUTO)
                {
                    Handles.DrawLine(points[2], points[3]);
                }

            }
            Color segmentCol = creator.lineColor;

            if (selectedSegmentIndex == i && curveDraggable)
            {
                segmentCol = creator.highlightColor;
            }

            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentCol, null, 2);
        }
        List<int> anchorsAux = new List<int>();
        List<int> controlsAux = new List<int>();

        for (int i = 0; i < SplinePath2D.PointCount; i++)
        {
            if ( i % 3 == 0)
            {
                anchorsAux.Add(i);
            } else {
                controlsAux.Add(i);
            }
        }

        Handles.color = new Color(0, 0, 0, 0);
        IconType tipo = 0;
        foreach (int i in anchorsAux)
        {
            float handleSize = anchorDiameter;
            float anchSize = anchorIconSize ;

            if (selectedNodes.Contains(i) || selectedNodesAux.Contains(i))
            {
                tipo = IconType.NODE_SELECTED;
            }
            else if (Event.current.control && !Event.current.shift)
            {
                tipo = IconType.REMOVE;
            }
            else
            {
                switch (SplinePath2D.GetAnchorStatus(i))
                {
                    case SplinePath2D.AnchorStatus.CORNER:
                        tipo = IconType.NODE;
                        break;
                    case SplinePath2D.AnchorStatus.FREE:
                        tipo = IconType.NODE_CONTROL_FREE;
                        break;
                    case SplinePath2D.AnchorStatus.LOCK:
                        tipo = IconType.NODE_CONTROL;
                        break;
                    case SplinePath2D.AnchorStatus.AUTO:
                        tipo = IconType.NODE_AUTO;
                        break;
                }
            }

            if (selectedNode == i)
            {
                anchSize *= scaleSelected;
                tipo = IconType.NODE_SELECTED;
            }

#if UNITY_2022_1_OR_NEWER
            Vector3 newPos = Handles.FreeMoveHandle(creator.GetPoint(i), handleSize, new Vector2(), Handles.CircleHandleCap);
#else
            Vector2 newPos = Handles.FreeMoveHandle(creator.GetPoint(i), Quaternion.identity, handleSize, new Vector2(), Handles.CircleHandleCap);
#endif

            if (creator.GetPoint(i) != newPos)
            {
                if (pressingV)
                    newPos = GetClosestGridPoint(newPos);

                if (selectedNodes.Contains(i))
                {
                    Undo.RecordObject(creator, "Move points");
                    Vector2 movement = newPos - creator.GetPoint(i);

                    foreach (int indx in selectedNodes)
                    {
                        creator.MovePoint(indx, creator.GetPoint(indx) + movement);
                    }
                }
                else
                {
                    Undo.RecordObject(creator, "Move point");
                    creator.MovePoint(i, newPos);
                }
            }
            DrawHandleIcon(tipo, creator.GetPoint(i), anchSize);

        }



        if (selectedSegmentIndex != -1)
        {

            Vector2[] p = creator.GetPointsInSegment(selectedSegmentIndex);
            if (p != null)
            {
                Vector2 midPoint = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], 0.5f);
                botones[6] = midPoint;
                DrawHandleIcon(IconType.ADD, midPoint, addIconSize);
                if (Handles.Button(midPoint, Quaternion.identity, addDiameter, addDiameter, Handles.CircleHandleCap))
                {
                    Undo.RecordObject(creator, "Split segment");
                    creator.SplitSegment(midPoint, selectedSegmentIndex);
                }
            }
        }

        if (selectedNode != -1 && selectedNode < SplinePath2D.PointCount)
        {
            int controlA = (SplinePath2D.PointCount + selectedNode - 1) % SplinePath2D.PointCount;
            int controlB = (selectedNode + 1) % SplinePath2D.PointCount;

            float buttonOffset = 0.28f * scale * scaleSelected;

            Vector2 dir;
            Vector2 pos;

            // CURVE BUTTONS
            if (SplinePath2D.GetAnchorStatus(selectedNode) != SplinePath2D.AnchorStatus.AUTO && SplinePath2D.GetAnchorStatus(selectedNode) != SplinePath2D.AnchorStatus.CORNER)
            {
                if (selectedNode > 0 || SplinePath2D.IsClosed)
                {
                    dir = new Vector2(-1, 0.5f).normalized;
                    pos = creator.GetPoint(selectedNode) + dir * buttonOffset;
                    botones[0] = pos;

                    if (Handles.Button(pos, Quaternion.identity, buttonDiameter, buttonDiameter, Handles.CircleHandleCap))
                    {
                        if (creator.GetPoint(selectedNode) == creator.GetPoint(controlA))
                        {
                            Undo.RecordObject(creator, "Open Node Handler");
                            if (creator.GetPoint(selectedNode) != creator.GetPoint(controlB))
                            {
                                dir = (creator.GetPoint(selectedNode) - creator.GetPoint(controlB)).normalized;
                            }
                            pos = creator.GetPoint(selectedNode) + dir * buttonOffset * 3;
                            creator.MovePoint(controlA, pos);
                        }
                        else
                        {
                            Undo.RecordObject(creator, "Close Node Handler");
                            creator.MovePoint(controlA, creator.GetPoint(selectedNode), true);
                        }
                    }
                    if (creator.GetPoint(selectedNode) == creator.GetPoint(controlA))
                    {
                        DrawHandleIcon(IconType.CURVE, pos, buttonIconSize);
                    }
                    else
                    {
                        DrawHandleIcon(IconType.FLAT, pos, buttonIconSize);
                    }
                }

                if (selectedNode < SplinePath2D.PointCount - 1 || SplinePath2D.IsClosed)
                {
                    dir = new Vector2(1, 0.5f).normalized;
                    pos = creator.GetPoint(selectedNode) + dir * buttonOffset;
                    botones[1] = pos;

                    if (Handles.Button(pos, Quaternion.identity, buttonDiameter, buttonDiameter, Handles.CircleHandleCap))
                    {
                        if (creator.GetPoint(selectedNode) == creator.GetPoint(controlB))
                        {
                            if (creator.GetPoint(selectedNode) != creator.GetPoint(controlA))
                            {
                                dir = (creator.GetPoint(selectedNode) - creator.GetPoint(controlA)).normalized;
                            }
                            pos = creator.GetPoint(selectedNode) + dir * buttonOffset * 3;
                            creator.MovePoint(controlB, pos);
                        }
                        else
                        {
                            creator.MovePoint(controlB, creator.GetPoint(selectedNode), true);
                        }

                    }
                    if (creator.GetPoint(selectedNode) == creator.GetPoint(controlB))
                    {
                        DrawHandleIcon(IconType.CURVE, pos, buttonIconSize);
                    }
                    else
                    {
                        DrawHandleIcon(IconType.FLAT, pos, buttonIconSize);
                    }
                }
            }
            // AUTO BUTTON
            dir = new Vector2(-2.2f, -1).normalized;
            pos = creator.GetPoint(selectedNode) + dir * buttonOffset;
            botones[2] = pos;
            if (Handles.Button(pos, Quaternion.identity, buttonDiameter, buttonDiameter, Handles.CircleHandleCap))
            {
                Undo.RecordObject(creator, "Set Node Type to Auto");
                SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.AUTO, true);
            }
            if (SplinePath2D.GetAnchorStatus(selectedNode) == SplinePath2D.AnchorStatus.AUTO)
            {
                DrawHandleIcon(IconType.AUTO, pos, buttonIconSize);
            } else {
                DrawHandleIcon(IconType.AUTO_U, pos, buttonIconSize);
            }

            // LOCK BUTTON
            dir = new Vector2(0.4f, -1).normalized;
            pos = creator.GetPoint(selectedNode) + dir * buttonOffset;
            botones[3] = pos;

            if (Handles.Button(pos, Quaternion.identity, buttonDiameter, buttonDiameter, Handles.CircleHandleCap))
            {
                Undo.RecordObject(creator, "Set Node Type to Lock");
                SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.LOCK, true);
            }
            if (SplinePath2D.GetAnchorStatus(selectedNode) == SplinePath2D.AnchorStatus.LOCK)
            {
                DrawHandleIcon(IconType.LOCK, pos, buttonIconSize);
            }
            else
            {
                DrawHandleIcon(IconType.LOCK_U, pos, buttonIconSize);
            }
            // FREE BUTTON
            dir = new Vector2(2.2f, -1).normalized;
            pos = creator.GetPoint(selectedNode) + dir * buttonOffset;
            botones[4] = pos;

            if (Handles.Button(pos, Quaternion.identity, buttonDiameter, buttonDiameter, Handles.CircleHandleCap))
            {
                Undo.RecordObject(creator, "Set Node Type to Free");
                SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.FREE, true);
            }
            if (SplinePath2D.GetAnchorStatus(selectedNode) == SplinePath2D.AnchorStatus.FREE)
            {
                DrawHandleIcon(IconType.FREE, pos, buttonIconSize);
            }
            else
            {
                DrawHandleIcon(IconType.FREE_U, pos, buttonIconSize);
            }

            // RECT BUTTON
            dir = new Vector2(-0.4f, -1).normalized;
            pos = creator.GetPoint(selectedNode) + dir * buttonOffset;
            botones[5] = pos;

            if (Handles.Button(pos, Quaternion.identity, buttonDiameter, buttonDiameter, Handles.CircleHandleCap))
            {
                Undo.RecordObject(creator, "Set Node Type to Corner");
                SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.CORNER, true);
            }
            if (SplinePath2D.GetAnchorStatus(selectedNode) == SplinePath2D.AnchorStatus.CORNER)
            {
                DrawHandleIcon(IconType.RECTO, pos, buttonIconSize);
            }
            else
            {
                DrawHandleIcon(IconType.RECTO_U, pos, buttonIconSize);
            }
        }

        // CONTROL POINTS
        if (!creator.hideControlPoints)
        {
            foreach (int i in controlsAux)
            {
                float handleSize = controlDiameter;

                bool esconder = false;
                var j = 0;
                if ((i + 1) % 3 != 0)
                {
                    j = (SplinePath2D.PointCount + i - 1) % SplinePath2D.PointCount;
                }
                else
                {
                    j = (i + 1) % SplinePath2D.PointCount;
                }

                if (creator.GetPoint(j) == creator.GetPoint(i) ||
                    SplinePath2D.GetAnchorStatus(j) == SplinePath2D.AnchorStatus.AUTO)
                    esconder = true;

                if (!esconder)
                {
#if UNITY_2022_1_OR_NEWER
                   Vector3 newPos = Handles.FreeMoveHandle(creator.GetPoint(i), handleSize, new Vector2(), Handles.CircleHandleCap);
#else
                   Vector2 newPos = Handles.FreeMoveHandle(creator.GetPoint(i), Quaternion.identity, handleSize, new Vector2(), Handles.CircleHandleCap);
#endif

                    DrawHandleIcon(IconType.CONTROL, creator.GetPoint(i), controlIconSize);
                    //Handles.Label(creator.GetPoint(i), i.ToString());

                    if (creator.GetPoint(i) != newPos)
                    {
                        if (pressingV)
                        {
                            Vector2 gridPos = GetClosestGridPoint(newPos);

                            if (gridPos != GetCorrespondentAnchorPos(i))
                                newPos = gridPos;
                        }
                        
                        Undo.RecordObject(creator, "Move point");
                        creator.MovePoint(i, newPos);
                    }
                }

            }
        }

        // DRAG SELECT
        if (dragSelect)
        {
            Handles.color = selectionColor;
            Handles.DrawSolidRectangleWithOutline(new Rect(dragStartPos, dragPos - dragStartPos), selectionColor, selectionBorderColor);
        }

    }

    private Vector2 GetCorrespondentAnchorPos(int indx)
    {
        if (indx < SplinePath2D.PointCount - 1)
        {
            return indx % 3 == 1 ? creator.GetPoint(indx - 1) : creator.GetPoint(indx + 1);
        } else {
            return creator.GetPoint(0);
        }
    }

    void SetAnchorStatus(int anchorIndex, SplinePath2D.AnchorStatus status, bool setSelection = false)
    {
        Undo.RecordObject(creator, "Change Node Type");
        if (setSelection && selectedNodes.Count > 0)
        {
            foreach (int i in selectedNodes)
            {
				SplinePath2D.SetAnchorStatus(i, status);
            }
        }
        if (anchorIndex != -1)
        {
            SplinePath2D.SetAnchorStatus(anchorIndex, status);
        }
    }

    void DrawHandleIcon(IconType tipo, Vector2 pos, float size)
    {
        GUIStyle estilo = new GUIStyle();

        float zoom = SceneView.currentDrawingSceneView.camera.orthographicSize * 2;
        float alto = SceneView.currentDrawingSceneView.camera.pixelRect.height;

        float sizeAux = size * alto / zoom;

        if (texturas.Length == 0)
            init();

        Texture text = texturas[(int)tipo];
        GUIContent content;
        if (text != null)
        {
            if (sizeAux > text.width)
                sizeAux = text.width;

            content = new GUIContent(text);
        } else {
            content = new GUIContent("Texture Missing");
        }

		estilo.fixedWidth = sizeAux;
        estilo.contentOffset = new Vector2(-sizeAux / 2 + 2, -sizeAux / 2 - 2);

        Handles.Label(pos, content, estilo);

    }

    void OnEnable()
    {
        creator = (EasySplinePath2D)target;
        if (creator.path == null)
        {
            creator.CreatePath();
        }
        init();
        creator.selected = true;
    }

	private void OnDisable()
	{
        creator.selected = false;
	}

	void init()
    {
        Texture[] texts = {
            EditorGUIUtility.Load("EasySplinePath2D/node.png") as Texture,                       // 1
            EditorGUIUtility.Load("EasySplinePath2D/nodo_auto.png") as Texture,                  // 2
            EditorGUIUtility.Load("EasySplinePath2D/nodo_control.png") as Texture,               // 3
            EditorGUIUtility.Load("EasySplinePath2D/nodo_control_free.png") as Texture,          // 4
            EditorGUIUtility.Load("EasySplinePath2D/control.png") as Texture,                    // 5
            EditorGUIUtility.Load("EasySplinePath2D/add.png") as Texture,                        // 6
            EditorGUIUtility.Load("EasySplinePath2D/curve.png") as Texture,                      // 7
            EditorGUIUtility.Load("EasySplinePath2D/rect.png") as Texture,                       // 8
            EditorGUIUtility.Load("EasySplinePath2D/boton_recto.png") as Texture,                // 9
            EditorGUIUtility.Load("EasySplinePath2D/boton_recto_u.png") as Texture,              // 10
            EditorGUIUtility.Load("EasySplinePath2D/mode_auto.png") as Texture,                  // 11
            EditorGUIUtility.Load("EasySplinePath2D/mode_auto_u.png") as Texture,                // 12
            EditorGUIUtility.Load("EasySplinePath2D/mode_lock.png") as Texture,                  // 13
            EditorGUIUtility.Load("EasySplinePath2D/mode_lock_u.png") as Texture,                // 14
            EditorGUIUtility.Load("EasySplinePath2D/mode_free.png") as Texture,                  // 15
            EditorGUIUtility.Load("EasySplinePath2D/mode_free_u.png") as Texture,                // 16
            EditorGUIUtility.Load("EasySplinePath2D/corner_mode_anchor.png") as Texture,         // 17
            EditorGUIUtility.Load("EasySplinePath2D/mode_corner_u.png") as Texture,              // 18
            EditorGUIUtility.Load("EasySplinePath2D/node_delete.png") as Texture,                // 19
            EditorGUIUtility.Load("EasySplinePath2D/node_selected.png") as Texture,              // 20
        };

        texturas = texts;
    }


    [MenuItem("GameObject/2D Object/EasySplinePath2D")]
    private static void CrearNuevo()
    {
        GameObject go = new GameObject("EasySplinePath2D");
        EasySplinePath2D esp2D = go.AddComponent<EasySplinePath2D>();
        if (Selection.transforms.Length > 0)
        {
            go.transform.parent = Selection.transforms[0];
            esp2D.path = new SplinePath2D(Selection.transforms[0].position, esp2D.defaultControlType);
            esp2D.SetUp();
        }
    }

}
