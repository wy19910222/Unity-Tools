/*
 * @Author: wangyun
 * @CreateTime: 2024-08-13 23:51:32 924
 * @LastEditor: wangyun
 * @EditTime: 2024-08-13 23:51:32 930
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace WYTools.ReferenceReplace {
	public static class AssetsDeepCloner {
		[MenuItem("Assets/克隆(复制内部依赖) %#D", priority = 0)]
		public static void Clone() {
			Clone(Selection.objects);
		}
		
		public static void Clone(UObject[] objs) {
			// 检查序列化模式
			if (EditorSettings.serializationMode != SerializationMode.ForceText) {
				if (!EditorUtility.DisplayDialog("警告", "当前序列化模式非「Force Text」，是否将Asset Serialization Mode设置成「Force Text」并继续？", "确定", "取消")) {
					return;
				}
				EditorSettings.serializationMode = SerializationMode.ForceText;
			}
			// 获取所有选中Asset的路径
			List<string> paths = new List<string>(objs.Length);
			foreach (UObject obj in objs) {
				string path = AssetDatabase.GetAssetPath(obj);
				if (!string.IsNullOrEmpty(path)) {
					paths.Add(path);
				}
			}
			// 去重，有父子关系的只保留父路径
			// 父路径总比子路径短，所以先排序
			paths.Sort((path1, path2) => path1.Length - path2.Length);
			for (int i = 0; i < paths.Count; i++) {
				for (int j = i + 1; j < paths.Count; j++) {
					if (paths[j].StartsWith(paths[i] + "/")) {
						paths.RemoveAt(j);
						j--;
					}
				}
			}
			// 收集所有要复制的文件，便于展示进度条
			List<string> srcPaths = new List<string>();
			List<string> dstPaths = new List<string>();
			foreach (string path in paths) {
				int pathLength = path.Length;
				if (Directory.Exists(path)) {
					string outputDirPath = Utility.GetOutputDirPath(path);
					string[] filePaths = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
					foreach (string filePath in filePaths) {
						if (!filePath.EndsWith(".meta")) {
							srcPaths.Add(filePath);
							dstPaths.Add(outputDirPath + filePath.Substring(pathLength));
						}
					}
				} else if (File.Exists(path)) {
					srcPaths.Add(path);
					dstPaths.Add(Utility.GetOutputFilePath(path));
				}
			}
			// 收集所有GUID并new出要替换的GUID
			Dictionary<string, (string, string)> metaFileGUIDDict = new Dictionary<string, (string, string)>();
			foreach (string srcPath in srcPaths) {
				string metaFilePath = srcPath + ".meta";
				metaFileGUIDDict.Add(metaFilePath, (Utility.GetGUIDFromMetaFile(metaFilePath), Guid.NewGuid().ToString("N")));
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
				CopyAndReplace(srcMetaFilePath, dstMetaFilePath, metaFileGUIDDict.Values);
				// 复制资源文件
				if (Utility.IsYamlFile(srcPath)) {
					CopyAndReplace(srcPath, dstPath, metaFileGUIDDict.Values);
				} else if (srcPath.EndsWith(".asmdef") || srcPath.EndsWith(".asmref")) {
					CopyAndReplace(srcPath, dstPath, metaFileGUIDDict.Values, "GUID:");
				} else {
					File.Copy(srcPath, dstPath, true);
				}
			}
			EditorUtility.ClearProgressBar();
			Debug.Log("复制完成");
			AssetDatabase.Refresh();
		}

		private static bool CopyAndReplace(string srcFilePath, string dstFilePath, IEnumerable<(string, string)> guidMaps, string prefix = "guid: ") {
			string text = Utility.ReadAllText(srcFilePath);
			foreach ((string from, string to) in guidMaps) {
				if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to)) {
					text = text.Replace(prefix + from, prefix + to);
				}
			}
			return Utility.WriteAllText(dstFilePath, text);
		}
	}
}
