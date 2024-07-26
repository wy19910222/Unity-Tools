/*
 * @Author: wangyun
 * @CreateTime: 2022-05-02 01:13:30 495
 * @LastEditor: wangyun
 * @EditTime: 2022-05-04 01:51:33 841
 */

#if LUA_BEHAVIOUR_EXIST

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using LuaApp;

using UObject = UnityEngine.Object;

namespace WYTools.TransformSearch {
	public class SearchLuaBehaviour : BaseSearch {
		[MenuItem("Tools/TransformSearch/SearchLuaBehaviour")]
		private static void Init() {
			SearchLuaBehaviour window = GetWindow<SearchLuaBehaviour>("LuaBehaviourSearch");
			window.minSize = new Vector2(200F, 200F);
			window.Show();
		}
	
		protected const string LUA_SRC_PATH = "Assets/Basic/Scripts/Lua/";
		protected const string LUA_FILE_EXT = ".lua";
		protected static readonly GUILayoutOption WIDTH_OPTION = GUILayout.Width(60F);

		[SerializeField]
		private string m_LuaPath;
	
		protected override void OnEnable() {
			base.OnEnable();
			m_LuaPath = EditorPrefs.GetString(GetType().FullName + ".LuaPath");
		}

		protected override List<UObject> Match(Transform trans) {
			List<UObject> comps = new List<UObject>();
			foreach (var luaBehaviour in trans.GetComponents<LuaBehaviourWithPath>()) {
				if (luaBehaviour.luaPath == m_LuaPath) {
					comps.Add(luaBehaviour);
				}
			}
			return comps;
		}

		protected override void DrawHeader() {
			GUILayout.BeginHorizontal();
			DrawLuaPath();
			if (GUILayout.Button("搜索", WIDTH_OPTION)) {
				Search();
			}
			GUILayout.EndHorizontal();
		}

		protected void DrawLuaPath() {
			GUILayout.BeginHorizontal();
		
			GUILayout.Label("Lua Script", WIDTH_OPTION);
		
			bool settingsExist = File.Exists("ProjectSettings/LuaBehaviourSettings.asset");
			bool luaPathIsEmpty = string.IsNullOrEmpty(m_LuaPath);
			string luaSrcPath = LuaBehaviourSettings.instance.luaSrcPath;
			string luaFileExtension = LuaBehaviourSettings.instance.luaFileExtension;
			UObject asset = !settingsExist || luaPathIsEmpty ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(luaSrcPath + m_LuaPath.Replace(".", "/") + luaFileExtension);;
			if (settingsExist && (luaPathIsEmpty || asset)) {
				// 已经设置过Lua根目录，且路径没错（路径为空或者能找到Lua文件），则支持文件拖放
				UObject newAsset = EditorGUILayout.ObjectField(asset, typeof(TextAsset), true);
				if (newAsset != asset) {
					// 根据lua文件记录lua路径
					if (!newAsset) {
						asset = null;
						Undo.RecordObject(this, "LuaPath");
						m_LuaPath = string.Empty;
					} else {
						string newLuaPath = AssetDatabase.GetAssetPath(newAsset);
						if (newLuaPath.StartsWith(luaSrcPath) && newLuaPath.EndsWith(luaFileExtension)) {
							int length = newLuaPath.Length - luaSrcPath.Length - luaFileExtension.Length;
							asset = newAsset;
							Undo.RecordObject(this, "LuaPath");
							m_LuaPath = newLuaPath.Substring(luaSrcPath.Length, length).Replace("/", ".");
						}
					}
				}
			} else {
				// 尚未设置Lua根目录或找不到lua文件，显示文本框和设置按钮
				string newLuaPath = EditorGUILayout.TextField(m_LuaPath);
				if (newLuaPath != m_LuaPath) {
					Undo.RecordObject(this, "LuaPath");
					m_LuaPath = newLuaPath;
				}
				// 设置按钮
				if (GUILayout.Button("LuaSrcPathSetting", GUILayout.Width(120F))) {
					SettingsService.OpenProjectSettings("Project/LuaBehaviour");
				}
			}
		
			GUILayout.EndHorizontal();
		}
	}
}

#endif