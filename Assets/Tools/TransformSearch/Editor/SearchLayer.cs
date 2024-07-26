/*
 * @Author: wangyun
 * @CreateTime: 2022-07-26 18:47:11 750
 * @LastEditor: wangyun
 * @EditTime: 2022-07-26 18:47:11 754
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using UObject = UnityEngine.Object;

namespace WYTools.TransformSearch {
	public class SearchLayer : BaseSearch {
		[MenuItem("Tools/TransformSearch/SearchLayer")]
		private static void Init() {
			SearchLayer window = GetWindow<SearchLayer>("LayerSearch");
			window.minSize = new Vector2(200F, 200F);
			window.Show();
		}

		[SerializeField]
		private uint m_LayerMask;
	
		protected override void OnEnable() {
			base.OnEnable();
			m_LayerMask = (uint) EditorPrefs.GetInt(GetType().FullName + ".LayerMask");
		}

		protected override List<UObject> Match(Transform trans) {
			List<UObject> comps = new List<UObject>();
			if (((1 << trans.gameObject.layer) & m_LayerMask) > 0) {
				comps.Add(trans);
			}
			return comps;
		}

		protected override void DrawHeader() {
			GUILayout.BeginHorizontal();
			DrawLayer();
			if (GUILayout.Button("搜索", GUILayout.Width(60F))) {
				Search();
			}
			GUILayout.EndHorizontal();
		}

		protected void DrawLayer() {
			string[] displayedOptions = new string[31];
			for (int i = 0, length = displayedOptions.Length; i < length; i++) {
				displayedOptions[i] = i + ":" + LayerMask.LayerToName(i);
			}

			EditorGUIUtility.labelWidth = 40F;
			// m_LayerMask = EditorGUILayout.LayerMaskField(m_LayerMask, "Layer");
			MethodInfo mi = typeof(EditorGUILayout).GetMethod("LayerMaskField", BindingFlags.Static | BindingFlags.NonPublic,
				null, new []{ typeof(uint), typeof(GUIContent), typeof(GUILayoutOption[]) }, null);
			uint newLayerMask = (uint) (mi?.Invoke(null, new object[] { m_LayerMask, new GUIContent("Layer"), Array.Empty<GUILayoutOption>() }) ?? 0);
			if (newLayerMask != m_LayerMask) {
				Undo.RecordObject(this, "LayerMask");
				m_LayerMask = newLayerMask;
				EditorPrefs.SetInt(GetType().FullName + ".LayerMask", (int) m_LayerMask);
				Debug.Log(m_LayerMask);
			}
		}
	}
}
