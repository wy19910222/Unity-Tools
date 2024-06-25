/*
 * @Author: wangyun
 * @CreateTime: 2022-05-02 01:13:30 495
 * @LastEditor: wangyun
 * @EditTime: 2022-05-04 01:51:33 841
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using UObject = UnityEngine.Object;

namespace TransformSearch {
	public class SearchComponent : BaseSearch {
		[MenuItem("Tools/TransformSearch/SearchComponent")]
		private static void Init() {
			SearchComponent window = GetWindow<SearchComponent>("ComponentSearch");
			window.minSize = new Vector2(200F, 200F);
			window.Show();
		}

		[SerializeField]
		private string m_ComponentName;
		private Type m_ComponentType;
	
		protected override void OnEnable() {
			base.OnEnable();
			m_ComponentName = EditorPrefs.GetString(GetType().FullName + ".ComponentName");
		}

		protected override List<UObject> Match(Transform trans) {
			List<UObject> comps = new List<UObject>();
			foreach (var comp in trans.GetComponents<Component>()) {
				if (comp) {
					Type type = comp.GetType();
					if (m_ComponentType != null) {
						if (m_ComponentType.IsAssignableFrom(type)) {
							comps.Add(comp);
						}
					} else if (type.Name == m_ComponentName) {
						comps.Add(comp);
					}
				} else {
					Debug.LogError(trans, trans.root.gameObject);
				}
			}
			return comps;
		}

		protected override void DrawHeader() {
			GUILayout.BeginHorizontal();
			DrawComponentName();
			if (Selection.activeObject is MonoScript script) {
				Type type = script.GetClass();
				if (type != null) {
					string typeName = type.Name;
					string fullName = type.FullName;
					if (m_ComponentName != typeName && m_ComponentName != fullName) {
						if (GUILayout.Button(typeName, GUILayout.ExpandWidth(false))) {
							m_ComponentName = fullName;
						}
					}
				}
			}
			if (GUILayout.Button("搜索", GUILayout.Width(60F))) {
				m_ComponentType = NameToType(m_ComponentName);
				Search();
			}
			GUILayout.EndHorizontal();
		}

		protected void DrawComponentName() {
			GUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = 70F;
			string newComponentName = EditorGUILayout.TextField("Comp Name", m_ComponentName);
			if (newComponentName != m_ComponentName) {
				Undo.RecordObject(this, "ComponentName");
				m_ComponentName = newComponentName;
				EditorPrefs.SetString(GetType().FullName + ".ComponentName", m_ComponentName);
			}
			GUILayout.EndHorizontal();
		}

		protected static Type NameToType(string typeName) {
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies) {
				Type type = assembly.GetType(typeName);
				if (type != null) {
					return type;
				}
			}
			return null;
		}
	}
}
