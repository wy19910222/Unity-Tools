/*
 * @Author: wangyun
 * @CreateTime: 2023-04-16 19:19:49 528
 * @LastEditor: wangyun
 * @EditTime: 2023-04-16 19:19:49 533
 */

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;


using UObject = UnityEngine.Object;

namespace TransformSearch {
	public class SearchReferenceInScene : BaseSearch {
		[MenuItem("Tools/TransformSearch/SearchReferenceInScene")]
		private static void Init() {
			SearchReferenceInScene window = GetWindow<SearchReferenceInScene>("ReferenceInSceneSearch");
			window.minSize = new Vector2(200F, 200F);
			window.Show();
		}

		private readonly List<UObject> m_Targets = new List<UObject>();
		private GameObject m_TargetGo;
		private bool m_LockTargets;

		protected override void OnEnable() {
			base.OnEnable();
			Selection.selectionChanged += GetReferenceTargets;
		}

		protected override void OnDisable() {
			base.OnDisable();
			Selection.selectionChanged -= GetReferenceTargets;
		}

		private void GetReferenceTargets() {
			if (!m_LockTargets) {
				if (Selection.objects.Length == 1 && Selection.objects[0] is GameObject singleGO) {
					if (singleGO != m_TargetGo) {
						m_TargetGo = singleGO;
						m_Targets.Clear();
						m_Targets.Add(m_TargetGo);
						m_Targets.AddRange(m_TargetGo.GetComponents<Component>());
					}
				} else {
					m_TargetGo = null;
					m_Targets.Clear();
					HashSet<UObject> selections = new HashSet<UObject>();
					foreach (var obj in Selection.objects) {
						switch (obj) {
							case DefaultAsset da: {
								string path = AssetDatabase.GetAssetPath(da);
								if (Directory.Exists(path)) {
									var filePaths = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
									foreach (var filePath in filePaths) {
										if (filePath.EndsWith(".prefab")) {
											GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
											if (go) {
												selections.Add(go);
											}
										}
									}
								} else {
									selections.Add(obj);
								}
								break;
							}
							case GameObject go:
								selections.Add(go);
								break;
						}
					}
					m_Targets.AddRange(selections);
				}
			}
		}

		protected override List<UObject> Match(Transform trans) {
			List<UObject> comps = new List<UObject>();
			foreach (var comp in trans.GetComponents<Component>()) {
				if (IsReferenced(comp)) {
					comps.Add(comp);
				}
			}
			return comps;
		}

		protected override void DrawHeader() {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(m_TargetGo ? "搜索目标：" + m_TargetGo.name : "搜索目标：选中的" + m_Targets.Count + "个GO");
			if (GUILayout.Button(m_LockTargets ? "重新选择目标" : "搜索", GUILayout.MinWidth(60F))) {
				if (m_LockTargets) {
					m_LockTargets = false;
					GetReferenceTargets();
				} else if (m_Targets.Count > 0) {
					m_LockTargets = true;
					Search();
				}
			}
			EditorGUILayout.EndHorizontal();
			if (m_TargetGo) {
				// 单选模式，精确选择引用目标
				bool isSelectedGo = m_Targets.Contains(m_TargetGo);
				bool newIsSelectedGo = EditorGUILayout.ToggleLeft("[GameObject]", isSelectedGo);
				switch (newIsSelectedGo) {
					case true when !isSelectedGo:
						m_Targets.Add(m_TargetGo);
						break;
					case false when isSelectedGo:
						m_Targets.Remove(m_TargetGo);
						break;
				}
				Component[] comps = m_TargetGo.GetComponents<Component>();
				foreach (var comp in comps) {
					bool isSelected = m_Targets.Contains(comp);
					bool newIsSelected = EditorGUILayout.ToggleLeft(GetDisplayCompName(comp), isSelected);
					switch (newIsSelected) {
						case true when !isSelected:
							m_Targets.Add(comp);
							break;
						case false when isSelected:
							m_Targets.Remove(comp);
							break;
					}
				}
			} else {
				const int DISPLAY_MAX = 8;
				int targetCount = m_Targets.Count;
				Type type = typeof(UObject);
				for (int i = 0, length = Mathf.Min(targetCount, DISPLAY_MAX); i < length; ++i) {
					EditorGUILayout.ObjectField(m_Targets[i], type, true);
				}
				if (targetCount > DISPLAY_MAX) {
					EditorGUILayout.LabelField("略...");
				}
			}
		}

		protected override void GetSearchRange() {
			PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
			// ReSharper disable once Unity.NoNullPropagation
			GameObject[] gos = (stage?.scene ?? SceneManager.GetActiveScene()).GetRootGameObjects();
			foreach (var go in gos) {
				m_SearchRangeTransList.Add(go.transform);
			}
		}
		
		private bool IsReferenced(UObject obj) {
			HashSet<object> set = new HashSet<object>();	// 用于避免循环引用导致死循环
			Queue<object> queue = new Queue<object>();	// 用于广度优先遍历
			if (obj != null) {
				queue.Enqueue(obj);
			}
			while (queue.Count > 0) {
				object current = queue.Dequeue();
				if (!set.Contains(current)) {
					set.Add(current);
					List<object> list = new List<object>();
					Type tempType = current.GetType();
					while (tempType != null) {
						FieldInfo[] fis = tempType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
						foreach (var fi in fis) {
							CollectValue(fi.GetValue(current), list);
						}
						tempType = tempType.BaseType;
					}
					foreach (var value in list) {
						Type valueType = value.GetType();
						if (!valueType.IsPrimitive) {	// 排除基本类型
							switch (value) {
								case string _:
								case Guid _:
								case DateTime _:
								case DateTimeOffset _:
									// 排除基本类型
									break;
								case GameObject go:
									if (m_Targets.Contains(go)) {
										return true;
									}
									break;
								case Component comp:
									if (m_TargetGo) {
										// 单选模式，精确查找
										if (m_Targets.Contains(comp)) {
											return true;
										}
									} else {
										// 多选模式，只要引用了目标的GameObject就算引用了
										if (m_Targets.Contains(comp.gameObject)) {
											return true;
										}
									}
									break;
								default:
									queue.Enqueue(value);
									break;
							}
						}
					}
				}
			}
			return false;
		}

		private static void CollectValue(object value, ICollection<object> list) {
			if (value != null) {
				switch (value) {
					case UObject uObj:
						if (uObj) {
							list.Add(value);
						}
						break;
					case IEnumerable ie:
						// 如果是可迭代对象，则拆散了放进列表
						foreach (var element in ie) {
							CollectValue(element, list);
						}
						break;
					default:
						list.Add(value);
						break;
				}
			}
		}
	}
}
