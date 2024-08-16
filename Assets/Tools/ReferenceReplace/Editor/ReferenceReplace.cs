/*
 * @Author: wangyun
 * @CreateTime: 2022-06-06 13:07:54 746
 * @LastEditor: wangyun
 * @EditTime: 2024-06-29 00:55:12 339
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using UObject = UnityEngine.Object;

namespace WYTools.ReferenceReplace {
	public class ReferenceReplace : EditorWindow {
		[MenuItem("Tools/WYTools/Reference Replace")]
		private static void Init() {
			ReferenceReplace window = GetWindow<ReferenceReplace>();
			window.minSize = new Vector2(200F, 200F);
			window.Show();
		}
		
		private const float List_THUMB_WIDTH = 14;
		private const float List_ADD_BUTTON_WIDTH = 30;

		[Serializable]
		private struct ReplaceMap {
			public UObject from;
			public UObject to;
		}

		[SerializeField]
		private List<ReplaceMap> m_ReplaceMaps = new List<ReplaceMap>();
		[SerializeField]
		private UObject m_Target;

		private ReorderableList m_List;
		private Vector2 m_ScrollPos = Vector2.zero;
		
		private static GUIStyle m_SwapBtnStyle;

		private void OnEnable() {
			LoadMaps();

			m_List = new ReorderableList(m_ReplaceMaps, typeof(ReplaceMap), true, true, true, true) {
				drawHeaderCallback = rect => {
					// Header比Element左右各宽1像素，在这里对齐一下
					rect.x += 1;
					rect.width -= 2;
					
					float thumbWidth = m_List.draggable ? List_THUMB_WIDTH : 0;
					float headerWidth = rect.width + 7;	// 右边空白处也用起来
					// 左端拖拽区域宽度 + 原对象宽度 + 替换为宽度 + 右端添加按钮宽度
					float labelWidth = (headerWidth - thumbWidth - List_ADD_BUTTON_WIDTH) * 0.5F;
					float swapBtnWidth = 24F;
					
					Rect leftRect = new Rect(rect.x + thumbWidth, rect.y, labelWidth - swapBtnWidth, rect.height);
					EditorGUI.LabelField(leftRect, "原对象");
					
					Rect middleRect = new Rect(rect.x + thumbWidth + labelWidth - swapBtnWidth, rect.y - 1, swapBtnWidth, rect.height + 2);
					m_SwapBtnStyle ??= new GUIStyle("Button") { fontSize = 16 };
					if (GUI.Button(middleRect, "⇌", m_SwapBtnStyle)) {
						Undo.RecordObject(this, "ReferenceReplace.MapSwap");
						Undo.SetCurrentGroupName("ReferenceReplace.MapSwap");
						for (int i = 0, length = m_ReplaceMaps.Count; i < length; ++i) {
							ReplaceMap map = m_ReplaceMaps[i];
							(map.from, map.to) = (map.to, map.from);
							m_ReplaceMaps[i] = map;
						}
					}

					Rect rightRect = new Rect(rect.x + thumbWidth + labelWidth, rect.y, labelWidth, rect.height);
					EditorGUI.LabelField(rightRect, "替换为");

					Rect tailRect = new Rect(rect.x + headerWidth - List_ADD_BUTTON_WIDTH, rect.y - 1, List_ADD_BUTTON_WIDTH, rect.height + 2);
					if (GUI.Button(tailRect, "+")) {
						Undo.RecordObject(this, "ReferenceReplace.MapAdd");
						Undo.SetCurrentGroupName("ReferenceReplace.MapAdd");
						m_ReplaceMaps.Add(new ReplaceMap());
					}
				},
				drawElementCallback = (rect, index, isActive, isFocused) => {
					const float BUTTON_WIDTH = 30;
					float LABEL_WIDTH = (rect.width - BUTTON_WIDTH + 7) * 0.5F;

					ReplaceMap map = m_ReplaceMaps[index];
					Rect leftRect = new Rect(rect.x, rect.y + 1, LABEL_WIDTH - 2, rect.height - 2);
					UObject newFrom = EditorGUI.ObjectField(leftRect, map.from, typeof(UObject), true);
					Rect rightRect = new Rect(rect.x + LABEL_WIDTH, rect.y + 1, LABEL_WIDTH, rect.height - 2);
					UObject newTo = EditorGUI.ObjectField(rightRect, map.to, typeof(UObject), true);
					if (newFrom != map.from || newTo != map.to) {
						Undo.RecordObject(this, "ReferenceReplace.MapUpdate");
						Undo.SetCurrentGroupName("ReferenceReplace.MapUpdate");
						map.from = newFrom;
						map.to = newTo;
						m_ReplaceMaps[index] = map;
					}

					Rect tailRect = new Rect(rect.x + rect.width - BUTTON_WIDTH + 8, rect.y + 1, BUTTON_WIDTH - 2, rect.height - 2);
					if (GUI.Button(tailRect, "×")) {
						EditorApplication.delayCall += () => {
							Undo.RecordObject(this, "ReferenceReplace.MapRemove");
							Undo.SetCurrentGroupName("ReferenceReplace.MapRemove");
							m_ReplaceMaps.RemoveAt(index);
							Repaint();
						};
					}
				},
				elementHeight = 20, footerHeight = 0
			};
		}

		private void OnDisable() {
			SaveMaps();
		}

		private void OnGUI() {
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.ExpandHeight(false));
			Undo.RecordObject(this, "ReferenceReplace.MapUpdate");
			m_List.DoLayoutList();
			EditorGUILayout.EndScrollView();
			GUILayout.Space(-2F);
			EditorGUILayout.BeginHorizontal();
			GUILayoutOption clearBtnHeight = GUILayout.Height(EditorGUIUtility.singleLineHeight * 2F + 4F);
			if (GUILayout.Button(new GUIContent("清空\n列表"), clearBtnHeight)) {
				Undo.RecordObject(this, "ReferenceReplace.MapClear");
				Undo.SetCurrentGroupName("ReferenceReplace.MapClear");
				m_ReplaceMaps.Clear();
			}
			EditorGUILayout.BeginVertical();
			if (GUILayout.Button("全选左边对象")) {
				int count = m_ReplaceMaps.Count;
				UObject[] objects = new UObject[count];
				for (int i = 0; i < count; ++i) {
					objects[i] = m_ReplaceMaps[i].from;
				}
				Selection.objects = objects;
			}
			if (GUILayout.Button("选中对象覆盖到左边")) {
				Undo.RecordObject(this, "ReferenceReplace.InsertSelectionsToMapLeft");
				Undo.SetCurrentGroupName("ReferenceReplace.InsertSelectionsToMapLeft");
				int listCount = m_ReplaceMaps.Count;
				int selectionCount = Selection.objects.Length;
				for (int i = 0; i < selectionCount && i < listCount; ++i) {
					ReplaceMap map = m_ReplaceMaps[i];
					map.from = Selection.objects[i];
					m_ReplaceMaps[i] = map;
				}
				for (int i = listCount; i < selectionCount; ++i) {
					m_ReplaceMaps.Add(new ReplaceMap {
						from = Selection.objects[i]
					});
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical();
			if (GUILayout.Button("全选右边对象")) {
				int count = m_ReplaceMaps.Count;
				UObject[] objects = new UObject[count];
				for (int i = 0; i < count; ++i) {
					objects[i] = m_ReplaceMaps[i].to;
				}
				Selection.objects = objects;
			}
			if (GUILayout.Button("选中对象覆盖到右边")) {
				Undo.RecordObject(this, "ReferenceReplace.InsertSelectionsToMapRight");
				Undo.SetCurrentGroupName("ReferenceReplace.InsertSelectionsToMapRight");
				int listCount = m_ReplaceMaps.Count;
				int selectionCount = Selection.objects.Length;
				for (int i = 0; i < selectionCount && i < listCount; ++i) {
					ReplaceMap map = m_ReplaceMaps[i];
					map.to = Selection.objects[i];
					m_ReplaceMaps[i] = map;
				}
				for (int i = listCount; i < selectionCount; ++i) {
					m_ReplaceMaps.Add(new ReplaceMap {
						to = Selection.objects[i]
					});
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10F);

			EditorGUIUtility.labelWidth = 70F;
			UObject newTarget = EditorGUILayout.ObjectField("替换目标", m_Target, typeof(UObject), true);
			if (newTarget != m_Target) {
				// if (newTarget && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(newTarget))) {
				// 	EditorUtility.DisplayDialog("错误", "只支持拖入文本型资产文件，如Scene、Prefab、Material等。", "确定");
				// } else {
					Undo.RecordObject(this, "ReferenceReplace.Target");
					Undo.SetCurrentGroupName("ReferenceReplace.Target");
					m_Target = newTarget;
				// }
			}

			EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
			if (GUILayout.Button("生成副本并替换")) {
				Replace(true);
			}
			if (GUILayout.Button("直接替换", GUILayout.Width(80F))) {
				Replace(false);
			}
			EditorGUILayout.EndHorizontal();
		}

		private void SaveMaps() {
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			List<string> pairs = new List<string>();
			foreach (ReplaceMap map in m_ReplaceMaps) {
				string fromPath = AssetDatabase.GetAssetPath(map.from);
				string fromGUID = AssetDatabase.AssetPathToGUID(fromPath);
				string toPath = AssetDatabase.GetAssetPath(map.to);
				string toGUID = AssetDatabase.AssetPathToGUID(toPath);
				pairs.Add($"{{\"from\":\"{fromGUID}\",\"to\":\"{toGUID}\"}}");
			}
			sb.Append(string.Join(",", pairs));
			sb.Append("]");
			string json = sb.ToString();
			// [{"from":"26512af483b2a2c468a185b4aab5d1a5","to":"26512af483b2a2c468a185b4aab5d1a5"}]
			EditorPrefs.SetString("ReferenceReplace.Maps", json);
		}
		private void LoadMaps() {
			m_ReplaceMaps.Clear();
			// [{"from":"26512af483b2a2c468a185b4aab5d1a5","to":"26512af483b2a2c468a185b4aab5d1a5"}]
			string json = EditorPrefs.GetString("ReferenceReplace.Maps", "{}");
			string pairs = json.Length < 2 ? string.Empty : json.Substring(1, json.Length - 2);
			if (pairs != string.Empty) {
				foreach (string pair in Regex.Split(pairs, "(?<=}),(?={)")) {
					string fromGUID = Regex.Match(pair, "(?<=\"from\":\")\\w{0,32}(?=\")").Value;
					string toGUID = Regex.Match(pair, "(?<=\"to\":\")\\w{0,32}(?=\")").Value;
					string fromPath = AssetDatabase.GUIDToAssetPath(fromGUID);
					UObject from = AssetDatabase.LoadAssetAtPath<UObject>(fromPath);
					string toPath = AssetDatabase.GUIDToAssetPath(toGUID);
					UObject to = AssetDatabase.LoadAssetAtPath<UObject>(toPath);
					m_ReplaceMaps.Add(new ReplaceMap {from = from, to = to});
				}
			}
		}

		private void Replace(bool clone) {
			if (EditorSettings.serializationMode != SerializationMode.ForceText) {
				if (!EditorUtility.DisplayDialog("警告", "当前序列化模式非「Force Text」，是否将Asset Serialization Mode设置成「Force Text」并继续？", "确定", "取消")) {
					return;
				}
				// SettingsService.OpenProjectSettings("Project/Editor");
				// // GetWindow<ProjectSettingsWindow>().m_SearchText = "Mode";
				// Type editorType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectSettingsWindow");
				// EditorWindow projectSettingsWindow = GetWindow(editorType);
				// FieldInfo searchTextFI = editorType?.BaseType?.GetField("m_SearchText", BindingFlags.Instance | BindingFlags.NonPublic);
				// searchTextFI?.SetValue(projectSettingsWindow, "Mode");
				// return;
				EditorSettings.serializationMode = SerializationMode.ForceText;
			}

			string targetPath = AssetDatabase.GetAssetPath(m_Target);
			if (string.IsNullOrEmpty(targetPath)) {
				ReplaceSceneObject(m_Target, clone);
			} else {
				ReplaceAsset(targetPath, clone);
			}
		}

		private void ReplaceSceneObject(UObject target, bool clone) {
			List<(UObject from, UObject to)> objMaps = ReplaceMapsToObjectMaps(m_ReplaceMaps);
			if (target is GameObject go) {
				if (PrefabUtility.IsPartOfAnyPrefab(go)) {
					EditorUtility.DisplayDialog("警告", "目标对象属于Prefab，请选择Prefab文件执行此操作。", "确定");
					return;
				}
				// 克隆
				if (clone) {
					UObject prevSelectedObject = Selection.activeObject;
					Selection.activeGameObject = go;
					Unsupported.DuplicateGameObjectsUsingPasteboard();
					GameObject dstGo = Selection.activeGameObject;
					Selection.activeObject = prevSelectedObject;
					Transform srcTrans = go.transform;
					Transform dstTrans = dstGo.transform;
					dstTrans.SetParent(srcTrans.parent);
					dstTrans.localPosition = srcTrans.localPosition;
					dstTrans.localRotation = srcTrans.localRotation;
					dstTrans.localScale = srcTrans.localScale;
					go = dstGo;
				}
				// 遍历所有组件，分成不在Prefab中的组件、在Prefab中的组件，另外记录所有根Prefab
				Component[] comps = go.GetComponentsInChildren<Component>(true);
				List<Component> selfComps = new List<Component>();
				List<Component> prefabComps = new List<Component>();
				HashSet<GameObject> prefabSet = new HashSet<GameObject>();
				foreach (var comp in comps) {
					if (!PrefabUtility.IsPartOfAnyPrefab(comp)) {
						selfComps.Add(comp);
					} else {
						prefabComps.Add(comp);
						GameObject subPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(comp);
						if (subPrefab) {
							prefabSet.Add(subPrefab);
						}
					}
				}
				// 遍历不在Prefab中的组件，替换引用
				int referenceChangedCount = 0;
				int selfCompChangedCount = 0;
				if (selfComps.Count > 0) {
					foreach (Component comp in selfComps) {
						int changeCount = ReplaceInObject(comp, objMaps);
						if (changeCount > 0) {
							referenceChangedCount += changeCount;
							selfCompChangedCount++;
						}
					}
				}
				// 遍历对Prefab的修改，替换引用
				int modificationCompChangedCount = 0;
				if (prefabSet.Count > 0) {
					HashSet<UObject> targetSet = new HashSet<UObject>();
					foreach (GameObject prefab in prefabSet) {
						PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(prefab);
						bool anyModificationChanged = false;
						foreach (PropertyModification modification in modifications) {
							bool changed = ReplaceInModification(modification, objMaps, prefabComps);
							if (changed) {
								anyModificationChanged = true;
								referenceChangedCount++;
								targetSet.Add(modification.target);
							}
						}
						if (anyModificationChanged) {
							PrefabUtility.SetPropertyModifications(prefab, modifications);
						}
					}
					modificationCompChangedCount = targetSet.Count;
				}
				// 遍历在Prefab中的组件，如果有需要替换的，则弹窗提示
				bool existInPrefab = false;
				if (prefabComps.Count > 0) {
					foreach (Component comp in prefabComps) {
						SerializedObject serializedObject = new SerializedObject(comp);
						SerializedProperty property = serializedObject.GetIterator();
						while (property.Next(true)) {
							if (property.propertyType == SerializedPropertyType.ObjectReference) {
								foreach (ReplaceMap map in m_ReplaceMaps) {
									if (property.objectReferenceValue == map.from) {
										existInPrefab = true;
										break;
									}
								}
								if (existInPrefab) {
									break;
								}
							}
						}
						if (existInPrefab) {
							break;
						}
					}
				}
				if (existInPrefab) {
					EditorUtility.DisplayDialog("警告", "部分引用位于Prefab内部，请选择相应Prefab文件执行此操作。", "确定");
				}
				// 如果没有改动，则删除克隆的对象
				if (clone && referenceChangedCount <= 0) {
					DestroyImmediate(go);
				}
				// 完成提示
				string text = $"{(clone ? "克隆" : "改动")}完成，{selfCompChangedCount + modificationCompChangedCount}个对象中的{referenceChangedCount}个引用被替换。";
				ShowNotification(EditorGUIUtility.TrTextContent(text), 1);
				Debug.Log(text);
			} else {
				if (clone) {
					target = Instantiate(target);
				}
				int changeCount = ReplaceInObject(target, objMaps);
				// 如果没有改动，则删除克隆的对象
				if (clone && changeCount <= 0) {
					DestroyImmediate(target);
				}
				// 完成提示
				string text = $"{(clone ? "克隆" : "改动")}完成，1个对象中的{changeCount}个引用被替换。";
				ShowNotification(EditorGUIUtility.TrTextContent(text), 1);
				Debug.Log(text);
			}
		}

		private void ReplaceAsset(string targetPath, bool clone) {
			List<(string fromGUID, string toGUID)> guidMaps = ReplaceMapsToGUIDMaps(m_ReplaceMaps);
				
			// 替换操作
			int targetPathLength = targetPath.Length;
			int count = 0;
			if (Directory.Exists(targetPath)) {
				// 如果是文件夹，则遍历操作文件夹内所有非meta文件
				string outputDir = clone ? Utility.GetOutputDirPath(targetPath) : targetPath;
				string[] filePaths = Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories);
				foreach (string filePath in filePaths) {
					if (!filePath.EndsWith(".meta")) {
						string outputPath = clone ? outputDir + filePath.Substring(targetPathLength) : filePath;
						if (ReplaceInFile(filePath, outputPath, guidMaps)) {
							count++;
						}
					}
				}
			} else if (File.Exists(targetPath)) {
				// 如果是文件，则操作该文件
				string outputPath = clone ? Utility.GetOutputFilePath(targetPath) : targetPath;
				if (ReplaceInFile(targetPath, outputPath, guidMaps)) {
					count++;
				}
			}
			
			// 刷新
			AssetDatabase.Refresh();
			string text = $"{(clone ? "克隆" : "改动")}完成，{count}个资源被替换。";
			ShowNotification(EditorGUIUtility.TrTextContent(text), 1);
			Debug.Log(text);
		}

		private static int ReplaceInObject(UObject obj, IReadOnlyList<(UObject from, UObject to)> objMaps) {
			SerializedObject serializedObject = new SerializedObject(obj);
			SerializedProperty property = serializedObject.GetIterator();
			int changedCount = 0;
			while (property.Next(true)) {
				if (property.propertyType == SerializedPropertyType.ObjectReference) {
					foreach ((UObject from, UObject to) in objMaps) {
						if (property.objectReferenceValue == @from) {
							property.objectReferenceValue = to;
							changedCount++;
							Debug.Log($"{obj}的{property.propertyPath}被替换。", obj);
							break;
						}
					}
				}
			}
			if (changedCount > 0) {
				Undo.RecordObject(obj, "ReferenceReplace.Replace");
				Undo.SetCurrentGroupName("ReferenceReplace.Replace");
				serializedObject.ApplyModifiedProperties();
			}
			serializedObject.Dispose();
			return changedCount;
		}

		private static bool ReplaceInModification(PropertyModification modification, IEnumerable<(UObject from, UObject to)> objMaps, IEnumerable<UObject> objsInPrefab = null) {
			if (modification.target && modification.objectReference) {
				foreach ((UObject from, UObject to) in objMaps) {
					if (modification.objectReference == from) {
						modification.objectReference = to;
						UObject target = modification.target;
						if (objsInPrefab != null) {
							foreach (UObject prefabComp in objsInPrefab) {
								if (PrefabUtility.GetCorrespondingObjectFromSource(prefabComp) == target) {
									target = prefabComp;
									Undo.RecordObject(target, "ReferenceReplace.Replace");
									Undo.SetCurrentGroupName("ReferenceReplace.Replace");
									break;
								}
							}
						}
						Debug.Log($"Modification: {target}的{modification.propertyPath}被替换。", target);
						return true;
					}
				}
			}
			return false;
		}

		private static bool ReplaceInFile(string srcFilePath, string dstFilePath, IEnumerable<(string fromGUID, string toGUID)> guidMaps) {
			// 检查是不是YAML语法的文本文件
			if (!Utility.IsYamlFile(srcFilePath)) {
				return false;
			}
			// 读取
			string text = Utility.ReadAllText(srcFilePath);
			// 替换
			bool done = false;
			foreach ((string fromGUID, string toGUID) in guidMaps) {
				done = done || text.Contains(fromGUID);
				text = text.Replace(fromGUID, toGUID);
			}
			// 写入
			return done && Utility.WriteAllText(dstFilePath, text);
		}

		private static List<(UObject from, UObject to)> ReplaceMapsToObjectMaps(IReadOnlyList<ReplaceMap> objectMaps) {
			int mapCount = objectMaps.Count;
			List<(UObject from, UObject to)> objMaps = new List<(UObject from, UObject to)>(mapCount);
			for (int i = 0; i < mapCount; ++i) {
				ReplaceMap map = objectMaps[i];
				if (map.from && map.to) {
					objMaps.Add((map.from, map.to));
				}
			}
			return objMaps;
		}

		private static List<(string fromGUID, string toGUID)> ReplaceMapsToGUIDMaps(IReadOnlyList<ReplaceMap> objectMaps) {
			int mapCount = objectMaps.Count;
			List<(string fromGUID, string toGUID)> guidMaps = new List<(string fromGUID, string toGUID)>(mapCount);
			for (int i = 0; i < mapCount; ++i) {
				ReplaceMap map = objectMaps[i];
				if (map.from && map.to) {
					string fromGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map.from));
					string toGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map.to));
					if (!string.IsNullOrEmpty(fromGUID) && !string.IsNullOrEmpty(toGUID)) {
						guidMaps.Add((fromGUID, toGUID));
					}
				}
			}
			return guidMaps;
		}
	}
}