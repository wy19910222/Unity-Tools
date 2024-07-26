/*
 * @Author: wangyun
 * @CreateTime: 2022-05-04 08:12:59 425
 * @LastEditor: wangyun
 * @EditTime: 2023-04-25 22:04:15 083
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using UObject = UnityEngine.Object;

namespace TransformSearch {
	public abstract class BaseSearch : EditorWindow {
		// [MenuItem("Tools/SearchTransform/BaseSearch")]
		// private static void Init() {
		// 	BaseSearch window = GetWindow<BaseSearch>();
		// 	window.minSize = new Vector2(200F, 200F);
		// 	window.Show();
		// }

		private static readonly Color COLOR_NORMAL = Color.white;
#if UNITY_2021_1_OR_NEWER
		private static readonly Color COLOR_TOGGLE_CHECKED_EDITOR = new(1.7F, 0.6F, 0, 1);
#else
		private static readonly Color COLOR_TOGGLE_CHECKED_EDITOR = new Color(1, 0.5F, 0, 1);
#endif
		private static readonly Color COLOR_LINE_ODD = new Color(0, 0, 0, 0.08F);
		private static readonly Color COLOR_LINE_SELECTED = new Color(0, 0.4F, 0.4F, 0.3F);
		private static readonly Color COLOR_LABEL_MATCHED = new Color(0, 0.5F, 0.35F);
		private static readonly Color COLOR_LABEL_MATCHED_PRO = new Color(0.4F, 0.7F, 0.6F);
		private static readonly GUILayoutOption OPTION_ARROW_WIDTH = GUILayout.Width(15F);
		private static readonly GUILayoutOption OPTION_LINE_HEIGHT = GUILayout.Height(EditorGUIUtility.singleLineHeight - 2F);
		
		protected readonly Dictionary<Transform, bool> m_TransIsFoldedDict = new Dictionary<Transform, bool>();
		protected readonly HashSet<UObject> m_ObjIsMatchSet = new HashSet<UObject>();
		protected readonly List<Transform> m_SearchRangeTransList = new List<Transform>();

		private Vector2 m_ScrollPos = Vector2.zero;
		private int m_LineNumber;
		private bool m_IsSearching;
		
		private GUIStyle m_MatchedLabelStyle;

		[SerializeField]
		private bool m_DisplayComp;

		protected virtual void OnEnable() {
			m_MatchedLabelStyle = new GUIStyle() {
				alignment = TextAnchor.MiddleLeft,
				normal = {
					textColor = EditorGUIUtility.isProSkin ? COLOR_LABEL_MATCHED_PRO : COLOR_LABEL_MATCHED
				}
			};
			Selection.selectionChanged += Repaint;
		}

		protected virtual void OnDisable() {
			Selection.selectionChanged -= Repaint;
		}

		protected abstract List<UObject> Match(Transform trans);

		protected void Search() {
			m_IsSearching = true;
			Repaint();
			EditorApplication.delayCall += () => {
				m_TransIsFoldedDict.Clear();
				m_ObjIsMatchSet.Clear();
				m_SearchRangeTransList.Clear();

				GetSearchRange();
				foreach (var trans in m_SearchRangeTransList) {
					CheckTrans(trans);
				}

				m_IsSearching = false;
				Repaint();
				Debug.Log("Search complete!");
			};
		}

		protected virtual void GetSearchRange() {
			foreach (var obj in Selection.objects) {
				switch (obj) {
					case GameObject go:
						m_SearchRangeTransList.Add(go.transform);
						break;
					case DefaultAsset defaultAsset: {
						string path = AssetDatabase.GetAssetPath(defaultAsset);
						if (Directory.Exists(path)) {
							string[] filePaths = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
							foreach (var filePath in filePaths) {
								Transform trans = AssetDatabase.LoadAssetAtPath<Transform>(filePath);
								m_SearchRangeTransList.Add(trans);
							}
						}
						break;
					}
				}
			}
			// 任意两者有父子关系，则只取父节点，同时也可以去重
			for (int index1 = m_SearchRangeTransList.Count - 1; index1 >= 1; --index1) {
				for (int index2 = index1 - 1; index2 >= 0; --index2) {
					Transform trans1 = m_SearchRangeTransList[index1];
					Transform trans2 = m_SearchRangeTransList[index2];
					if (trans1.IsChildOf(trans2)) {
						m_SearchRangeTransList.RemoveAt(index1);
						break;
					}
					if (trans2.IsChildOf(trans1)) {
						m_SearchRangeTransList.RemoveAt(index2);
						--index1;
					}
				}
			}
		}

		private bool CheckTrans(Transform trans) {
			bool visible = false;
			foreach (Transform child in trans) {
				visible = CheckTrans(child) || visible;
			}
			List<UObject> objs = Match(trans);
			if (objs != null && objs.Count > 0) {
				foreach (var obj in objs) {
					m_ObjIsMatchSet.Add(obj);
				}
				m_TransIsFoldedDict[trans] = false;
				return true;
			}
			if (visible) {
				m_TransIsFoldedDict[trans] = false;
				return true;
			}
			return false;
		}

		protected virtual void OnGUI() {
			DrawHeader();
			DrawOptionButtons();
			DrawList();
		}

		protected virtual void DrawHeader() {
			if (GUILayout.Button("搜索")) {
				Search();
			}
		}

		protected virtual void DrawOptionButtons() {
			Color prevColor = GUI.backgroundColor;
			GUI.backgroundColor = m_DisplayComp ? COLOR_TOGGLE_CHECKED_EDITOR : COLOR_NORMAL;
			bool newDisplayComp = GUILayout.Toggle(m_DisplayComp, "Display Component", "Button");
			if (newDisplayComp != m_DisplayComp) {
				Undo.RecordObject(this, "DisplayComponent");
				m_DisplayComp = newDisplayComp;
			}
			GUI.backgroundColor = prevColor;
		}

		protected virtual void DrawList() {
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
			if (m_IsSearching) {
				GUILayout.Label("搜索中……", "CenteredLabel");
			} else {
				m_LineNumber = 0;
				bool anyDisplayed = false;
				foreach (var trans in m_SearchRangeTransList) {
					if (trans && m_TransIsFoldedDict.ContainsKey(trans)) {
						anyDisplayed = true;
						DisplayTransform(trans);
					}
				}
				if (anyDisplayed) {
					DrawBottom();
				} else {
					GUILayout.Label("没有搜索到结果！", "CenteredLabel");
				}
			}
			EditorGUILayout.EndScrollView();
		}
		protected virtual void DrawBottom() {
			if (GUILayout.Button("全选")) {
				HashSet<UObject> objIsMatchSet = new HashSet<UObject>();
				foreach (var obj in m_ObjIsMatchSet) {
					if (obj is Component comp) {
						objIsMatchSet.Add(comp.gameObject);
					} else {
						objIsMatchSet.Add(obj);
					}
				}
				Selection.objects = objIsMatchSet.ToArray();
			}
		}

		private void DisplayTransform(Transform trans, int indent = 0) {
			List<Transform> children = new List<Transform>();
			foreach (Transform child in trans) {
				if (m_TransIsFoldedDict.ContainsKey(child)) {
					children.Add(child);
				}
			}
			bool isFolded = m_TransIsFoldedDict[trans];
			bool isSelected = false;
			foreach (var obj in Selection.objects) {
				if (obj is GameObject go && go.transform == trans) {
					isSelected = true;
					break;
				}
			}
			bool goIsMatched = m_ObjIsMatchSet.Contains(trans.gameObject);
			List<Component> matchedComps = new List<Component>();
			Component[] comps = trans.GetComponents<Component>();
			foreach (var comp in comps) {
				if (comp && m_ObjIsMatchSet.Contains(comp)) {
					matchedComps.Add(comp);
				}
			}

			Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width));
			{
				m_LineNumber++;
				// 绘制底色
				if (isSelected) {
					Rect bgRect = rect;
					bgRect.height += 1;
					EditorGUI.DrawRect(bgRect, COLOR_LINE_SELECTED);
				} else if ((m_LineNumber & 1) == 1) {
					EditorGUI.DrawRect(rect, COLOR_LINE_ODD);
				}
				// 缩进
				GUILayout.Space(2F + 16F * indent);
				// 箭头
				if (children.Count > 0) {
					string content = isFolded ? "\u25BA" : "\u25BC";
					// GUIContent content = EditorGUIUtility.IconContent(isFolded ? "d_scrollright" : "d_scrolldown");
					m_TransIsFoldedDict[trans] = GUILayout.Toggle(isFolded, content, "Label", OPTION_ARROW_WIDTH, OPTION_LINE_HEIGHT);
					GUILayout.Space(-3F);
				} else {
					GUILayout.Space(16F);
				}
				// 文本
				bool isMatched = goIsMatched || !m_DisplayComp && matchedComps.Count > 0;
				{
					GUIContent content = new GUIContent(GetDisplayTransName(trans), AssetPreview.GetMiniThumbnail(trans.gameObject));
					EditorGUILayout.LabelField(content, isMatched ? m_MatchedLabelStyle : "Label", OPTION_LINE_HEIGHT);
				}
				// 点击
				if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
					bool holdCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;
					bool holdCommand = (Event.current.modifiers & EventModifiers.Command) != 0;
					if (holdCtrl || holdCommand) {
						List<UObject> list = new List<UObject>(Selection.objects);
						if (isSelected) {
							list.Remove(trans.gameObject);
						} else {
							list.Add(trans.gameObject);
						}
						Selection.objects = list.ToArray();
					} else {
						Selection.objects = new UObject[] {trans.gameObject};
					}
				}
			}
			EditorGUILayout.EndHorizontal();
			
			if (m_DisplayComp) {
				foreach (var comp in matchedComps) {
					Rect _rect = EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width));
					{
						m_LineNumber++;
						bool _isSelected = Array.IndexOf(Selection.objects, comp) >= 0;
						// 绘制底色
						if (_isSelected) {
							Rect bgRect = _rect;
							bgRect.height += 1;
							EditorGUI.DrawRect(bgRect, COLOR_LINE_SELECTED);
						} else if ((m_LineNumber & 1) == 1) {
							EditorGUI.DrawRect(_rect, COLOR_LINE_ODD);
						}
						// 缩进+箭头缩进
						GUILayout.Space(2F + 16F * indent + 16F + 16F);
						// 文本
						{
							GUIContent content = new GUIContent(GetDisplayCompName(comp), AssetPreview.GetMiniThumbnail(comp));
							EditorGUILayout.LabelField(content, m_MatchedLabelStyle, OPTION_LINE_HEIGHT);
						}
						// 点击
						if (Event.current.type == EventType.MouseDown && _rect.Contains(Event.current.mousePosition)) {
							bool holdCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;
							bool holdCommand = (Event.current.modifiers & EventModifiers.Command) != 0;
							if (holdCtrl || holdCommand) {
								List<UObject> list = new List<UObject>(Selection.objects);
								if (_isSelected) {
									list.Remove(comp);
								} else {
									list.Add(comp);
								}
								Selection.objects = list.ToArray();
							} else {
								Selection.objects = new UObject[] {comp};
							}
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			
			if (!isFolded) {
				foreach (Transform child in children) {
					if (child) {
						DisplayTransform(child, indent + 1);
					}
				}
			}
		}

		protected virtual string GetDisplayTransName(Transform trans) {
			return trans.name;
		}

		protected virtual string GetDisplayCompName(Component comp) {
			string compName = "[" + comp.GetType().Name + "]";
			string customLabel = GetCustomLabel(comp);
			if (customLabel != null) {
				compName += " - " + customLabel;
			}
			return compName;
		}
	
		private const BindingFlags FLAG_INSTANCE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private const BindingFlags FLAG_INSTANCE_IGNORE_CASE = FLAG_INSTANCE | BindingFlags.IgnoreCase;
		private static readonly string[] CUSTOM_LABEL_KEYS = {"id", "title"};
		public static string GetCustomLabel(Component comp) {
			Type type = comp.GetType();
			foreach (var key in CUSTOM_LABEL_KEYS) {
				FieldInfo fi = type.GetField(key, FLAG_INSTANCE) ?? type.GetField(key, FLAG_INSTANCE_IGNORE_CASE);
				if (fi != null) {
					object value = fi.GetValue(comp);
					string valueStr = value?.ToString();
					if (!string.IsNullOrEmpty(valueStr)) {
						return valueStr;
					}
				}
				PropertyInfo pi = type.GetProperty(key, FLAG_INSTANCE) ?? type.GetProperty(key, FLAG_INSTANCE_IGNORE_CASE);
				if (pi != null) {
					object value = pi.GetValue(comp);
					string valueStr = value?.ToString();
					if (!string.IsNullOrEmpty(valueStr)) {
						return valueStr;
					}
				}
			}
			return null;
		}
	}
}
