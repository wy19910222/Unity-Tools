/*
 * @Author: wangyun
 * @CreateTime: 2023-05-17 20:56:33 481
 * @LastEditor: wangyun
 * @EditTime: 2023-05-17 20:56:33 496
 */

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UObject = UnityEngine.Object;

[CanEditMultipleObjects]
[CustomEditor(typeof(RectTransform))]
public class RectTransformInspector : Editor {
	private Editor m_InternalEditor;
	
	// private SerializedProperty m_Position;
	// private SerializedProperty m_Rotation;
	// private SerializedProperty m_Scale;
	//
	// private FieldInfo showAnchorsFI;
	//
	// private bool m_IsGlobalVisible;
	
	private void OnEnable() {
		Type editorType = typeof(Editor).Assembly.GetType("UnityEditor.RectTransformEditor");
		m_InternalEditor = CreateEditor(targets, editorType);
		// showAnchorsFI = editorType.GetField("m_ShowLayoutOptions", BindingFlags.Instance | BindingFlags.NonPublic);
		//
		// FieldInfo positionFI = editorType.GetField("m_Position", BindingFlags.Instance | BindingFlags.NonPublic);
		// m_Position = positionFI?.GetValue(m_InternalEditor) as SerializedProperty;
		//
		// FieldInfo scaleFI = editorType.GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
		// m_Scale = scaleFI?.GetValue(m_InternalEditor) as SerializedProperty;
		//
		// FieldInfo rotationGuiFI = editorType.GetField("m_RotationGUI", BindingFlags.Instance | BindingFlags.NonPublic);
		// object rotationGUI = rotationGuiFI?.GetValue(m_InternalEditor);
		// FieldInfo rotationFI = rotationGUI?.GetType().GetField("m_Rotation", BindingFlags.Instance | BindingFlags.NonPublic);
		// m_Rotation = rotationFI?.GetValue(rotationGUI) as SerializedProperty;
		//
		// m_IsGlobalVisible = EditorPrefs.GetBool("Transform.IsGlobalVisible", false);
	}
	
	private void OnDisable() {
		DestroyImmediate(m_InternalEditor);
	}
	
