/*
 * @Author: wangyun
 * @CreateTime: 2023-05-28 04:01:33 232
 * @LastEditor: wangyun
 * @EditTime: 2023-05-28 04:01:33 243
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EasySplinePath2DPlus)), CanEditMultipleObjects]
public class EasySplinePath2DPlusEditor : Editor {
	private EasySplinePath2DPlus creator;
	private SplinePath2D SplinePath2D => creator.path;

	private const float segmentSelectDistanceThreshold = 0.2f;
	private static int selectedSegmentIndex = -1;
	private static int selectedNode = -1;
	private static int clickedNode = -1;
	private bool dragging;

	private float anchorDiameter = 0.145f;
	private float buttonDiameter = 0.064f;
	private float controlDiameter = 0.07f;
	private float addDiameter = 0.05f;

	private float anchorSelectOffset = 0.145f;
	private float buttonSelectOffset = 0.064f;
	private float controlSelectOffset = 0.7f;
	private const float scaleSelected = 1.5f;
	private float addOffset = 0.05f;

	private static readonly List<int> selectedNodes = new List<int>();
	private static readonly List<int> selectedNodesAux = new List<int>();

	private Texture[] textures;
	private bool dragSelect;
	private bool dragCurve;
	private bool curveDraggable;
	private Vector3 dragStartPos;
	private Vector3 dragPos;
	private readonly Color selectionColor = new Color(0.7f, 1f, 0.5f, 0.3f);
	private readonly Color selectionBorderColor = new Color(0.2f, 0.3f, 0f, 1f);
	private bool deselect;
	private int movingSegment = -1;
	private bool pressingC;
	private bool pressingV;

	private float anchorIconSize = 1;
	private float controlIconSize = 0.4f;
	private float buttonIconSize = 0.2f;
	private float addIconSize = 0.15f;

	// private delegate void OnSceneFunction();

	private readonly Vector3[] buttons = {
		new Vector3(),
		new Vector3(),
		new Vector3(),
		new Vector3(),
		new Vector3(),
		new Vector3(),
		new Vector3(),
	};

	private enum IconType {
		NODE, //1
		NODE_AUTO, //2
		NODE_CONTROL, //3
		NODE_CONTROL_FREE, //4
		CONTROL, //5
		ADD, //6
		CURVE, //7
		FLAT, //8
		RECTO, //9
		RECTO_U, //10
		AUTO, //11
		AUTO_U, //12
		LOCK, //13
		LOCK_U, //14
		FREE, //15
		FREE_U, //16

		// ReSharper disable once UnusedMember.Local
		CORNER, //17

		// ReSharper disable once UnusedMember.Local
		CORNER_U, //18
		REMOVE, //19
		NODE_SELECTED, //20
	}

	public override void OnInspectorGUI() {
		//EditorUtility.SetDirty(creator);
		EditorGUI.BeginChangeCheck();

		float displayScale = EditorGUILayout.FloatField("Display Scale", creator.displayScale);
		if (!Mathf.Approximately(displayScale, creator.displayScale)) {
			Undo.RecordObject(creator, "Set Display Scale");
			creator.displayScale = displayScale;
		}

		bool isClosed = EditorGUILayout.Toggle("Close Loop", SplinePath2D.IsClosed);
		if (isClosed != SplinePath2D.IsClosed) {
			Undo.RecordObject(creator, "Toggle Closed");
			SplinePath2D.IsClosed = isClosed;
		}

		bool alwaysVisible = EditorGUILayout.Toggle("Always Visible", creator.alwaysVisible);
		if (alwaysVisible != creator.alwaysVisible) {
			Undo.RecordObject(creator, "Set Always Visible");
			creator.alwaysVisible = alwaysVisible;
		}

		bool hideControlPoints = EditorGUILayout.Toggle("Hide Handles", creator.hideControlPoints);
		if (hideControlPoints != creator.hideControlPoints) {
			Undo.RecordObject(creator, "Set Hide Handles");
			creator.hideControlPoints = hideControlPoints;
		}

		GUIStyle style = new GUIStyle();
		GUILayout.Space(10);

		GUILayout.Label("Default Node Type");
		GUILayout.BeginHorizontal();
		style.fixedWidth = 50;
		style.fixedHeight = 50;

		if (GUILayout.Button(textures[creator.defaultControlType == SplinePath2D.AnchorStatus.AUTO ? (int) IconType.AUTO : (int) IconType.AUTO_U], style)) {
			if (creator.defaultControlType != SplinePath2D.AnchorStatus.AUTO)
				Undo.RecordObject(creator, "Set Node Type");

			creator.defaultControlType = SplinePath2D.AnchorStatus.AUTO;
		}

		if (GUILayout.Button(textures[creator.defaultControlType == SplinePath2D.AnchorStatus.CORNER ? (int) IconType.RECTO : (int) IconType.RECTO_U], style)) {
			if (creator.defaultControlType != SplinePath2D.AnchorStatus.CORNER)
				Undo.RecordObject(creator, "Set Node Type");

			creator.defaultControlType = SplinePath2D.AnchorStatus.CORNER;
		}

		if (GUILayout.Button(textures[creator.defaultControlType == SplinePath2D.AnchorStatus.LOCK ? (int) IconType.LOCK : (int) IconType.LOCK_U], style)) {
			if (creator.defaultControlType != SplinePath2D.AnchorStatus.LOCK)
				Undo.RecordObject(creator, "Set Node Type");

			creator.defaultControlType = SplinePath2D.AnchorStatus.LOCK;
		}

		if (GUILayout.Button(textures[creator.defaultControlType == SplinePath2D.AnchorStatus.FREE ? (int) IconType.FREE : (int) IconType.FREE_U], style)) {
			if (creator.defaultControlType != SplinePath2D.AnchorStatus.FREE)
				Undo.RecordObject(creator, "Set Node Type");

			creator.defaultControlType = SplinePath2D.AnchorStatus.FREE;
		}

		GUILayout.EndHorizontal();

		if (selectedNode != -1 || selectedNodes.Count > 0) {
			GUILayout.Space(10);

			style.fixedWidth = 50;
			style.fixedHeight = 50;
			GUILayout.Label("Change Selected Nodes Type");
			GUILayout.BeginHorizontal();

			bool iguales = true;
			SplinePath2D.AnchorStatus stat;
			if (selectedNodes.Count > 0) {
				iguales = AreAllTheSame();
				stat = SplinePath2D.GetAnchorStatus(selectedNodes[0]);
			} else {
				stat = SplinePath2D.GetAnchorStatus(selectedNode);
			}

			if (GUILayout.Button(textures[iguales && stat == SplinePath2D.AnchorStatus.AUTO ? (int) IconType.AUTO : (int) IconType.AUTO_U], style)) {
				if (stat != SplinePath2D.AnchorStatus.AUTO)
					Undo.RecordObject(creator, "Set Node Type Multi");

				SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.AUTO, true);
			}

			if (GUILayout.Button(textures[iguales && stat == SplinePath2D.AnchorStatus.CORNER ? (int) IconType.RECTO : (int) IconType.RECTO_U], style)) {
				if (stat != SplinePath2D.AnchorStatus.CORNER)
					Undo.RecordObject(creator, "Set Node Type Multi");

				SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.CORNER, true);
			}

			if (GUILayout.Button(textures[iguales && stat == SplinePath2D.AnchorStatus.LOCK ? (int) IconType.LOCK : (int) IconType.LOCK_U], style)) {
				if (stat != SplinePath2D.AnchorStatus.LOCK)
					Undo.RecordObject(creator, "Set Node Type Multi");

				SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.LOCK, true);
			}

			if (GUILayout.Button(textures[iguales && stat == SplinePath2D.AnchorStatus.FREE ? (int) IconType.FREE : (int) IconType.FREE_U], style)) {
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
		if (!Mathf.Approximately(snapX, creator.snapSizeX)) {
			Undo.RecordObject(creator, "Set Snap Size X");
			creator.snapSizeX = snapX;
		}

		float snapY = EditorGUILayout.FloatField("Snap Size Y", creator.snapSizeY);
		if (!Mathf.Approximately(snapY, creator.snapSizeY)) {
			Undo.RecordObject(creator, "Set Snap Size Y");
			creator.snapSizeY = snapY;
		}

		if (GUILayout.Button("Snap Anchors to grid")) {
			Undo.RecordObject(creator, "Snap Anchors to Grid");
			if (selectedNodes.Count > 0) {
				foreach (var i in selectedNodes) {
					Vector2 pos = GetClosestGridPoint(creator.GetPoint(i, Space.Self));
					creator.MovePoint(i, pos, false, Space.Self);
				}
			} else {
				for (int i = 0; i < SplinePath2D.PointCount; i += 3) {
					Vector2 pos = GetClosestGridPoint(creator.GetPoint(i, Space.Self));
					creator.MovePoint(i, pos, false, Space.Self);
				}
			}
		}

		if (GUILayout.Button("Snap Handlers to grid")) {
			Undo.RecordObject(creator, "Snap Handlers to Grid");
			if (selectedNodes.Count > 0) {
				for (int j = 0; j < 2; j++) {
					foreach (var i in selectedNodes) {
						if (i > 0) {
							Vector3 pos = GetClosestGridPoint(creator.GetPoint(i - 1, Space.Self));
							if (pos == creator.GetPoint(i, Space.Self))
								pos = GetClosestGridPoint(creator.GetPoint(i - 1, Space.Self), true);
							creator.MovePoint(i - 1, pos, false, Space.Self);
						} else if (SplinePath2D.IsClosed) {
							Vector3 pos = GetClosestGridPoint(creator.GetPoint(SplinePath2D.PointCount - 1, Space.Self));
							if (pos == creator.GetPoint(i, Space.Self))
								pos = GetClosestGridPoint(creator.GetPoint(SplinePath2D.PointCount - 1, Space.Self), true);
							creator.MovePoint(SplinePath2D.PointCount - 1, pos, false, Space.Self);
						}

						if (i < SplinePath2D.PointCount - 1 || SplinePath2D.IsClosed) {
							Vector3 pos = GetClosestGridPoint(creator.GetPoint(i + 1, Space.Self));
							if (pos == creator.GetPoint(i, Space.Self))
								pos = GetClosestGridPoint(creator.GetPoint(i + 1, Space.Self), true);
							creator.MovePoint(i + 1, pos, false, Space.Self);
						}
					}
				}
			} else {
				for (int j = 0; j < 2; j++) {
					for (int i = 0; i < SplinePath2D.PointCount; i++) {
						if (i % 3 != 0) {
							Vector3 pos = GetClosestGridPoint(creator.GetPoint(i, Space.Self));
							if (pos == GetCorrespondentAnchorPos(i, Space.Self))
								pos = GetClosestGridPoint(creator.GetPoint(i, Space.Self), true);
							creator.MovePoint(i, pos, false, Space.Self);
						}
					}
				}
			}
		}

		GUILayout.Space(20);

		if (GUILayout.Button("Reset")) {
			Undo.RecordObject(creator, "Reset");
			selectedSegmentIndex = -1;
			creator.Reset();
			SceneView.RepaintAll();
		}

		GUILayout.Space(10);
		GUILayout.Label("Other Options:");

		bool autoUpdate = EditorGUILayout.Toggle("Auto Update", creator.autoUpdate);
		if (autoUpdate != creator.autoUpdate) {
			Undo.RecordObject(creator, "Set Auto Update");
			creator.autoUpdate = autoUpdate;
		}

		float spacing = EditorGUILayout.FloatField("Spacing", creator.spacing);
		if (!Mathf.Approximately(spacing, creator.spacing)) {
			Undo.RecordObject(creator, "Set Spacing");
			creator.displayScale = spacing;
		}

		float resolution = EditorGUILayout.FloatField("Spacing", creator.resolution);
		if (!Mathf.Approximately(resolution, creator.resolution)) {
			Undo.RecordObject(creator, "Set Resolution");
			creator.displayScale = resolution;
		}

		if (EditorGUI.EndChangeCheck()) {
			SceneView.RepaintAll();
		}

		base.OnInspectorGUI();
	}

	private Vector2 GetClosestGridPoint(Vector2 pos, bool invert = false) {
		float diffX = pos.x / creator.snapSizeX;
		float diffY = pos.y / creator.snapSizeY;
		int indexX;
		int indexY;

		if (diffX > 0) {
			if (invert) {
				indexX = (diffX - (int) diffX) > 0.5f ? Mathf.FloorToInt(diffX) : Mathf.CeilToInt(diffX);
			} else {
				indexX = (diffX - (int) diffX) > 0.5f ? Mathf.CeilToInt(diffX) : Mathf.FloorToInt(diffX);
			}
		} else {
			if (invert) {
				indexX = (diffX - (int) diffX) > -0.5f ? Mathf.FloorToInt(diffX) : Mathf.CeilToInt(diffX);
			} else {
				indexX = (diffX - (int) diffX) > -0.5f ? Mathf.CeilToInt(diffX) : Mathf.FloorToInt(diffX);
			}
		}

		if (diffY > 0) {
			if (invert) {
				indexY = (diffY - (int) diffY) > 0.5f ? Mathf.FloorToInt(diffY) : Mathf.CeilToInt(diffY);
			} else {
				indexY = (diffY - (int) diffY) > 0.5f ? Mathf.CeilToInt(diffY) : Mathf.FloorToInt(diffY);
			}
		} else {
			if (invert) {
				indexY = (diffY - (int) diffY) > -0.5f ? Mathf.FloorToInt(diffY) : Mathf.CeilToInt(diffY);
			} else {
				indexY = (diffY - (int) diffY) > -0.5f ? Mathf.CeilToInt(diffY) : Mathf.FloorToInt(diffY);
			}
		}
		
		return new Vector2(creator.snapSizeX * indexX, creator.snapSizeY * indexY);
	}

	private bool AreAllTheSame() {
		SplinePath2D.AnchorStatus stat = SplinePath2D.GetAnchorStatus(selectedNodes[0]);
		foreach (var i in selectedNodes) {
			if (SplinePath2D.GetAnchorStatus(i) != stat)
				return false;
		}
		return true;
	}

	private void OnSceneGUI() {
		Input();
		Draw();
	}

	private int ClosestAnchor(Vector3 mousePos) {
		float minDistToAnchor = anchorSelectOffset * 0.5f;
		int closestAnchorIndex = -1;
		for (int i = 0; i < SplinePath2D.PointCount; i += 3) {
			float dist = Vector3.Distance(mousePos, creator.GetPoint(i));
			if (dist < minDistToAnchor) {
				minDistToAnchor = dist;
				closestAnchorIndex = i;
			}
		}
		return closestAnchorIndex;
	}

	private bool IsClickingButton(Vector3 mousePos) {
		for (int i = 0; i < buttons.Length; i++) {
			Vector3 button = buttons[i];
			float off = buttonSelectOffset;
			if (i == buttons.Length) {
				off = addOffset;
			}
			if (Vector3.Distance(mousePos, button) < off * 0.5f) {
				return true;
			}
		}
		return false;
	}

	private bool IsClickingNodeOrControl(Vector2 mousePos) {
		for (int i = 0; i < SplinePath2D.PointCount; i++) {
			float minDist = (i % 3 == 0) ? anchorSelectOffset * 0.5f : controlSelectOffset * 0.5f;
			float dist = Vector2.Distance(mousePos, creator.GetPoint(i));
			if (dist < minDist) {
				return true;
			}
		}
		return false;
	}

	private void UpdateSelection() {
		List<int> selection = new List<int>();
		for (int i = 0; i < SplinePath2D.PointCount; i += 3) {
			if ((Mathf.Abs(dragStartPos.x - creator.GetPoint(i).x) <= Mathf.Abs(dragStartPos.x - dragPos.x) &&
							Mathf.Abs(dragPos.x - creator.GetPoint(i).x) <= Mathf.Abs(dragStartPos.x - dragPos.x)) &&
					(Mathf.Abs(dragStartPos.y - creator.GetPoint(i).y) <= Mathf.Abs(dragStartPos.y - dragPos.y) &&
							Mathf.Abs(dragPos.y - creator.GetPoint(i).y) <= Mathf.Abs(dragStartPos.y - dragPos.y)) &&
					(Mathf.Abs(dragStartPos.z - creator.GetPoint(i).z) <= Mathf.Abs(dragStartPos.z - dragPos.z) &&
							Mathf.Abs(dragPos.z - creator.GetPoint(i).z) <= Mathf.Abs(dragStartPos.z - dragPos.z))) {
				if (!selectedNodes.Contains(i) || !selectedNodesAux.Contains(i))
					selection.Add(i);
			} else {
				if (selectedNodesAux.Contains(i))
					selectedNodesAux.Remove(i);

				if (selection.Contains(i))
					selection.Remove(i);
			}
		}
		foreach (int i in selection) {
			if (!selectedNodesAux.Contains(i))
				selectedNodesAux.Add(i);
		}
	}

	private void Input() {
		Event guiEvent = Event.current;
		Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
		Transform creatorTrans = creator.transform;
		Vector3 creatorForward = creatorTrans.forward;
		Vector3 camera2Creator = creatorTrans.position - mouseRay.origin;
		float angleForward2Creator = Vector3.Angle(creatorForward, camera2Creator) * Mathf.Deg2Rad;
		float angleForward2Mouse = Vector3.Angle(creatorForward, mouseRay.direction) * Mathf.Deg2Rad;
		float distance = camera2Creator.magnitude * Mathf.Cos(angleForward2Creator) / Mathf.Cos(angleForward2Mouse);
		Vector3 mousePos = mouseRay.GetPoint(distance);

		if (!pressingV && guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.V) {
			GUIUtility.hotControl = 0;
			Event.current.Use();
			pressingV = true;
		}

		if (pressingV && guiEvent.type == EventType.KeyUp && guiEvent.keyCode == KeyCode.V) {
			pressingV = false;
		}

		if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.C) {
			GUIUtility.hotControl = 0;
			Event.current.Use();
			pressingC = true;
		}

		if (guiEvent.type == EventType.KeyUp && guiEvent.keyCode == KeyCode.C) {
			pressingC = false;
		}

		if (guiEvent.type == EventType.MouseMove || guiEvent.type == EventType.MouseDown) {
			float minDistToSegment = 100000;
			int newSelectedSegmentIndex = -1;

			for (int i = 0; i < SplinePath2D.SegmentCount; i++) {
				Vector3[] points = creator.GetPointsInSegment(i);
				float dist = HandleUtility.DistancePointBezier(
					mousePos,
					points[0] + (Vector3) creator.offset,
					points[3] + (Vector3) creator.offset,
					points[1] + (Vector3) creator.offset,
					points[2] + (Vector3) creator.offset
				);
				if (dist < minDistToSegment) {
					minDistToSegment = dist;
					newSelectedSegmentIndex = i;
				}
			}

			if (minDistToSegment < segmentSelectDistanceThreshold && pressingC) {
				curveDraggable = true;
			} else {
				curveDraggable = false;
			}

			HandleUtility.Repaint();
			if (newSelectedSegmentIndex != selectedSegmentIndex) {
				selectedSegmentIndex = newSelectedSegmentIndex;
				HandleUtility.Repaint();
			}
		}

		if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.Delete) {
			GUIUtility.hotControl = 0;
			Event.current.Use();
			if (!dragging && !dragSelect) {
				selectedNodes.Sort();
				for (int i = selectedNodes.Count - 1; i >= 0; i--) {
					selectedSegmentIndex = -1;
					Undo.RecordObject(creator, "Delete Node");
					SplinePath2D.DeleteSegment(selectedNodes[i]);
				}

				if (selectedNode != -1 && !selectedNodes.Contains(selectedNode)) {
					SplinePath2D.DeleteSegment(selectedNode);
					selectedNode = -1;
				}

				selectedNodes.Clear();

				HandleUtility.Repaint();
			}
		}

		if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.Escape || guiEvent.type == EventType.MouseUp && guiEvent.button == 1) {
			deselect = true;
			selectedNode = -1;
			selectedNodes.Clear();
			HandleUtility.Repaint();
		}

		if (guiEvent.shift && guiEvent.control) {
			if (selectedSegmentIndex != -1) {
				Vector3[] segmentPoints = creator.GetPointsInSegment(selectedSegmentIndex);
				if (creator.defaultControlType == SplinePath2D.AnchorStatus.CORNER) {
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

		if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift && !guiEvent.alt && !guiEvent.control) {
			int closest = ClosestAnchor(mousePos);
			if (closest != -1) {
				if (!selectedNodes.Contains(closest)) {
					selectedNodes.Add(closest);
					deselect = false;
					if (selectedNode != -1) {
						selectedNodes.Add(selectedNode);
						selectedNode = -1;
					}
				} else {
					selectedNodes.Remove(closest);
				}
			} else {
				dragSelect = true;
				dragStartPos = mousePos;
				dragPos = mousePos;
			}
		}

		if (guiEvent.type == EventType.MouseDrag) {
			dragging = true;
			if (dragSelect) {
				selectedSegmentIndex = -1;
				dragPos = mousePos;
				UpdateSelection();
				HandleUtility.Repaint();
			}

			if (dragCurve) {
				Undo.RecordObject(creator, "Move Curve");
				creator.MoveSegment(movingSegment, mousePos - dragPos, dragStartPos);
				dragPos = mousePos;
				HandleUtility.Repaint();
			}

			deselect = false;
		}

		if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && !guiEvent.shift && !guiEvent.alt && !guiEvent.control) {
			int closestAnchorIndex = ClosestAnchor(mousePos);
			if (closestAnchorIndex == -1) {
				if (!IsClickingButton(mousePos) && !IsClickingNodeOrControl(mousePos)) {
					if (selectedSegmentIndex != -1 && curveDraggable) {
						dragCurve = true;
						dragStartPos = mousePos;
						dragPos = mousePos;
						movingSegment = selectedSegmentIndex;
					}
					deselect = true;
				}
				clickedNode = -1;
			} else if (closestAnchorIndex != clickedNode) {
				clickedNode = closestAnchorIndex;
				dragSelect = false;
			}
		}

		if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && !guiEvent.shift && !guiEvent.alt && !guiEvent.control) {
			if (!dragging) {
				if (clickedNode != -1) {
					if (selectedNode != clickedNode) {
						selectedNode = clickedNode;
					} else {
						selectedNode = -1;
					}
				} else if (!IsClickingButton(mousePos)) {
					selectedNode = -1;
				}
			}
			HandleUtility.Repaint();
		}

		if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.control && !guiEvent.shift) {
			int closest = ClosestAnchor(mousePos);
			if (closest != -1) {
				Undo.RecordObject(creator, "Delete Node");
				SplinePath2D.DeleteSegment(closest);
				selectedSegmentIndex = -1;
			}
		}

		if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0) {
			dragging = false;
			dragSelect = false;

			if (deselect) {
				selectedNodes.Clear();
			} else {
				foreach (int i in selectedNodesAux) {
					if (!selectedNodes.Contains(i)) {
						selectedNodes.Add(i);
					}
				}

				selectedNodesAux.Clear();
			}

			dragCurve = false;
			movingSegment = -1;
			Repaint();
		}

		if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && guiEvent.shift && !guiEvent.alt && guiEvent.control) {
			if (selectedSegmentIndex != -1 && !IsClickingNodeOrControl(mousePos) && !IsClickingButton(mousePos) && selectedNodes.Count == 0) {
				Undo.RecordObject(creator, "Split segment");
				creator.SplitSegment(mousePos, selectedSegmentIndex);
				HandleUtility.Repaint();
			}
		}

		if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && guiEvent.shift && !guiEvent.alt && !guiEvent.control) {
			if (!IsClickingNodeOrControl(mousePos) && !IsClickingButton(mousePos) && selectedNodes.Count == 0 && !SplinePath2D.IsClosed) {
				Undo.RecordObject(creator, "Add segment");
				if (pressingV) {
					creator.AddSegment(GetClosestGridPoint(creatorTrans.InverseTransformPoint(mousePos)), Space.Self);
				} else {
					creator.AddSegment(mousePos);
				}

				HandleUtility.Repaint();
			}
		}

		HandleUtility.AddDefaultControl(0);
	}

	private void Draw() {
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

		for (int i = 0; i < SplinePath2D.SegmentCount; i++) {
			Vector3[] points = creator.GetPointsInSegment(i);
			if (!creator.hideControlPoints) {
				Handles.color = creator.handleLineColor;
				if (SplinePath2D.GetAnchorStatus(i * 3) != SplinePath2D.AnchorStatus.AUTO) {
					Handles.DrawLine(points[1], points[0]);
				}

				int nextAnchor = (i + 1) * 3;
				if (SplinePath2D.IsClosed && i == SplinePath2D.SegmentCount - 1)
					nextAnchor = 0;

				if (SplinePath2D.GetAnchorStatus(nextAnchor) != SplinePath2D.AnchorStatus.AUTO) {
					Handles.DrawLine(points[2], points[3]);
				}
			}

			Color segmentCol = creator.lineColor;

			if (selectedSegmentIndex == i && curveDraggable) {
				segmentCol = creator.highlightColor;
			}

			Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentCol, null, 2);
		}

		List<int> anchorsAux = new List<int>();
		List<int> controlsAux = new List<int>();

		for (int i = 0; i < SplinePath2D.PointCount; i++) {
			if (i % 3 == 0) {
				anchorsAux.Add(i);
			} else {
				controlsAux.Add(i);
			}
		}

		Handles.color = new Color(0, 0, 0, 0);
		IconType tipo = 0;
		foreach (int i in anchorsAux) {
			float handleSize = anchorDiameter;
			float anchSize = anchorIconSize;

			if (selectedNodes.Contains(i) || selectedNodesAux.Contains(i)) {
				tipo = IconType.NODE_SELECTED;
			} else if (Event.current.control && !Event.current.shift) {
				tipo = IconType.REMOVE;
			} else {
				switch (SplinePath2D.GetAnchorStatus(i)) {
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

			if (selectedNode == i) {
				anchSize *= scaleSelected;
				tipo = IconType.NODE_SELECTED;
			}

			Vector3 newPos = HandlesFreeMoveHandle(creator.GetPoint(i), handleSize);

			if (creator.GetPoint(i) != newPos) {
				if (pressingV)
					newPos = creator.transform.TransformPoint(GetClosestGridPoint(creator.transform.InverseTransformPoint(newPos)));

				if (selectedNodes.Contains(i)) {
					Undo.RecordObject(creator, "Move points");
					Vector2 movement = newPos - creator.GetPoint(i);

					foreach (int indx in selectedNodes) {
						creator.MovePoint(indx, creator.GetPoint(indx) + (Vector3) movement);
					}
				} else {
					Undo.RecordObject(creator, "Move point");
					creator.MovePoint(i, newPos);
				}
			}

			DrawHandleIcon(tipo, creator.GetPoint(i), anchSize);
		}


		if (selectedSegmentIndex != -1) {
			Vector3[] p = creator.GetPointsInSegment(selectedSegmentIndex, Space.Self);
			if (p != null) {
				Vector3 midPoint = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], 0.5f);
				midPoint = creator.transform.TransformPoint(midPoint);
				buttons[6] = midPoint;
				DrawHandleIcon(IconType.ADD, midPoint, addIconSize);
				if (HandlesButton(midPoint, addDiameter, addDiameter)) {
					Undo.RecordObject(creator, "Split segment");
					creator.SplitSegment(midPoint, selectedSegmentIndex);
				}
			}
		}

		if (selectedNode != -1 && selectedNode < SplinePath2D.PointCount) {
			int controlA = (SplinePath2D.PointCount + selectedNode - 1) % SplinePath2D.PointCount;
			int controlB = (selectedNode + 1) % SplinePath2D.PointCount;

			float buttonOffset = 0.28f * scale * scaleSelected;

			Vector3 dir;
			Vector3 pos;

			// CURVE BUTTONS
			if (SplinePath2D.GetAnchorStatus(selectedNode) != SplinePath2D.AnchorStatus.AUTO &&
					SplinePath2D.GetAnchorStatus(selectedNode) != SplinePath2D.AnchorStatus.CORNER) {
				if (selectedNode > 0 || SplinePath2D.IsClosed) {
					dir = new Vector2(-1, 0.5f).normalized;
					pos = creator.GetPoint(selectedNode, Space.Self) + dir * buttonOffset;
					pos = creator.transform.TransformPoint(pos);
					buttons[0] = pos;

					if (HandlesButton(pos, buttonDiameter, buttonDiameter)) {
						if (creator.GetPoint(selectedNode) == creator.GetPoint(controlA)) {
							Undo.RecordObject(creator, "Open Node Handler");
							if (creator.GetPoint(selectedNode) != creator.GetPoint(controlB)) {
								dir = (creator.GetPoint(selectedNode) - creator.GetPoint(controlB)).normalized;
							}

							pos = creator.GetPoint(selectedNode) + dir * (buttonOffset * 3);
							creator.MovePoint(controlA, pos);
						} else {
							Undo.RecordObject(creator, "Close Node Handler");
							creator.MovePoint(controlA, creator.GetPoint(selectedNode), true);
						}
					}

					if (creator.GetPoint(selectedNode) == creator.GetPoint(controlA)) {
						DrawHandleIcon(IconType.CURVE, pos, buttonIconSize);
					} else {
						DrawHandleIcon(IconType.FLAT, pos, buttonIconSize);
					}
				}

				if (selectedNode < SplinePath2D.PointCount - 1 || SplinePath2D.IsClosed) {
					dir = new Vector2(1, 0.5f).normalized;
					pos = creator.GetPoint(selectedNode, Space.Self) + dir * buttonOffset;
					pos = creator.transform.TransformPoint(pos);
					buttons[1] = pos;

					if (HandlesButton(pos, buttonDiameter, buttonDiameter)) {
						if (creator.GetPoint(selectedNode) == creator.GetPoint(controlB)) {
							if (creator.GetPoint(selectedNode) != creator.GetPoint(controlA)) {
								dir = (creator.GetPoint(selectedNode) - creator.GetPoint(controlA)).normalized;
							}

							pos = creator.GetPoint(selectedNode) + dir * (buttonOffset * 3);
							creator.MovePoint(controlB, pos);
						} else {
							creator.MovePoint(controlB, creator.GetPoint(selectedNode), true);
						}
					}

					if (creator.GetPoint(selectedNode) == creator.GetPoint(controlB)) {
						DrawHandleIcon(IconType.CURVE, pos, buttonIconSize);
					} else {
						DrawHandleIcon(IconType.FLAT, pos, buttonIconSize);
					}
				}
			}

			// AUTO BUTTON
			dir = new Vector2(-2.2f, -1).normalized;
			pos = creator.GetPoint(selectedNode, Space.Self) + dir * buttonOffset;
			pos = creator.transform.TransformPoint(pos);
			buttons[2] = pos;
			if (HandlesButton(pos, buttonDiameter, buttonDiameter)) {
				Undo.RecordObject(creator, "Set Node Type to Auto");
				SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.AUTO, true);
			}

			if (SplinePath2D.GetAnchorStatus(selectedNode) == SplinePath2D.AnchorStatus.AUTO) {
				DrawHandleIcon(IconType.AUTO, pos, buttonIconSize);
			} else {
				DrawHandleIcon(IconType.AUTO_U, pos, buttonIconSize);
			}

			// LOCK BUTTON
			dir = new Vector2(0.4f, -1).normalized;
			pos = creator.GetPoint(selectedNode, Space.Self) + dir * buttonOffset;
			pos = creator.transform.TransformPoint(pos);
			buttons[3] = pos;

			if (HandlesButton(pos, buttonDiameter, buttonDiameter)) {
				Undo.RecordObject(creator, "Set Node Type to Lock");
				SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.LOCK, true);
			}

			if (SplinePath2D.GetAnchorStatus(selectedNode) == SplinePath2D.AnchorStatus.LOCK) {
				DrawHandleIcon(IconType.LOCK, pos, buttonIconSize);
			} else {
				DrawHandleIcon(IconType.LOCK_U, pos, buttonIconSize);
			}

			// FREE BUTTON
			dir = new Vector2(2.2f, -1).normalized;
			pos = creator.GetPoint(selectedNode, Space.Self) + dir * buttonOffset;
			pos = creator.transform.TransformPoint(pos);
			buttons[4] = pos;

			if (HandlesButton(pos, buttonDiameter, buttonDiameter)) {
				Undo.RecordObject(creator, "Set Node Type to Free");
				SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.FREE, true);
			}

			if (SplinePath2D.GetAnchorStatus(selectedNode) == SplinePath2D.AnchorStatus.FREE) {
				DrawHandleIcon(IconType.FREE, pos, buttonIconSize);
			} else {
				DrawHandleIcon(IconType.FREE_U, pos, buttonIconSize);
			}

			// RECT BUTTON
			dir = new Vector2(-0.4f, -1).normalized;
			pos = creator.GetPoint(selectedNode, Space.Self) + dir * buttonOffset;
			pos = creator.transform.TransformPoint(pos);
			buttons[5] = pos;

			if (HandlesButton(pos, buttonDiameter, buttonDiameter)) {
				Undo.RecordObject(creator, "Set Node Type to Corner");
				SetAnchorStatus(selectedNode, SplinePath2D.AnchorStatus.CORNER, true);
			}

			if (SplinePath2D.GetAnchorStatus(selectedNode) == SplinePath2D.AnchorStatus.CORNER) {
				DrawHandleIcon(IconType.RECTO, pos, buttonIconSize);
			} else {
				DrawHandleIcon(IconType.RECTO_U, pos, buttonIconSize);
			}
		}

		// CONTROL POINTS
		if (!creator.hideControlPoints) {
			foreach (int i in controlsAux) {
				float handleSize = controlDiameter;

				bool esconder = false;
				int j;
				if ((i + 1) % 3 != 0) {
					j = (SplinePath2D.PointCount + i - 1) % SplinePath2D.PointCount;
				} else {
					j = (i + 1) % SplinePath2D.PointCount;
				}

				if (creator.GetPoint(j, Space.Self) == creator.GetPoint(i, Space.Self) || SplinePath2D.GetAnchorStatus(j) == SplinePath2D.AnchorStatus.AUTO)
					esconder = true;

				if (!esconder) {
					Vector3 globalPos = creator.GetPoint(i);
					Vector3 newGlobalPos = HandlesFreeMoveHandle(globalPos, handleSize);

					DrawHandleIcon(IconType.CONTROL, globalPos, controlIconSize);
					//Handles.Label(creator.GetPoint(i), i.ToString());

					if (newGlobalPos != globalPos) {
						if (pressingV) {
							Vector3 localPos = creator.transform.InverseTransformPoint(newGlobalPos);
							Vector3 gridPos = GetClosestGridPoint(localPos);
							if (gridPos != GetCorrespondentAnchorPos(i, Space.Self)) {
								newGlobalPos = creator.transform.TransformPoint(gridPos);
							}
						}

						Undo.RecordObject(creator, "Move point");
						creator.MovePoint(i, newGlobalPos);
					}
				}
			}
		}

		// DRAG SELECT
		if (dragSelect) {
			Handles.color = selectionColor;
			Handles.DrawSolidRectangleWithOutline(new Rect(dragStartPos, dragPos - dragStartPos), selectionColor, selectionBorderColor);
		}
	}

	private Vector3 GetCorrespondentAnchorPos(int index, Space space = Space.World) {
		if (index < SplinePath2D.PointCount - 1) {
			return index % 3 == 1 ? creator.GetPoint(index - 1, space) : creator.GetPoint(index + 1, space);
		}
		return creator.GetPoint(0, space);
	}

	private void SetAnchorStatus(int anchorIndex, SplinePath2D.AnchorStatus status, bool setSelection = false) {
		Undo.RecordObject(creator, "Change Node Type");
		if (setSelection && selectedNodes.Count > 0) {
			foreach (int i in selectedNodes) {
				SplinePath2D.SetAnchorStatus(i, status);
			}
		}
		if (anchorIndex != -1) {
			SplinePath2D.SetAnchorStatus(anchorIndex, status);
		}
	}

	private void DrawHandleIcon(IconType type, Vector3 pos, float size) {
		GUIStyle style = new GUIStyle();

		float sizeAux = size;
		Camera camera = SceneView.currentDrawingSceneView.camera;
		if (camera.orthographic) {
			float alto = camera.pixelRect.height;
			float zoom = camera.orthographicSize * 2;
			sizeAux *= alto / zoom;
		} else {
			Transform camaraTrans = camera.transform;
			float distance = Vector3.Project(pos - camaraTrans.position, camaraTrans.forward).magnitude;
			sizeAux *= 1000 / distance;
		}

		if (textures.Length == 0)
			init();

		Texture text = textures[(int) type];
		GUIContent content;
		if (text != null) {
			if (sizeAux > text.width)
				sizeAux = text.width;

			content = new GUIContent(text);
		} else {
			content = new GUIContent("Texture Missing");
		}

		style.fixedWidth = sizeAux;
		style.contentOffset = new Vector2(-sizeAux / 2, -sizeAux / 2);
		
		Handles.Label(pos, content, style);
	}

	private Vector3 HandlesFreeMoveHandle(Vector3 pos, float size) {
		float sizeAux = size;
		Camera camera = SceneView.currentDrawingSceneView.camera;
		if (!camera.orthographic) {
			float alto = camera.pixelRect.height;
			sizeAux *= 1200 / alto;
		}
#if UNITY_2022_1_OR_NEWER
		Vector3 newPos = Handles.FreeMoveHandle(pos, sizeAux, Vector3.zero, Handles.CircleHandleCap);
#else
		Vector3 newPos = Handles.FreeMoveHandle(pos, camera.transform.rotation, sizeAux, Vector3.zero, Handles.CircleHandleCap);
#endif
		Transform creatorTrans = creator.transform;
		Vector3 creatorForward = creatorTrans.forward;
		Vector3 cameraPos = camera.transform.position;
		Vector3 camera2Creator = creatorTrans.position - cameraPos;
		Vector3 camera2Mouse = newPos - cameraPos;
		float angleForward2Creator = Vector3.Angle(creatorForward, camera2Creator) * Mathf.Deg2Rad;
		float angleForward2Mouse = Vector3.Angle(creatorForward, camera2Mouse) * Mathf.Deg2Rad;
		float distance = camera2Creator.magnitude * Mathf.Cos(angleForward2Creator) / Mathf.Cos(angleForward2Mouse);
		return cameraPos + camera2Mouse.normalized * distance;
	}

	private static bool HandlesButton(Vector3 pos, float size, float pickSize) {
		float sizeAux = size;
		Camera camera = SceneView.currentDrawingSceneView.camera;
		if (!camera.orthographic) {
			float alto = camera.pixelRect.height;
			sizeAux *= 1200 / alto;
		}
		return Handles.Button(pos, camera.transform.rotation, sizeAux, sizeAux, Handles.CircleHandleCap);
	}

	private void OnEnable() {
		creator = (EasySplinePath2DPlus) target;
		if (creator.path == null) {
			creator.CreatePath();
		}
		init();
		creator.selected = true;
	}

	private void OnDisable() {
		creator.selected = false;
	}

	private void init() {
		Texture[] texts = {
			Resources.Load<Texture>("EasySplinePath2D/node"),					// 1
			Resources.Load<Texture>("EasySplinePath2D/nodo_auto"),				// 2
			Resources.Load<Texture>("EasySplinePath2D/nodo_control"),			// 3
			Resources.Load<Texture>("EasySplinePath2D/nodo_control_free"),		// 4
			Resources.Load<Texture>("EasySplinePath2D/control"),				// 5
			Resources.Load<Texture>("EasySplinePath2D/add"),					// 6
			Resources.Load<Texture>("EasySplinePath2D/curve"),					// 7
			Resources.Load<Texture>("EasySplinePath2D/rect"),					// 8
			Resources.Load<Texture>("EasySplinePath2D/boton_recto"),			// 9
			Resources.Load<Texture>("EasySplinePath2D/boton_recto_u"),			// 10
			Resources.Load<Texture>("EasySplinePath2D/mode_auto"),				// 11
			Resources.Load<Texture>("EasySplinePath2D/mode_auto_u"),			// 12
			Resources.Load<Texture>("EasySplinePath2D/mode_lock"),				// 13
			Resources.Load<Texture>("EasySplinePath2D/mode_lock_u"),			// 14
			Resources.Load<Texture>("EasySplinePath2D/mode_free"),				// 15
			Resources.Load<Texture>("EasySplinePath2D/mode_free_u"),			// 16
			Resources.Load<Texture>("EasySplinePath2D/corner_mode_anchor"),		// 17
			Resources.Load<Texture>("EasySplinePath2D/mode_corner_u"),			// 18
			Resources.Load<Texture>("EasySplinePath2D/node_delete"),			// 19
			Resources.Load<Texture>("EasySplinePath2D/node_selected"),			// 20
		};

		textures = texts;
	}


	[MenuItem("GameObject/2D Object/EasySplinePath2DPlus")]
	private static void CreateNew() {
		GameObject go = new GameObject("EasySplinePath2DPlus");
		EasySplinePath2DPlus esp2D = go.AddComponent<EasySplinePath2DPlus>();
		if (Selection.transforms.Length > 0) {
			go.transform.parent = Selection.transforms[0];
			esp2D.path = new SplinePath2D(Selection.transforms[0].position, esp2D.defaultControlType);
			esp2D.SetUp();
		}
	}
}
