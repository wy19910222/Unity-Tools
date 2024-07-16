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

public class ReferenceReplace : EditorWindow {
	[MenuItem("Tools/Reference Replace")]
	private static void Init() {
		ReferenceReplace window = GetWindow<ReferenceReplace>();
		window.minSize = new Vector2(200F, 200F);
		window.Show();
	}
	
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
	
	private void OnEnable() {
		LoadMaps();
		
		m_List = new ReorderableList(m_ReplaceMaps, typeof(ReplaceMap), true, true, true, true) {
			drawHeaderCallback = rect => {
				const float THUMB_WIDTH = 16;
				const float BUTTON_WIDTH = 30;
				float LABEL_WIDTH = (rect.width - THUMB_WIDTH - BUTTON_WIDTH + 6) * 0.5F;
		
				Rect leftRect = new Rect(rect.x + THUMB_WIDTH, rect.y, LABEL_WIDTH, rect.height);
				EditorGUI.LabelField(leftRect, "原对象");
		
				Rect rightRect = new Rect(rect.x + THUMB_WIDTH + LABEL_WIDTH, rect.y, LABEL_WIDTH, rect.height);
				EditorGUI.LabelField(rightRect, "替换为");
		
				Rect tailRect = new Rect(rect.x + rect.width - BUTTON_WIDTH + 6, rect.y - 1, BUTTON_WIDTH, rect.height + 2);
				if (GUI.Button(tailRect, "+")) {
					Undo.RecordObject(this, "Maps.Add");
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
					map.from = newFrom;
					map.to = newTo;
					m_ReplaceMaps[index] = map;
				}
		
				Rect tailRect = new Rect(rect.x + rect.width - BUTTON_WIDTH + 8, rect.y + 1, BUTTON_WIDTH - 2, rect.height - 2);
				if (GUI.Button(tailRect, "×")) {
					Undo.RecordObject(this, "Maps.Remove");
					EditorApplication.delayCall += () => {
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
		Undo.RecordObject(this, "Maps");
		m_List.DoLayoutList();
		EditorGUILayout.EndScrollView();
		GUILayout.Space(-2F);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("清空列表")) {
			Undo.RecordObject(this, "Maps.Clear");
			m_ReplaceMaps.Clear();
		}
		if (GUILayout.Button("选中对象覆盖到左边")) {
			Undo.RecordObject(this, "Maps.InsertSelectionsToLeft");
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
		if (GUILayout.Button("选中对象覆盖到右边")) {
			Undo.RecordObject(this, "Maps.InsertSelectionsToRight");
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
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(10F);

		EditorGUIUtility.labelWidth = 70F;
		UObject newTarget = EditorGUILayout.ObjectField("替换目标", m_Target, typeof(UObject), true);
		if (newTarget != m_Target) {
			if (newTarget && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(newTarget))) {
				EditorUtility.DisplayDialog("错误", "只支持拖入文本型资产文件，如Scene、Prefab、Material等。", "确定");
			} else {
				Undo.RecordObject(this, "Target");
				m_Target = newTarget;
			}
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
			if (EditorUtility.DisplayDialog("警告", "当前序列化模式非「Force Text」，是否将Asset Serialization Mode设置成「Force Text」？", "确定", "取消")) {
				// SettingsService.OpenProjectSettings("Project/Editor");
				// // GetWindow<ProjectSettingsWindow>().m_SearchText = "Mode";
				// Type editorType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectSettingsWindow");
				// EditorWindow projectSettingsWindow = GetWindow(editorType);
				// FieldInfo searchTextFI = editorType?.BaseType?.GetField("m_SearchText", BindingFlags.Instance | BindingFlags.NonPublic);
				// searchTextFI?.SetValue(projectSettingsWindow, "Mode");
				EditorSettings.serializationMode = SerializationMode.ForceText;
			}
			return;
		}
		
		string targetPath = AssetDatabase.GetAssetPath(m_Target);
		if (!string.IsNullOrEmpty(targetPath)) {
			int count = 0;
			if (Directory.Exists(targetPath)) {
				// 如果是文件夹，则遍历操作文件夹内所有文件
				string outputDir = targetPath;
				if (clone) {
					string dirPath = targetPath + "_Clone";
					outputDir = dirPath;
					for (int i = 1; Directory.Exists(outputDir) || File.Exists(outputDir); i++) {
						outputDir = dirPath + "_" + i;
					}
				}
				string[] filePaths = Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories);
				foreach (string filePath in filePaths) {
					string outputPath = filePath;
					if (clone) {
						outputPath = outputDir + outputPath.Substring(targetPath.Length);
					}
					if (ReplaceInFile(filePath, outputPath)) {
						count++;
					}
				}
			} else if (File.Exists(targetPath)) {
				// 如果是文件，则操作该文件
				string outputPath = targetPath;
				if (clone) {
					int pointIndex = outputPath.LastIndexOf('.');
					if (pointIndex == -1) {
						pointIndex = outputPath.Length;
					}
					string filePathNoExt = outputPath.Substring(0, pointIndex) + "_Clone";
					string fileExt = outputPath.Substring(pointIndex);
					outputPath = filePathNoExt + fileExt;
					for (int i = 1; Directory.Exists(outputPath) || File.Exists(outputPath); i++) {
						outputPath = filePathNoExt + "_" + i + fileExt;
					}
				}
				if (ReplaceInFile(targetPath, outputPath)) {
					count++;
				}
			}
			// 刷新
			AssetDatabase.Refresh();
			string text = $"替换完成，{count}个资源被{(clone ? "复制" : "改动")}。";
			ShowNotification(EditorGUIUtility.TrTextContent(text), 1);
			Debug.Log(text);
		}
	}

	private bool ReplaceInFile(string srcFilePath, string dstFilePath) {
		// 检查是不是YAML语法的文本文件
		foreach (string line in File.ReadLines(srcFilePath)) {
			if (!line.StartsWith("%YAML")) {
				return false;
			}
			break;
		}
		// 读取
		string text = File.ReadAllText(srcFilePath);
		// 替换
		bool done = false;
		foreach (var map in m_ReplaceMaps) {
			if (map.from && map.to) {
				string fromPath = AssetDatabase.GetAssetPath(map.from);
				string fromGUID = AssetDatabase.AssetPathToGUID(fromPath);
				string toPath = AssetDatabase.GetAssetPath(map.to);
				string toGUID = AssetDatabase.AssetPathToGUID(toPath);
				if (!string.IsNullOrEmpty(fromGUID) && !string.IsNullOrEmpty(toGUID)) {
					done = done || text.Contains(fromGUID);
					text = text.Replace(fromGUID, toGUID);
				}
			}
		}
		// 写入
		if (done) {
			FileInfo file = new FileInfo(dstFilePath);
			DirectoryInfo dir = file.Directory;
			if (dir != null) {
				if (!dir.Exists) {
					dir.Create();
				}
				File.WriteAllText(dstFilePath, text);
				return true;
			}
		}
		return false;
	}
}
