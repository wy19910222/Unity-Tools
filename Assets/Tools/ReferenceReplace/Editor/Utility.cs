/*
 * @Author: wangyun
 * @CreateTime: 2024-08-13 12:29:53 900
 * @LastEditor: wangyun
 * @EditTime: 2024-08-13 12:29:53 905
 */

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace WYTools.ReferenceReplace {
	public static class Utility {
		private static PropertyInfo s_InspectorModePI = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
		public static long GetFileID(UObject obj) {
			using (SerializedObject sObj = new SerializedObject(obj)) {
				s_InspectorModePI?.SetValue(sObj, InspectorMode.Debug, null);
				SerializedProperty localIdProp = sObj.FindProperty("m_LocalIdentfierInFile");
				return localIdProp.longValue;
			}
		}
		
		public static string GetGUID(UObject obj) {
			return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
		}
		
		// 直接在最后拼上"(Clone)"作为输出路径
		public static string GetOutputDirPath(string dirPath) {
			do {
				dirPath += "(Clone)";
			} while (Directory.Exists(dirPath));
			return dirPath;
		}

		// 在扩展名前面拼上"(Clone)"作为输出路径
		// 因为调用放传入的都是AssetDatabase获取的路径，所以只考虑目录分隔符为"/"的情况
		public static string GetOutputFilePath(string filePath) {
			int pathLength = filePath.Length;
			int slashIndex = filePath.LastIndexOf('/');
			int dotIndex = filePath.LastIndexOf('.', pathLength - 1, pathLength - 1 - slashIndex);
			if (dotIndex == -1) {
				dotIndex = pathLength;
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
			return File.ReadAllText(filePath);
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
