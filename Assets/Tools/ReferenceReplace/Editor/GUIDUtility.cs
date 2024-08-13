/*
 * @Author: wangyun
 * @CreateTime: 2024-08-13 12:29:53 900
 * @LastEditor: wangyun
 * @EditTime: 2024-08-13 12:29:53 905
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace WYTools.ReferenceReplace {
	public static class GUIDUtility {
		[MenuItem("Assets/复制(保持内部依赖) %#D", priority = 0)]
		public static void CopyAssets() {
			// 检查序列化模式
			if (EditorSettings.serializationMode != SerializationMode.ForceText) {
				if (!EditorUtility.DisplayDialog("警告", "当前序列化模式非「Force Text」，是否将Asset Serialization Mode设置成「Force Text」？", "确定", "取消")) {
					return;
				}
				EditorSettings.serializationMode = SerializationMode.ForceText;
			}
			// 获取所有选中Asset的路径
			UObject[] objs = Selection.objects;
			List<string> selectedPaths = new List<string>(objs.Length);
			foreach (UObject obj in objs) {
				selectedPaths.Add(AssetDatabase.GetAssetPath(obj));
			}
			// 去重，有父子关系的只保留父路径
			// 父路径总比子路径短，所以先排序
			selectedPaths.Sort((path1, path2) => path1.Length - path2.Length);
			for (int i = 0; i < selectedPaths.Count; i++) {
				for (int j = i + 1; j < selectedPaths.Count; j++) {
					if (selectedPaths[j].StartsWith(selectedPaths[i] + "/")) {
						selectedPaths.RemoveAt(j);
						j--;
					}
				}
			}
			// 收集所有要复制的文件，便于展示进度条
			List<string> srcPaths = new List<string>();
			List<string> dstPaths = new List<string>();
			foreach (string selectedPath in selectedPaths) {
				int selectedPathLength = selectedPath.Length;
				if (Directory.Exists(selectedPath)) {
					string outputDirPath = GetOutputDirPath(selectedPath);
					string[] filePaths = Directory.GetFiles(selectedPath, "*", SearchOption.AllDirectories);
					foreach (string filePath in filePaths) {
						if (!filePath.EndsWith(".meta")) {
							srcPaths.Add(filePath);
							dstPaths.Add(outputDirPath + filePath.Substring(selectedPathLength));
						}
					}
				} else if (File.Exists(selectedPath)) {
					srcPaths.Add(selectedPath);
					dstPaths.Add(GetOutputFilePath(selectedPath));
				}
			}
			// 收集所有GUID并new出要替换的GUID
			Dictionary<string, (string, string)> metaFileGUIDDict = new Dictionary<string, (string, string)>();
			foreach (string srcPath in srcPaths) {
				string metaFilePath = srcPath + ".meta";
				metaFileGUIDDict.Add(metaFilePath, (GetGUIDFromMetaFile(metaFilePath), Guid.NewGuid().ToString("N")));
			}
			// 开始复制，并在复制过程中替换GUID
			for (int i = 0, length = srcPaths.Count; i < length; ++i) {
				string srcPath = srcPaths[i];
				string dstPath = dstPaths[i];
				string displayText = $"从 {srcPath} 到 {dstPath} ";
				EditorUtility.DisplayProgressBar("正在复制", displayText, (float) i / length);
				Debug.Log("正在复制：" + displayText);
				// 复制meta文件
				string srcMetaFilePath = srcPath + ".meta";
				string dstMetaFilePath = dstPath + ".meta";
				if (metaFileGUIDDict.TryGetValue(srcMetaFilePath, out (string from, string to) guidMap)) {
					string metaText = ReadAllText(srcMetaFilePath);
					metaText = metaText.Replace("guid: " + guidMap.from, "guid: " + guidMap.to);
					WriteAllText(dstMetaFilePath, metaText);
				} else {
					Debug.LogError($"替换meta文件的GUID失败：{srcMetaFilePath}");
					File.Copy(srcMetaFilePath, dstMetaFilePath, true);
				}
				// 复制资源文件
				if (IsYamlFile(srcPath)) {
					string text = ReadAllText(srcPath);
					foreach ((string from, string to) in metaFileGUIDDict.Values) {
						if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to)) {
							text = text.Replace("guid: " + from, "guid: " + to);
						}
					}
					WriteAllText(dstPath, text);
				} else {
					File.Copy(srcPath, dstPath, true);
				}
			}
			EditorUtility.ClearProgressBar();
			Debug.Log("复制完成");
			AssetDatabase.Refresh();
		}

		// 直接在最后拼上"(Clone)"作为输出路径
		public static string GetOutputDirPath(string dirPath) {
			do {
				dirPath += "(Clone)";
			} while (Directory.Exists(dirPath));
			return dirPath;
		}

		// 在扩展名前面拼上"(Clone)"作为输出路径
		public static string GetOutputFilePath(string filePath) {
			int selectedPathLength = filePath.Length;
			int slashIndex = filePath.LastIndexOf('/');
			int dotIndex = filePath.LastIndexOf('.', selectedPathLength - 1, selectedPathLength - 1 - slashIndex);
			if (dotIndex == -1) {
				dotIndex = selectedPathLength;
			}
			string pathWithoutExt = filePath.Substring(0, dotIndex);
			string extension = filePath.Substring(dotIndex);
			do {
				filePath = pathWithoutExt + "(Clone)" + extension;
			} while (File.Exists(filePath));
			return filePath;
		}

		public static string GetGUIDFromMetaFile(string metaFilePath) {
			try {
				foreach (string line in File.ReadLines(metaFilePath)) {
					if (line.StartsWith("guid: ")) {
						return line.Substring("guid: ".Length);
					}
				}
			} catch (Exception e) {
				Debug.LogError(e);
			}
			return null;
		}
		
		// 检查是不是YAML语法的文本文件
		public static bool IsYamlFile(string filePath) {
			try {
				foreach (string line in File.ReadLines(filePath)) {
					if (line.StartsWith("%YAML")) {
						return true;
					}
					break;
				}
			} catch (Exception e) {
				Debug.LogError(e);
			}
			return false;
		}

		public static string ReadAllText(string filePath) {
			return File.ReadAllText(filePath);;
		}

		public static bool WriteAllText(string filePath, string text) {
			FileInfo file = new FileInfo(filePath);
			DirectoryInfo dir = file.Directory;
			if (dir != null) {
				if (!dir.Exists) {
					dir.Create();
				}
				File.WriteAllText(filePath, text);
				return true;
			}
			return false;
		}
	}
}
