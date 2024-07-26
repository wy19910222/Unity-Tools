/*
 * @Author: wangyun
 * @CreateTime: 2022-05-02 01:13:30 495
 * @LastEditor: wangyun
 * @EditTime: 2022-05-04 01:51:33 841
 */

#if LUA_BEHAVIOUR_EXIST

using System.Collections.Generic;
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
		
			DefaultAsset asset = null;
			if (!string.IsNullOrEmpty(m_LuaPath)) {
				string luaFilePath = LUA_SRC_PATH + m_LuaPath.Replace(".", "/") + LUA_FILE_EXT;
				asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(luaFilePath);
				if (!asset) {
					// 如果找不到lua文件，则显示文本框
					string newLuaPath = EditorGUILayout.TextField(m_LuaPath);
					if (newLuaPath != m_LuaPath) {
						Undo.RecordObject(this, "LuaPath");
						m_LuaPath = newLuaPath;
						EditorPrefs.SetString(GetType().FullName + ".LuaPath", m_LuaPath);
					}
					
					GUILayout.EndHorizontal();
					return;
				}
			}
			DefaultAsset newAsset = EditorGUILayout.ObjectField(asset, typeof(DefaultAsset), true) as DefaultAsset;
			if (newAsset != asset) {
				// 如果根据lua文件记录lua路径
				if (!newAsset) {
					m_LuaPath = "";
				} else {
					string luaPath = AssetDatabase.GetAssetPath(newAsset);
					if (luaPath.StartsWith(LUA_SRC_PATH) && luaPath.EndsWith(LUA_FILE_EXT)) {
						luaPath = luaPath.Substring(LUA_SRC_PATH.Length, luaPath.Length - LUA_SRC_PATH.Length - LUA_FILE_EXT.Length);
						Undo.RecordObject(this, "LuaPath");
						m_LuaPath = luaPath.Replace("/", ".");
						EditorPrefs.SetString(GetType().FullName + ".LuaPath", m_LuaPath);
					}
				}
			}
		
			GUILayout.EndHorizontal();
		}
	}
}

#endif