/*
 * @Author: wangyun
 * @CreateTime: 2023-07-03 19:20:11 055
 * @LastEditor: wangyun
 * @EditTime: 2023-07-03 19:20:11 060
 */

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

using UObject = UnityEngine.Object;

namespace TransformSearch {
	public class SearchReferenceInSceneBySO : BaseSearch {
		[MenuItem("Tools/TransformSearch/SearchReferenceInSceneBySO")]
		private static void Init() {
			SearchReferenceInSceneBySO window = GetWindow<SearchReferenceInSceneBySO>("SOReferenceInSceneSearch");
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
										if (!filePath.EndsWith(".meta")) {
											if (filePath.EndsWith(".prefab")) {
												selections.Add(obj);
											} else {
												string _filePath = filePath.Replace('\\', '/');
												if (!_filePath.EndsWith(".meta") && !_filePath.Contains("/.") ) {
													foreach (UObject asset in AssetDatabase.LoadAllAssetsAtPath(_filePath)) {
														selections.Add(asset);
													}
												}
											}
										}
										
									}
								} else {
									string assetPath = AssetDatabase.GetAssetPath(obj);
									if (string.IsNullOrEmpty(assetPath)) {
										selections.Add(obj);
									} else {
										if (AssetDatabase.IsMainAsset(obj)) {
											foreach (UObject asset in AssetDatabase.LoadAllAssetsAtPath(assetPath)) {
												selections.Add(asset);
											}
										} else {
											selections.Add(obj);
										}
									}
								}
								break;
							}
							case SceneAsset sa: {
								selections.Add(sa);
								break;
							}
							case GameObject go: {
								selections.Add(go);
								break;
							}
							default: {
								string assetPath = AssetDatabase.GetAssetPath(obj);
								if (string.IsNullOrEmpty(assetPath)) {
									selections.Add(obj);
								} else {
									if (AssetDatabase.IsMainAsset(obj)) {
										foreach (UObject asset in AssetDatabase.LoadAllAssetsAtPath(assetPath)) {
											selections.Add(asset);
										}
									} else {
										selections.Add(obj);
									}
								}
								break;
							}
						}
					}
					m_Targets.AddRange(selections);
				}
			}
		}

		protected override List<UObject> Match(Transform trans) {
			List<UObject> matchedObjs = new List<UObject>();
			GameObject go = trans.gameObject;
			while ((go = PrefabUtility.GetCorrespondingObjectFromSource(go)) != null) {
				if (m_Targets.Contains(go)) {
					matchedObjs.Add(trans.gameObject);
					break;
				}
			}
			foreach (var comp in trans.GetComponents<Component>()) {
				if (IsReferenced(comp)) {
					matchedObjs.Add(comp);
				}
			}
			return matchedObjs;
		}

		protected override void DrawHeader() {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(m_TargetGo ? "搜索目标：" + m_TargetGo.name : "搜索目标：选中的" + m_Targets.Count + "个对象");
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
		
		private bool IsReferenced(Component comp) {
			SerializedObject serializedObject = new SerializedObject(comp);
			SerializedProperty property = serializedObject.GetIterator();
			if (m_TargetGo) {
				// 单选模式，精确查找
				while (property.Next(true)) {
					if (property.propertyType == SerializedPropertyType.ObjectReference && m_Targets.Contains(property.objectReferenceValue)) {
						return true;
					}
				}
			} else {
				// 多选模式，只要引用了目标的GameObject就算引用了
				while (property.Next(true)) {
					if (property.propertyType == SerializedPropertyType.ObjectReference) {
						UObject obj = property.objectReferenceValue;
						if (obj is Component component) {
							obj = component.gameObject;
						}
						if (obj && m_Targets.Contains(obj)) {
							return true;
						}
					}
				}
			}
			return false;
		}
	}
}
