/*
 * @Author: wangyun
 * @CreateTime: 2022-06-06 13:07:54 746
 * @LastEditor: wangyun
 * @EditTime: 2023-02-14 13:38:17 570
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

using UObject = UnityEngine.Object;

namespace WYTools.BatchRename {
	public class BatchRename : EditorWindow {
		[MenuItem("Tools/Batch Rename")]
		private static void Init() {
			BatchRename window = GetWindow<BatchRename>();
			window.minSize = new Vector2(200F, 200F);
			window.Show();
		}

		public static readonly Color COLOR_NORMAL = Color.white;
#if UNITY_2021_1_OR_NEWER
		public static readonly Color COLOR_TOGGLE_CHECKED_EDITOR = new(1.7F, 0.6F, 0, 1);
#else
		public static readonly Color COLOR_TOGGLE_CHECKED_EDITOR = new Color(1, 0.5F, 0, 1);
#endif
		private static readonly GUILayoutOption BTN_WIDTH_OPTION = GUILayout.Width(80F);
		private static readonly GUILayoutOption ICON_WIDTH_OPTION = GUILayout.Width(16F);
		private static readonly GUILayoutOption DOUBLE_HEIGHT_OPTION = GUILayout.Height(EditorGUIUtility.singleLineHeight * 2);

		private enum BatchRenameMode {
			NORMAL,
			REPLACE
		}
		[SerializeField]
		private BatchRenameMode m_Mode;
		private BatchRenameMode Mode {
			get => m_Mode;
			set {
				if (value != m_Mode) {
					Undo.RecordObject(this, nameof(Mode));
					EditorPrefs.SetInt($"{nameof(BatchRename)}.{nameof(Mode)}", (int) value);
					m_Mode = value;
				}
			}
		}

		[SerializeField]
		private bool m_UseRegex;
		private bool UseRegex {
			get => m_UseRegex;
			set => SetValue(nameof(UseRegex), ref m_UseRegex, value);
		}

		[SerializeField]
		private string m_NamePattern;
		private string NamePattern {
			get => m_NamePattern;
			set => SetValue(nameof(NamePattern), ref m_NamePattern, value);
		}

		[SerializeField]
		private string m_ReplacePattern;
		private string ReplacePattern {
			get => m_ReplacePattern;
			set => SetValue(nameof(ReplacePattern), ref m_ReplacePattern, value);
		}
		[SerializeField]
		private string m_Replacement;
		private string Replacement {
			get => m_Replacement;
			set => SetValue(nameof(Replacement), ref m_Replacement, value);
		}

		[SerializeField]
		private int m_StartIndex;
		private int StartIndex {
			get => m_StartIndex;
			set => SetValue(nameof(StartIndex), ref m_StartIndex, value);
		}
		[SerializeField]
		private int m_IndexStepSize;
		private int IndexStepSize {
			get => m_IndexStepSize;
			set => SetValue(nameof(IndexStepSize), ref m_IndexStepSize, value);
		}
		[SerializeField]
		private int m_IndexDigits;
		private int IndexDigits {
			get => m_IndexDigits;
			set => SetValue(nameof(IndexDigits), ref m_IndexDigits, value);
		}

		public void OnEnable() {
			LoadAllCache();
			Undo.undoRedoPerformed += SaveAllCache;
		}
		public void OnDisable() {
			Undo.undoRedoPerformed -= SaveAllCache;
		}

		private void OnGUI() {
			EditorGUIUtility.labelWidth = 60F;

			EditorGUILayout.BeginHorizontal();
			{
				Color oldColor = GUI.backgroundColor;
				bool isNormalMode = m_Mode == BatchRenameMode.NORMAL;
				GUI.backgroundColor = isNormalMode ? COLOR_TOGGLE_CHECKED_EDITOR : COLOR_NORMAL;
				if (GUILayout.Toggle(isNormalMode, "整体", "ButtonLeft") && !isNormalMode) {
					Mode = BatchRenameMode.NORMAL;
				}

				bool isReplaceMode = m_Mode == BatchRenameMode.REPLACE;
				GUI.backgroundColor = isReplaceMode ? COLOR_TOGGLE_CHECKED_EDITOR : COLOR_NORMAL;
				if (GUILayout.Toggle(isReplaceMode, "替换", "ButtonRight") && !isReplaceMode) {
					Mode = BatchRenameMode.REPLACE;
				}
				GUI.backgroundColor = oldColor;
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(-3F);

			EditorGUILayout.BeginVertical();
			{
				GUILayout.Space(8F);

				EditorGUILayout.BeginHorizontal();
				{
					if (m_Mode == BatchRenameMode.REPLACE) {
						ReplacePattern = EditorGUILayout.TextField("把：", m_ReplacePattern);
					} else {
						NamePattern = EditorGUILayout.TextField("命名规则：", m_NamePattern);
					}

					Color oldColor = GUI.backgroundColor;
					GUI.backgroundColor = m_UseRegex ? COLOR_TOGGLE_CHECKED_EDITOR : COLOR_NORMAL;
					UseRegex = GUILayout.Toggle(m_UseRegex, "正则表达式", "Button", BTN_WIDTH_OPTION);
					GUI.backgroundColor = oldColor;
				}
				EditorGUILayout.EndHorizontal();

				if (m_Mode == BatchRenameMode.REPLACE) {
					Replacement = EditorGUILayout.TextField("替换成：", m_Replacement);
				} else {
					if (m_UseRegex) {
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField(new GUIContent(""), "CN EntryInfoIconSmall", ICON_WIDTH_OPTION);
							EditorGUILayout.LabelField("使用 /pattern/ 插入要与原始名称进行匹配的正则表达式\n如 /.*/_Clone 表示原始名称后面加上“_Clone”", DOUBLE_HEIGHT_OPTION);
						}
						EditorGUILayout.EndHorizontal();
					} else {
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField(new GUIContent(""), "CN EntryInfoIconSmall", ICON_WIDTH_OPTION);
							EditorGUILayout.LabelField("使用 * 插入原始名称");
						}
						EditorGUILayout.EndHorizontal();
					}
				}
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField(new GUIContent(""), "CN EntryInfoIconSmall", ICON_WIDTH_OPTION);
					EditorGUILayout.LabelField("使用 # 插入数字编号");
				}
				EditorGUILayout.EndHorizontal();

				StartIndex = EditorGUILayout.IntField("起始编号：", m_StartIndex);
				IndexStepSize = EditorGUILayout.IntField("递增步长：", m_IndexStepSize);
				IndexDigits = EditorGUILayout.IntField("编号位数：", m_IndexDigits);
			}
			EditorGUILayout.EndVertical();

			GUILayout.Space(5F);

			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("重命名")) {
					Rename();
				}
				if (m_Mode == BatchRenameMode.NORMAL) {
					if (GUILayout.Button("推测命名", BTN_WIDTH_OPTION)) {
						MaybeName();
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		[SerializeField]
		private int m_MaybeIndex = -1;
		private void MaybeName() {
			string[] rawNames = Array.ConvertAll(Selection.objects, obj => Regex.Replace(obj.name, "\\s*(\\d+|(\\(\\d+\\)))$", string.Empty));
			string[] names = new HashSet<string>(rawNames).ToArray();
			int goCount = names.Length;
			if (goCount > 0) {
				Undo.RecordObject(this, "MaybeIndex");
				m_MaybeIndex = (m_MaybeIndex + 1) % goCount;
				NamePattern = names[m_MaybeIndex] + "#";
			}
		}

		private void Rename() {
			switch (m_Mode) {
				case BatchRenameMode.NORMAL:
					if (m_UseRegex) {
						Regex regex = null;
						string pattern1 = m_NamePattern;
						string pattern2 = "";
						int slashIndex1 = pattern1.IndexOf("/");
						int slashIndex2 = pattern1.LastIndexOf("/");
						if (slashIndex1 != -1 && slashIndex2 != -1) {
							regex = new Regex(pattern1.Substring(slashIndex1 + 1, slashIndex2 - slashIndex1 - 1));
							pattern2 = pattern1.Substring(slashIndex2 + 1);
							pattern1 = pattern1.Substring(0, slashIndex1);
						}
						ForeachRename((oldName, index) => {
							string newName = pattern1.Replace("#", index.ToString("D" + m_IndexDigits));
							if (regex != null) {
								newName += regex.Match(oldName).Value;
								newName += pattern2.Replace("#", index.ToString("D" + m_IndexDigits));
							}
							return newName;
						});
					} else {
						ForeachRename((oldName, index) => 
								m_NamePattern.Replace("*", oldName).Replace("#", index.ToString("D" + m_IndexDigits))
						);
					}
					break;
				case BatchRenameMode.REPLACE:
					if (m_UseRegex) {
						Regex regex = new Regex(m_NamePattern);
						ForeachRename((oldName, index) => {
							string replacement = m_Replacement.Replace("#", index.ToString("D" + m_IndexDigits));
							return regex.Replace(oldName, replacement);
						});
					} else {
						ForeachRename((oldName, index) => {
							string replacement = m_Replacement.Replace("#", index.ToString("D" + m_IndexDigits));
							return oldName.Replace(m_ReplacePattern, replacement);
						});
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ForeachRename(Func<string, int, string> callback) {
			UObject[] objs = Selection.objects;
			int index = m_StartIndex;
			for (int i = 0, length = objs.Length; i < length; ++i) {
				UObject obj = objs[i];
				if (EditorUtility.IsPersistent(obj)) {
					string path = AssetDatabase.GetAssetPath(obj);
					int pointIndex = path.LastIndexOf('.');
					if (pointIndex == -1) {
						pointIndex = path.Length;
					}
					int slashIndex = path.LastIndexOf('/');
					string dirPath = path.Substring(0, slashIndex + 1);
					string extension = path.Substring(pointIndex, path.Length - pointIndex);
					string newName = callback(obj.name, index);
					AssetDatabase.MoveAsset(path, dirPath + newName + extension);
				} else {
					string newName = callback(obj.name, index);
					Undo.RecordObject(obj, "Name");
					obj.name = newName;
				}
				index += m_IndexStepSize;
			}
		}

		private void SetValue<T>(string recordKey, ref T variable, T value) where T : IEquatable<T> {
			if (!Equals(value, variable)) {
				Undo.RecordObject(this, recordKey);
				switch (value) {
					case bool bValue:
						EditorPrefs.SetBool($"{nameof(BatchRename)}.{recordKey}", bValue);
						break;
					case int iValue:
						EditorPrefs.SetInt($"{nameof(BatchRename)}.{recordKey}", iValue);
						break;
					case string sValue:
						EditorPrefs.SetString($"{nameof(BatchRename)}.{recordKey}", sValue);
						break;
				}
				variable = value;
			}
		}

		public void LoadAllCache() {
			m_Mode = (BatchRenameMode) EditorPrefs.GetInt("BatchRename.Mode", (int) BatchRenameMode.NORMAL);
			m_NamePattern = EditorPrefs.GetString("BatchRename.NamePattern");
			m_ReplacePattern = EditorPrefs.GetString("BatchRename.ReplacePattern");
			m_Replacement = EditorPrefs.GetString("BatchRename.Replacement");
			m_UseRegex = EditorPrefs.GetBool("BatchRename.UseRegex", false);
			m_StartIndex = EditorPrefs.GetInt("BatchRename.StartIndex", 0);
			m_IndexStepSize = EditorPrefs.GetInt("BatchRename.IndexStepSize", 1);
			m_IndexDigits = EditorPrefs.GetInt("BatchRename.IndexDigits", 1);
		}

		public void SaveAllCache() {
			EditorPrefs.SetInt("BatchRename.Mode", (int) m_Mode);
			EditorPrefs.SetString("BatchRename.NamePattern", m_NamePattern);
			EditorPrefs.SetString("BatchRename.ReplacePattern", m_ReplacePattern);
			EditorPrefs.SetString("BatchRename.Replacement", m_Replacement);
			EditorPrefs.SetBool("BatchRename.UseRegex", m_UseRegex);
			EditorPrefs.SetInt("BatchRename.StartIndex", m_StartIndex);
			EditorPrefs.SetInt("BatchRename.IndexStepSize", m_IndexStepSize);
			EditorPrefs.SetInt("BatchRename.IndexDigits", m_IndexDigits);
		}
	}
}