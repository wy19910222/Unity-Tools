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
using ComplexIDMap = System.ValueTuple<(string fromGUID, long fromFileID), (string toGUID, long toFileID)>;

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
		private const float LIST_ELEMENT_INFO_WIDTH = 30;
		
		private static readonly Color DROP_AREA_COLOR = new Color(0.2F, 0.4F, 0.6F, 0.7F);
		
		private static GUIStyle m_SwapBtnStyle;
		private static GUIStyle SwapBtnStyle => m_SwapBtnStyle ?? (m_SwapBtnStyle = new GUIStyle("Button") { fontSize = 16 });

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
		private Rect m_DropRect;

		private void Awake() {
			LoadMaps();
		}

		private void OnDestroy() {
			SaveMaps();
		}

		private void OnEnable() {
			m_List = new ReorderableList(m_ReplaceMaps, typeof(ReplaceMap), true, true, true, true) {
				drawHeaderCallback = rect => {
					// Header比Element左右各宽1像素，在这里对齐一下
					rect.x += 1;
					rect.width -= 2;
					
					float thumbWidth = m_List.draggable ? List_THUMB_WIDTH : 0;
					float headerWidth = rect.width + 7;	// 右边空白处也用起来
					// 左端拖拽区域宽度 + 原对象宽度 + 替换为宽度 + 右端添加按钮宽度
					float labelWidth = (headerWidth - thumbWidth - List_ADD_BUTTON_WIDTH - LIST_ELEMENT_INFO_WIDTH) * 0.5F;
					float swapBtnWidth = 24F;
					
					Rect leftRect = new Rect(rect.x + thumbWidth, rect.y, labelWidth - swapBtnWidth, rect.height);
					EditorGUI.LabelField(leftRect, "原引用");
					
					Rect swapBtnRect = new Rect(rect.x + thumbWidth + labelWidth - swapBtnWidth, rect.y - 1, swapBtnWidth, rect.height + 2);
					if (GUI.Button(swapBtnRect, "⇌", SwapBtnStyle)) {
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

					Rect infoRect = new Rect(rect.x + thumbWidth + labelWidth + labelWidth, rect.y, LIST_ELEMENT_INFO_WIDTH, rect.height);
					EditorGUI.LabelField(infoRect, "详情");

					Rect tailRect = new Rect(rect.x + headerWidth - List_ADD_BUTTON_WIDTH, rect.y - 1, List_ADD_BUTTON_WIDTH, rect.height + 2);
					if (GUI.Button(tailRect, EditorGUIUtility.IconContent("Toolbar Plus"))) {
						Undo.RecordObject(this, "ReferenceReplace.MapAdd");
						Undo.SetCurrentGroupName("ReferenceReplace.MapAdd");
						m_ReplaceMaps.Add(new ReplaceMap());
					}
				},
				drawElementCallback = (rect, index, isActive, isFocused) => {
					float labelWidth = (rect.width - List_ADD_BUTTON_WIDTH - LIST_ELEMENT_INFO_WIDTH + 7) * 0.5F;

					ReplaceMap map = m_ReplaceMaps[index];
					Rect leftRect = new Rect(rect.x, rect.y + 1, labelWidth - 2, rect.height - 2);
					UObject newFrom = EditorGUI.ObjectField(leftRect, map.from, typeof(UObject), true);
					Rect rightRect = new Rect(rect.x + labelWidth, rect.y + 1, labelWidth - 2, rect.height - 2);
					UObject newTo = EditorGUI.ObjectField(rightRect, map.to, typeof(UObject), true);
					if (newFrom != map.from || newTo != map.to) {
						Undo.RecordObject(this, "ReferenceReplace.MapUpdate");
						Undo.SetCurrentGroupName("ReferenceReplace.MapUpdate");
						map.from = newFrom;
						map.to = newTo;
						m_ReplaceMaps[index] = map;
					}

					Type fromType = map.from ? map.from.GetType() : null;
					Type toType = map.to ? map.to.GetType() : null;
					if (fromType != null && toType != null) {
						Rect infoRect = new Rect(rect.x + labelWidth + labelWidth, rect.y + 1, LIST_ELEMENT_INFO_WIDTH, rect.height - 2);
						if (!fromType.IsAssignableFrom(toType) && !toType.IsAssignableFrom(fromType)) {
							GUIContent content = new GUIContent() {
								image = EditorGUIUtility.FindTexture("console.warnicon.sml"),
								tooltip = "类型不匹配，可能并不是期望的映射关系"
							};
							EditorGUI.LabelField(infoRect, content, "CenteredLabel");
						} else if (map.from is GameObject fromGo && map.to is GameObject toGo) {
							GUIContent content = new GUIContent() {
								image = EditorGUIUtility.FindTexture("Toolbar Plus More"),
								tooltip = "将其组件加入到映射表"
							};
							if (GUI.Button(infoRect, content)) {
								OnAddCompsBtnClick(infoRect, fromGo, toGo, index);
							}
						}
					}

					Rect tailRect = new Rect(rect.x + rect.width - List_ADD_BUTTON_WIDTH + 8, rect.y + 1, List_ADD_BUTTON_WIDTH - 2, rect.height - 2);
					if (GUI.Button(tailRect, EditorGUIUtility.IconContent("Toolbar Minus"))) {
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
			Undo.undoRedoPerformed += Repaint;
		}

		private void OnDisable() {
			Undo.undoRedoPerformed -= Repaint;
		}

		private void OnAddCompsBtnClick(Rect btnRect, GameObject fromGo, GameObject toGo, int goIndex) {
			Component[] fromComps = fromGo.GetComponents<Component>();
			List<Component> toComps = new List<Component>(toGo.GetComponents<Component>());
			List<ReplaceMap> maps = new List<ReplaceMap>();
			for (int i = 0, lengthFrom = fromComps.Length; i < lengthFrom; ++i) {
				Component formComp = fromComps[i];
				Type fromCompType = formComp.GetType();
				for (int j = 0, lengthTo = toComps.Count; j < lengthTo; ++j) {
					Component toComp = toComps[j];
					Type toCompType = toComp.GetType();
					if (fromCompType.IsAssignableFrom(toCompType) || toCompType.IsAssignableFrom(fromCompType)) {
						maps.Add(new ReplaceMap() { from = formComp, to = toComp });
						toComps.RemoveAt(j);
						break;
					}
				}
			}
			GUIStyle style = "MenuToggleItem";
			float widthMax = 0;
			int mapCount = maps.Count;
			GUIContent[] displays = new GUIContent[mapCount];
			for (int i = 0; i < mapCount; i++) {
				GUIContent displayText =  EditorGUIUtility.TrTextContent($"{maps[i].from.GetType().Name} -> {maps[i].to.GetType().Name}");
				float width = style.CalcSize(displayText).x;
				if (width > widthMax) {
					widthMax = width;
				}
				displays[i] = displayText;
			}
			bool[] selected = new bool[mapCount];
			bool[] newSelected = new bool[mapCount];
			for (int i = 0; i < mapCount; ++i) {
				ReplaceMap map = maps[i];
				foreach (ReplaceMap _map in m_ReplaceMaps) {
					if (_map.from == map.from && _map.to == map.to) {
						selected[i] = true;
						newSelected[i] = true;
						break;
					}
				}
			}
			PopupWindow.Show(btnRect, new PopupContent() {
				Width = widthMax + 10,
				Height = mapCount * 19F + 10,
				OnGUIAction = popupRect => {
					for (int i = 0; i < mapCount; ++i) {
						Rect _rect = new Rect(popupRect.x + 5, popupRect.y + i * 19F + 6, popupRect.width - 10, 19F);
						newSelected[i] = GUI.Toggle(_rect, newSelected[i], displays[i], style);
					}
				},
				OnCloseAction = () => {
					bool added = false, removed = false;
					for (int i = 0; i < mapCount; ++i) {
						if (!selected[i] && newSelected[i]) {
							added = true;
						}
						if (selected[i] && !newSelected[i]) {
							removed = true;
						}
					}
					if (added) {
						if (removed) {
							Undo.RecordObject(this, "ReferenceReplace.MapUpdate");
							Undo.SetCurrentGroupName("ReferenceReplace.MapUpdate");
						} else {
							Undo.RecordObject(this, "ReferenceReplace.MapAdd");
							Undo.SetCurrentGroupName("ReferenceReplace.MapAdd");
						}
					} else {
						if (removed) {
							Undo.RecordObject(this, "ReferenceReplace.MapRemove");
							Undo.SetCurrentGroupName("ReferenceReplace.MapRemove");
						} else {
							return;
						}
					}

					int index = goIndex;
					for (int i = 0; i < mapCount; ++i) {
						ReplaceMap map = maps[i];
						int compIndex = -1;
						for (int j = 0, length = m_ReplaceMaps.Count; j < length; j++) {
							ReplaceMap _map = m_ReplaceMaps[j];
							if (_map.from == map.from && _map.to == map.to) {
								compIndex = j;
								break;
							}
						}
						if (newSelected[i]) {
							if (compIndex == -1) {
								m_ReplaceMaps.Insert(++index, map);
							} else {
								++index;
							}
						} else {
							if (compIndex != -1) {
								m_ReplaceMaps.RemoveAt(compIndex);
								if (compIndex < index) {
									index--;
								}
							}
						}
					}
				}
			});
		}

		private void OnGUI() {
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.ExpandHeight(false));
			Undo.RecordObject(this, "ReferenceReplace.MapUpdate");
			m_List.DoLayoutList();
			HandleListDragAndDrop();
			EditorGUILayout.EndScrollView();
			GUILayout.Space(-2F);
			EditorGUILayout.BeginHorizontal();
			// EditorGUILayout.BeginVertical();
			// if (GUILayout.Button("全选左边对象")) {
			// 	int count = m_ReplaceMaps.Count;
			// 	UObject[] objects = new UObject[count];
			// 	for (int i = 0; i < count; ++i) {
			// 		objects[i] = m_ReplaceMaps[i].from;
			// 	}
			// 	Selection.objects = objects;
			// }
			// if (GUILayout.Button("选中对象覆盖到左边")) {
			// 	Undo.RecordObject(this, "ReferenceReplace.InsertSelectionsToMapLeft");
			// 	Undo.SetCurrentGroupName("ReferenceReplace.InsertSelectionsToMapLeft");
			// 	int listCount = m_ReplaceMaps.Count;
			// 	int selectionCount = Selection.objects.Length;
			// 	for (int i = 0; i < selectionCount && i < listCount; ++i) {
			// 		ReplaceMap map = m_ReplaceMaps[i];
			// 		map.from = Selection.objects[i];
			// 		m_ReplaceMaps[i] = map;
			// 	}
			// 	for (int i = listCount; i < selectionCount; ++i) {
			// 		m_ReplaceMaps.Add(new ReplaceMap {
			// 			from = Selection.objects[i]
			// 		});
			// 	}
			// }
			// EditorGUILayout.EndVertical();
			// EditorGUILayout.BeginVertical();
			// if (GUILayout.Button("全选右边对象")) {
			// 	int count = m_ReplaceMaps.Count;
			// 	UObject[] objects = new UObject[count];
			// 	for (int i = 0; i < count; ++i) {
			// 		objects[i] = m_ReplaceMaps[i].to;
			// 	}
			// 	Selection.objects = objects;
			// }
			// if (GUILayout.Button("选中对象覆盖到右边")) {
			// 	Undo.RecordObject(this, "ReferenceReplace.InsertSelectionsToMapRight");
			// 	Undo.SetCurrentGroupName("ReferenceReplace.InsertSelectionsToMapRight");
			// 	int listCount = m_ReplaceMaps.Count;
			// 	int selectionCount = Selection.objects.Length;
			// 	for (int i = 0; i < selectionCount && i < listCount; ++i) {
			// 		ReplaceMap map = m_ReplaceMaps[i];
			// 		map.to = Selection.objects[i];
			// 		m_ReplaceMaps[i] = map;
			// 	}
			// 	for (int i = listCount; i < selectionCount; ++i) {
			// 		m_ReplaceMaps.Add(new ReplaceMap {
			// 			to = Selection.objects[i]
			// 		});
			// 	}
			// }
			// EditorGUILayout.EndVertical();
			// GUILayoutOption clearBtnHeight = GUILayout.Height(EditorGUIUtility.singleLineHeight * 2F + 4F);
			// if (GUILayout.Button(new GUIContent("清空\n列表"), clearBtnHeight)) {
			if (GUILayout.Button(new GUIContent("清空列表", EditorGUIUtility.FindTexture("TreeEditor.Trash")))) {
				Undo.RecordObject(this, "ReferenceReplace.MapClear");
				Undo.SetCurrentGroupName("ReferenceReplace.MapClear");
				m_ReplaceMaps.Clear();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10F);

			EditorGUIUtility.labelWidth = 70F;
			UObject newTarget = EditorGUILayout.ObjectField("操作目标", m_Target, typeof(UObject), true);
			if (newTarget != m_Target) {
				// if (newTarget && Utility.IsSceneObject(newTarget)) {
				// 	EditorUtility.DisplayDialog("错误", "只支持拖入文本型资产文件，如Scene、Prefab、Material等。", "确定");
				// } else {
					Undo.RecordObject(this, "ReferenceReplace.Target");
					Undo.SetCurrentGroupName("ReferenceReplace.Target");
					m_Target = newTarget;
				// }
			}

			// EditorGUILayout.BeginHorizontal();
			// if (GUILayout.Button("生成副本并替换"))) {
			// 	Replace(true);
			// }
			// if (GUILayout.Button("直接替换", GUILayout.Width(80F))) {
			// 	Replace(false);
			// }
			// EditorGUILayout.EndHorizontal();
			if (GUILayout.Button("替换引用", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2F + 2F))) {
				Replace(false);
			}
		}

		private void HandleListDragAndDrop() {
			if (Event.current.type == EventType.Repaint) {
				if (m_DropRect != default) {
					EditorGUI.DrawRect(m_DropRect, DROP_AREA_COLOR);
					m_DropRect = default;
				}
			}
			Rect listRect = GUILayoutUtility.GetLastRect();
			switch (Event.current.type) {
				case EventType.DragUpdated: {
					Vector2 mousePos = Event.current.mousePosition;
					if (listRect.Contains(mousePos)) {
						if (Event.current.control) {
							DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
						} else {
							DragAndDrop.visualMode = DragAndDropVisualMode.Move;
						}
						float deltaX = mousePos.x - listRect.x;
						float thumbWidth = m_List.draggable ? List_THUMB_WIDTH + 5 : 5;
						float leftWidth = (listRect.width + thumbWidth - LIST_ELEMENT_INFO_WIDTH - List_ADD_BUTTON_WIDTH) * 0.5F;
						bool isLeft = deltaX < leftWidth;
						float deltaY = mousePos.y - listRect.y - m_List.headerHeight - 4;
						float lineHeight = m_List.elementHeight + 2;
						float x = isLeft ? listRect.x + thumbWidth : listRect.x + leftWidth;
						float width = leftWidth - thumbWidth;
						float y, height;
						if (Event.current.control) {
							int index = Mathf.Clamp(Mathf.RoundToInt(deltaY / lineHeight), 0, m_List.count);
							y = listRect.y + m_List.headerHeight + 4 + index * lineHeight - 1;
							height = 2;
						} else {
							int index = Mathf.Clamp(Mathf.FloorToInt(deltaY / lineHeight), 0, m_List.count);
							int count = DragAndDrop.objectReferences.Length;
							y = listRect.y + m_List.headerHeight + 4 + index * lineHeight;
							height = Mathf.Min(count, Mathf.Max(m_List.count, 1) - index) * lineHeight;
						}
						m_DropRect = new Rect(x, y, width, height);
						Repaint();
					}
					break;
				}
				case EventType.DragPerform: {
					Vector2 mousePos = Event.current.mousePosition;
					if (listRect.Contains(mousePos)) {
						DragAndDrop.AcceptDrag();
						if (Event.current.control) {
							Undo.RecordObject(this, "ReferenceReplace.MapAdd");
							Undo.SetCurrentGroupName("ReferenceReplace.MapAdd");
						} else {
							Undo.RecordObject(this, "ReferenceReplace.MapUpdate");
							Undo.SetCurrentGroupName("ReferenceReplace.MapUpdate");
						}
						float deltaX = mousePos.x - listRect.x;
						float thumbWidth = m_List.draggable ? List_THUMB_WIDTH + 5 : 5;
						float leftWidth = (listRect.width + thumbWidth - LIST_ELEMENT_INFO_WIDTH - List_ADD_BUTTON_WIDTH) * 0.5F;
						bool isLeft = deltaX < leftWidth;
						float deltaY = mousePos.y - listRect.y - m_List.headerHeight - 4;
						float lineHeight = m_List.elementHeight + 2;
						if (Event.current.control) {
							int index = Mathf.Clamp(Mathf.RoundToInt(deltaY / lineHeight), 0, m_List.count);
							if (isLeft) {
								for (int i = 0, length = DragAndDrop.objectReferences.Length; i < length; ++i) {
									m_ReplaceMaps.Insert(index + i, new ReplaceMap() { from = DragAndDrop.objectReferences[i] });
								}
							} else {
								for (int i = 0, length = DragAndDrop.objectReferences.Length; i < length; ++i) {
									m_ReplaceMaps.Insert(index + i, new ReplaceMap() { to = DragAndDrop.objectReferences[i] });
								}
							}
						} else {
							int index = Mathf.Clamp(Mathf.FloorToInt(deltaY / lineHeight), 0, m_List.count);
							int count = DragAndDrop.objectReferences.Length;
							for (int i = m_ReplaceMaps.Count, length = index + count; i < length; i++) {
								m_ReplaceMaps.Add(new ReplaceMap());
							}
							if (isLeft) {
								for (int i = 0; i < count; i++) {
									ReplaceMap map = m_ReplaceMaps[index + i];
									map.from = DragAndDrop.objectReferences[i];
									m_ReplaceMaps[index + i] = map;
								}
							} else {
								for (int i = 0; i < count; i++) {
									ReplaceMap map = m_ReplaceMaps[index + i];
									map.to = DragAndDrop.objectReferences[i];
									m_ReplaceMaps[index + i] = map;
								}
							}
						}
					}
					break;
				}
			}
		}

		private void SaveMaps() {
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			List<string> pairs = new List<string>();
			foreach (ReplaceMap map in m_ReplaceMaps) {
				string fromGUID = null, toGUID = null;
				long fromFileID = 0, toFileID = 0;
				int fromInstanceID = 0, toInstanceID = 0;
				if (Utility.IsSceneObject(map.from)) {
					fromInstanceID = map.from.GetInstanceID();
				} else {
					fromGUID = Utility.GetGUID(map.from);
					fromFileID = Utility.GetFileID(map.from);
				}
				if (Utility.IsSceneObject(map.to)) {
					toInstanceID = map.to.GetInstanceID();
				} else {
					toGUID = Utility.GetGUID(map.to);
					toFileID = Utility.GetFileID(map.to);
				}
				pairs.Add($"{{\"from\":\"{fromGUID}_{fromFileID}_{fromInstanceID}\",\"to\":\"{toGUID}_{toFileID}_{toInstanceID}\"}}");
			}
			sb.Append(string.Join(",", pairs));
			sb.Append("]");
			string json = sb.ToString();
			// [{"from":"60d6fe07be344425ba61cb9d96cddd4d_5727705958130629547_42034","to":"813b7b02ec20473e916866a669053c5c_5593592556152845978_42034"}]
			EditorPrefs.SetString("ReferenceReplace.Maps", json);
		}
		private void LoadMaps() {
			m_ReplaceMaps.Clear();
			// [{"from":"60d6fe07be344425ba61cb9d96cddd4d_5727705958130629547_42034","to":"813b7b02ec20473e916866a669053c5c_5593592556152845978_42034"}]
			string json = EditorPrefs.GetString("ReferenceReplace.Maps", "{}");
			string pairs = json.Length < 2 ? string.Empty : json.Substring(1, json.Length - 2);
			if (pairs != string.Empty) {
				foreach (string pair in Regex.Split(pairs, "(?<=}),(?={)")) {
					try {
						string fromID = Regex.Match(pair, "(?<=\"from\":\")\\w{0,32}_\\w+_\\w+(?=\")").Value;
						string[] fromIDParts = fromID.Split('_');
						string fromGUID = fromIDParts[0];
						long fromFileID = long.Parse(fromIDParts[1]);
						int fromInstanceID = int.Parse(fromIDParts[2]);
						string toID = Regex.Match(pair, "(?<=\"to\":\")\\w{0,32}_\\w+_\\w+(?=\")").Value;
						string[] toIDParts = toID.Split('_');
						string toGUID = toIDParts[0];
						long toFileID = long.Parse(toIDParts[1]);
						int toInstanceID = int.Parse(toIDParts[2]);
						m_ReplaceMaps.Add(new ReplaceMap {
							from = Utility.GetObject(fromGUID, fromFileID, fromInstanceID),
							to = Utility.GetObject(toGUID, toFileID, toInstanceID)
						});
					} catch (Exception e) {
						Debug.LogError(e);
					}
				}
			}
		}

		private void Replace(bool clone) {
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
				prefabSet.Clear();
				if (prefabComps.Count > 0) {
					foreach (Component comp in prefabComps) {
						SerializedObject serializedObject = new SerializedObject(comp);
						SerializedProperty property = serializedObject.GetIterator();
						bool isExist = false;
						while (property.Next(true)) {
							if (property.propertyType == SerializedPropertyType.ObjectReference) {
								foreach (ReplaceMap map in m_ReplaceMaps) {
									if (property.objectReferenceValue == map.from) {
										isExist = true;
										break;
									}
								}
								if (isExist) {
									break;
								}
							}
						}
						if (isExist) {
							GameObject subPrefab = PrefabUtility.GetNearestPrefabInstanceRoot(comp);
							if (subPrefab) {
								prefabSet.Add(subPrefab);
							}
						}
					}
				}
				if (prefabSet.Count > 0) {
					EditorUtility.DisplayDialog("警告", "部分引用位于prefab内部，请根据log选择相应prefab文件执行此操作。", "确定");
					Debug.Log($"部分引用位于以下{prefabSet.Count}个prefab中：");
					foreach (GameObject prefab in prefabSet) {
						Debug.Log(prefab, PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab));
					}
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
			
			List<ComplexIDMap> idMaps = ReplaceMapsToIDMaps(m_ReplaceMaps);
				
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
						if (ReplaceInFile(filePath, outputPath, idMaps)) {
							Debug.Log("Replace succeeded:" + filePath);
							count++;
						} else {
							Debug.LogWarning("Replace failed:" + filePath);
						}
					}
				}
			} else if (File.Exists(targetPath)) {
				// 如果是文件，则操作该文件
				string outputPath = clone ? Utility.GetOutputFilePath(targetPath) : targetPath;
				if (ReplaceInFile(targetPath, outputPath, idMaps)) {
					Debug.Log("Replace succeeded:" + targetPath);
					count++;
				} else {
					Debug.LogWarning("Replace failed:" + targetPath);
				}
			}
			
			// 刷新
			AssetDatabase.Refresh();
			string text = $"{(clone ? "克隆" : "改动")}完成，{count}个资源被替换。";
			ShowNotification(EditorGUIUtility.TrTextContent(text), 1);
			Debug.Log(text);
		}

		private static int ReplaceInObject(UObject obj, IReadOnlyList<(UObject, UObject)> objMaps) {
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

		private static bool ReplaceInModification(PropertyModification modification, IEnumerable<(UObject, UObject)> objMaps, IEnumerable<UObject> objsInPrefab = null) {
			if (modification.target && modification.objectReference) {
				foreach ((UObject from, UObject to) in objMaps) {
					if (modification.objectReference == from) {
						modification.objectReference = to;
						UObject target = modification.target;
						if (objsInPrefab != null) {
							foreach (UObject obj in objsInPrefab) {
								if (PrefabUtility.GetCorrespondingObjectFromSource(obj) == target) {
									target = obj;
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

		private static bool ReplaceInFile(string srcFilePath, string dstFilePath, IEnumerable<ComplexIDMap> idMaps) {
			if (Utility.IsYamlFile(srcFilePath)) {
				// 如果是YAML语法的文本文件，说明是Unity序列化文件，meta文件只存自己的GUID，不需要管
				return CopyAndReplace(srcFilePath, dstFilePath, idMaps);
			} else if (srcFilePath.EndsWith(".asmdef") || srcFilePath.EndsWith(".asmref")) {
				// 如果是AssemblyDefinition或AssemblyDefinitionReference文件，meta文件只存自己的GUID，不需要管
				return CopyAndReplace(srcFilePath, dstFilePath, idMaps, "GUID:{0}");
			} else {
				// 如果是非Unity序列化文件，引用GUID存在meta文件内，应该操作meta文件而不是资源文件
				string srcMetaFilePath = srcFilePath + ".meta";
				string dstMetaFilePath = dstFilePath + ".meta";
				string guid = Utility.GetGUIDFromMetaFile(srcMetaFilePath);
				bool done = CopyAndReplace(srcMetaFilePath, dstMetaFilePath, idMaps, guid);
				if (done && dstFilePath != srcFilePath) {
					File.Copy(srcFilePath, dstFilePath, true);
				}
				return done;
			}
		}

		private static bool CopyAndReplace(string srcFilePath, string dstFilePath, IEnumerable<ComplexIDMap> idMaps, string pattern = "fileID: {1}, guid: {0}") {
			string text = Utility.ReadAllText(srcFilePath);
			bool done = false;
			foreach (((string fromGUID, long fromFileID), (string toGUID, long toFileID)) in idMaps) {
				string fromStr = string.Format(pattern, fromGUID, fromFileID);
				done = done || text.Contains(fromStr);
				text = text.Replace(fromStr, string.Format(pattern, toGUID, toFileID));
			}
			return done && Utility.WriteAllText(dstFilePath, text);
		}

		private static List<(UObject from, UObject to)> ReplaceMapsToObjectMaps(IReadOnlyList<ReplaceMap> objectMaps) {
			int mapCount = objectMaps.Count;
			List<(UObject, UObject)> objMaps = new List<(UObject, UObject)>(mapCount);
			for (int i = 0; i < mapCount; ++i) {
				ReplaceMap map = objectMaps[i];
				if (map.from && map.to) {
					objMaps.Add((map.from, map.to));
				}
			}
			return objMaps;
		}

		private static List<ComplexIDMap> ReplaceMapsToIDMaps(IReadOnlyList<ReplaceMap> objectMaps) {
			int mapCount = objectMaps.Count;
			List<ComplexIDMap> guidMaps = new List<ComplexIDMap>(mapCount);
			for (int i = 0; i < mapCount; ++i) {
				ReplaceMap map = objectMaps[i];
				if (map.from && map.to) {
					string fromGUID = Utility.GetGUID(map.from);
					string toGUID = Utility.GetGUID(map.to);
					if (!string.IsNullOrEmpty(fromGUID) && !string.IsNullOrEmpty(toGUID)) {
						long fromFileID = Utility.GetFileID(map.from);
						long toFileID = Utility.GetFileID(map.to);
						guidMaps.Add(((fromGUID, fromFileID), (toGUID, toFileID)));
					}
				}
			}
			return guidMaps;
		}
	}

	public class PopupContent : PopupWindowContent {
		public float Width { get; set; }
		public float Height { get; set; }
		public Action<Rect> OnGUIAction { get; set; }
		public Action OnOpenAction { get; set; }
		public Action OnCloseAction { get; set; }

		public override Vector2 GetWindowSize() => new Vector2(Width, Height);

		public override void OnGUI(Rect rect) => OnGUIAction?.Invoke(rect);

		public override void OnOpen() => OnOpenAction?.Invoke();

		public override void OnClose() => OnCloseAction?.Invoke();
	}
}