	public override void OnInspectorGUI() {
		if (m_InternalEditor) {
			m_InternalEditor.OnInspectorGUI();
		} else {
			base.OnInspectorGUI();
		}

		int rate = 0;
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("保留整数")) {
			rate = 1;
		}
		if (GUILayout.Button("保留2位小数")) {
			rate = 100;
		}
		EditorGUILayout.EndHorizontal();
		if (rate > 0 && target is RectTransform rectTrans) {
			Undo.RecordObject(rectTrans, "Round");
			Vector3 position = rectTrans.localPosition;
			position.x = Mathf.Round(position.x * rate) / rate;
			position.y = Mathf.Round(position.y * rate) / rate;
			position.z = Mathf.Round(position.z * rate) / rate;
			rectTrans.localPosition = position;
			Vector3 angles = rectTrans.localEulerAngles;
			angles.x = Mathf.Round(angles.x * rate) / rate;
			angles.y = Mathf.Round(angles.y * rate) / rate;
			angles.z = Mathf.Round(angles.z * rate) / rate;
			rectTrans.localEulerAngles = angles;
			Vector3 scale = rectTrans.localScale;
			scale.x = Mathf.Round(scale.x * rate) / rate;
			scale.y = Mathf.Round(scale.y * rate) / rate;
			scale.z = Mathf.Round(scale.z * rate) / rate;
			rectTrans.localScale = scale;
			Vector2 anchoredPos = rectTrans.anchoredPosition;
			anchoredPos.x = Mathf.Round(anchoredPos.x * rate) / rate;
			anchoredPos.y = Mathf.Round(anchoredPos.y * rate) / rate;
			rectTrans.anchoredPosition = anchoredPos;
			Vector2 sizeDelta = rectTrans.sizeDelta;
			sizeDelta.x = Mathf.Round(sizeDelta.x * rate) / rate;
			sizeDelta.y = Mathf.Round(sizeDelta.y * rate) / rate;
			rectTrans.sizeDelta = sizeDelta;
		}
		// bool m_ShowAnchors = showAnchorsFI?.GetValue(m_InternalEditor) is bool isShow && isShow;
		// if (m_ShowAnchors) {
		// }
		//
		// EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight * 3 - 8F);
		// EditorGUILayout.BeginHorizontal();
		// EditorGUILayout.LabelField("", GUILayout.Width(-20F));
		// EditorGUILayout.LabelField("", GUILayout.Width(20F));
		// ShowRightClickMenu(
		// 	() => m_Position.vector3Value = Vector3.zero,
		// 	() => {
		// 		Vector3 localPosition = m_Position.vector3Value;
		// 		localPosition.x = Mathf.Round(localPosition.x * 100) * 0.01F;
		// 		localPosition.y = Mathf.Round(localPosition.y * 100) * 0.01F;
		// 		localPosition.z = Mathf.Round(localPosition.z * 100) * 0.01F;
		// 		m_Position.vector3Value = localPosition;
		// 	}
		// );
		// EditorGUILayout.EndHorizontal();
		//
		// EditorGUILayout.BeginHorizontal();
		// EditorGUILayout.LabelField("", GUILayout.Width(-20F));
		// EditorGUILayout.LabelField("", GUILayout.Width(20F));
		// ShowRightClickMenu(
		// 	() => m_Rotation.quaternionValue = Quaternion.identity,
		// 	() => {
		// 		Vector3 eulerAngles = m_Rotation.quaternionValue.eulerAngles;
		// 		eulerAngles.x = Mathf.Round(eulerAngles.x * 100) * 0.01F;
		// 		eulerAngles.y = Mathf.Round(eulerAngles.y * 100) * 0.01F;
		// 		eulerAngles.z = Mathf.Round(eulerAngles.z * 100) * 0.01F;
		// 		m_Rotation.quaternionValue = Quaternion.Euler(eulerAngles);
		// 	}
		// );
		// EditorGUILayout.EndHorizontal();
		//
		// EditorGUILayout.BeginHorizontal();
		// EditorGUILayout.LabelField("", GUILayout.Width(-20F));
		// EditorGUILayout.LabelField("", GUILayout.Width(20F));
		// ShowRightClickMenu(
		// 	() => m_Scale.vector3Value = Vector3.one,
		// 	() => {
		// 		Vector3 scale = m_Scale.vector3Value;
		// 		scale.x = Mathf.Round(scale.x * 100) * 0.01F;
		// 		scale.y = Mathf.Round(scale.y * 100) * 0.01F;
		// 		scale.z = Mathf.Round(scale.z * 100) * 0.01F;
		// 		m_Scale.vector3Value = scale;
		// 	}
		// );
		// EditorGUILayout.EndHorizontal();
		//
		// // EditorGUILayout.BeginHorizontal();
		// // bool fold = EditorPrefs.GetBool("Fold.TransformGlobal", false);
		// // EditorGUILayout.LabelField("", GUILayout.Width(fold ? -20F : -21F));
		// // EditorGUILayout.BeginVertical();
		// // EditorGUILayout.Space(1F);
		// // bool newFold = GUILayout.Toggle(fold, fold ? "\u25BA Global" : "\u25BC Global", EditorStyles.label);
		// // if (newFold != fold) {
		// // 	EditorPrefs.SetBool("Fold.TransformGlobal", newFold);
		// // }
		// // EditorGUILayout.EndVertical();
		// // EditorGUILayout.EndHorizontal();
		//
		// if (m_IsGlobalVisible) {
		// 	Vector3 position = (target as Transform)?.position ?? Vector3.zero;
		// 	EditorGUI.showMixedValue = Array.Exists(targets, o => o is Transform trans && trans.position != position);
		// 	EditorGUI.BeginChangeCheck();
		// 	Vector3 newPosition = EditorGUILayout.Vector3Field("GlobalPosition", position);
		// 	if (EditorGUI.EndChangeCheck()) {
		// 		foreach (var o in targets) {
		// 			if (o is Transform trans) {
		// 				trans.position = newPosition;
		// 				EditorUtility.SetDirty(trans);
		// 			}
		// 		}
		// 	}
		// 	EditorGUI.showMixedValue = false;
		// 	
		// 	Vector3 eulerAngles = (target as Transform)?.eulerAngles ?? Vector3.zero;
		// 	EditorGUI.showMixedValue = Array.Exists(targets, o => o is Transform trans && trans.eulerAngles != eulerAngles);
		// 	EditorGUI.BeginChangeCheck();
		// 	Vector3 newEulerAngles = EditorGUILayout.Vector3Field("GlobalRotation", eulerAngles);
		// 	if (EditorGUI.EndChangeCheck()) {
		// 		foreach (var o in targets) {
		// 			if (o is Transform trans) {
		// 				trans.eulerAngles = newEulerAngles;
		// 				EditorUtility.SetDirty(trans);
		// 			}
		// 		}
		// 	}
		// 	EditorGUI.showMixedValue = false;
		// 	
		// 	EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight * 2 - 6F);
		// 	EditorGUILayout.BeginHorizontal();
		// 	EditorGUILayout.LabelField("", GUILayout.Width(-20F));
		// 	EditorGUILayout.LabelField("", GUILayout.Width(20F));
		// 	ShowRightClickMenu(
		// 		() => {
		// 			foreach (var o in targets) {
		// 				if (o is Transform trans) {
		// 					trans.position = Vector3.zero;
		// 					EditorUtility.SetDirty(trans);
		// 				}
		// 			}
		// 		},
		// 		() => {
		// 			foreach (var o in targets) {
		// 				if (o is Transform trans) {
		// 					Vector3 pos = trans.position;
		// 					pos.x = Mathf.Round(pos.x * 100) * 0.01F;
		// 					pos.y = Mathf.Round(pos.y * 100) * 0.01F;
		// 					pos.z = Mathf.Round(pos.z * 100) * 0.01F;
		// 					trans.position = pos;
		// 					EditorUtility.SetDirty(trans);
		// 				}
		// 			}
		// 		}
		// 	);
		// 	EditorGUILayout.EndHorizontal();
		// 	
		// 	EditorGUILayout.BeginHorizontal();
		// 	EditorGUILayout.LabelField("", GUILayout.Width(-20F));
		// 	EditorGUILayout.LabelField("", GUILayout.Width(20F));
		// 	ShowRightClickMenu(
		// 		() => {
		// 			foreach (var o in targets) {
		// 				if (o is Transform trans) {
		// 					trans.eulerAngles = Vector3.zero;
		// 					EditorUtility.SetDirty(trans);
		// 				}
		// 			}
		// 		},
		// 		() => {
		// 			foreach (var o in targets) {
		// 				if (o is Transform trans) {
		// 					Vector3 angles = trans.eulerAngles;
		// 					angles.x = Mathf.Round(angles.x * 100) * 0.01F;
		// 					angles.y = Mathf.Round(angles.y * 100) * 0.01F;
		// 					angles.z = Mathf.Round(angles.z * 100) * 0.01F;
		// 					trans.eulerAngles = angles;
		// 					EditorUtility.SetDirty(trans);
		// 				}
		// 			}
		// 		}
		// 	);
		// 	EditorGUILayout.EndHorizontal();
		// }
	}
	//
	// private void ShowRightClickMenu(Action resetAction, Action roundAction) {
	// 	EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), Color.cyan);
	// 	Event e = Event.current;
	// 	if (e.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(e.mousePosition)) {
	// 		ShowMenu(resetAction, roundAction);
	// 		e.Use();
	// 	}
	// }
	//
	// private void ShowMenu(Action resetAction, Action roundAction) {
	// 	GenericMenu genericMenu = new GenericMenu();
	// 	if (resetAction != null) {
	// 		genericMenu.AddItem(new GUIContent("重置"), false, () => {
	// 			resetAction();
	// 			serializedObject.ApplyModifiedProperties();
	// 		});
	// 	}
	// 	if (resetAction != null) {
	// 		genericMenu.AddItem(new GUIContent("保留2位小数"), false, () => {
	// 			roundAction();
	// 			serializedObject.ApplyModifiedProperties();
	// 		});
	// 	}
	// 	genericMenu.AddItem(new GUIContent(m_IsGlobalVisible ? "隐藏Global" : "显示Global"), false, () => {
	// 		EditorPrefs.SetBool("Transform.IsGlobalVisible", m_IsGlobalVisible = !m_IsGlobalVisible);
	// 	});
	// 	genericMenu.ShowAsContext();
	// }
}