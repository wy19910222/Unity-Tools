/*
 * @Author: wangyun
 * @CreateTime: 2023-07-18 17:01:58 016
 * @LastEditor: wangyun
 * @EditTime: 2023-07-18 17:01:58 022
 */

#if UNITY_2020_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.AssetImporters;

using UObject = UnityEngine.Object;

[CanEditMultipleObjects]
[CustomEditor(typeof(SpriteAtlasImporter))]
public class SpriteAtlasImporterInspector : Editor {
	private Editor m_InternalImporterEditor;
	private Editor m_InternalEditor;

	private SpriteAtlas m_Target;
	private SpriteAtlas Target => m_Target ? m_Target : m_Target = AssetDatabase.LoadAssetAtPath<SpriteAtlas>((target as SpriteAtlasImporter)?.assetPath);

	private void OnEnable() {
		Type importerEditorType = typeof(Editor).Assembly.GetType("UnityEditor.U2D.SpriteAtlasImporterInspector");
		m_InternalImporterEditor = CreateEditor(targets, importerEditorType);
		if (m_InternalImporterEditor is AssetImporterEditor) {
			Type editorType = typeof(Editor).Assembly.GetType("UnityEditor.U2D.SpriteAtlasInspector");
			UObject[] _targets = Array.ConvertAll(targets, t => (UObject) AssetDatabase.LoadAssetAtPath<SpriteAtlas>((t as SpriteAtlasImporter)?.assetPath));
			m_InternalEditor = CreateEditor(_targets, editorType);
			MethodInfo mi = typeof(AssetImporterEditor).GetMethod("InternalSetAssetImporterTargetEditor", BindingFlags.Instance | BindingFlags.NonPublic);
			mi?.Invoke(m_InternalImporterEditor, new object[] {m_InternalEditor});
		}
	}

	private void OnDisable() {
		DestroyImmediate(m_InternalImporterEditor);
		if (m_InternalEditor) {
			DestroyImmediate(m_InternalEditor);
		}
	}
	
	public override void OnInspectorGUI() {
		if (m_InternalImporterEditor) {
			m_InternalImporterEditor.OnInspectorGUI();
		} else {
			base.OnInspectorGUI();
		}
		if (targets.Length == 1) {
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("    去重    ")) {
				List<UObject> newPackables = new List<UObject>();
				HashSet<UObject> packableSet = new HashSet<UObject>();
				UObject[] packables = Target.GetPackables();
				foreach (var packable in packables) {
					if (!packableSet.Contains(packable)) {
						packableSet.Add(packable);
						newPackables.Add(packable);
					}
				}
				Undo.RecordObject(Target, "RemoveDuplication");
				Target.Remove(packables);
				Target.Add(newPackables.ToArray());
			}
			if (GUILayout.Button("深入文件夹去重")) {
				List<UObject> newPackables = new List<UObject>();
				HashSet<UObject> packableTempSet = new HashSet<UObject>();
				HashSet<UObject> packableExtSet = new HashSet<UObject>();
				UObject[] packables = Target.GetPackables();
				List<UObject> packableList = new List<UObject>(packables);
				packableList.Sort((packable1, packable2) => {
					bool isDir1 = packable1 is DefaultAsset da1 && Directory.Exists(AssetDatabase.GetAssetPath(da1));
					bool isDir2 = packable2 is DefaultAsset da2 && Directory.Exists(AssetDatabase.GetAssetPath(da2));
					if (isDir1) {
						if (isDir2) {
							// 子文件夹路径永远比父文件夹路径长
							return AssetDatabase.GetAssetPath((DefaultAsset) packable1).Length - AssetDatabase.GetAssetPath((DefaultAsset) packable2).Length;
						}
						return -1;
					}
					return isDir2 ? 1 : 0;
				});
				foreach (var packable in packableList) {
					if (!packableExtSet.Contains(packable)) {
						packableTempSet.Add(packable);
						packableExtSet.Add(packable);
						if (packable is DefaultAsset dAsset) {
							string dirPath = AssetDatabase.GetAssetPath(dAsset);
							string[] childDirs = Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories);
							foreach (string childDir in childDirs) {
								packableExtSet.Add(AssetDatabase.LoadAssetAtPath<DefaultAsset>(childDir));
							}
							string[] files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
							foreach (string filePath in files) {
								UObject asset = AssetDatabase.LoadAssetAtPath<UObject>(filePath);
								if (asset) {
									packableExtSet.Add(asset);
								}
							}
						}
					}
				}
				HashSet<UObject> packableSet = new HashSet<UObject>();
				foreach (var packable in packables) {
					if (packableTempSet.Contains(packable) && !packableSet.Contains(packable)) {
						packableSet.Add(packable);
						newPackables.Add(packable);
					}
				}
				Undo.RecordObject(Target, "RemoveDuplication");
				Target.Remove(packables);
				Target.Add(newPackables.ToArray());
			}
			EditorGUILayout.EndHorizontal();
		
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("清空列表")) {
				Undo.RecordObject(Target, "Clear");
				Target.Remove(Target.GetPackables());
			}
			if (GUILayout.Button("  添加选中对象  ")) {
				List<UObject> list = new List<UObject>();
				foreach (var obj in Selection.objects) {
					switch (obj) {
						case Sprite sprite:
							list.Add(sprite.texture);
							break;
						case Texture texture:
							if (AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(texture))) {
								list.Add(texture);
							}
							break;
						case DefaultAsset dAsset: {
							if (Directory.Exists(AssetDatabase.GetAssetPath(dAsset))) {
								list.Add(dAsset);
							}
							break;
						}
					}
				}
				Undo.RecordObject(Target, "AddSelections");
				Target.Add(list.ToArray());
			}
			EditorGUILayout.EndHorizontal();
		}
	}
	
	public override bool HasPreviewGUI() => m_InternalImporterEditor.HasPreviewGUI();
	public override GUIContent GetPreviewTitle() => m_InternalImporterEditor.GetPreviewTitle();
	public override Texture2D RenderStaticPreview(string assetPath, UObject[] subAssets, int width, int height) =>
		m_InternalImporterEditor.RenderStaticPreview(assetPath, subAssets, width, height);
	public override void OnPreviewGUI(Rect r, GUIStyle background) => m_InternalImporterEditor.OnPreviewGUI(r, background);
	public override void OnInteractivePreviewGUI(Rect r, GUIStyle background) => m_InternalImporterEditor.OnInteractivePreviewGUI(r, background);
	public override void OnPreviewSettings() => m_InternalImporterEditor.OnPreviewSettings();

	public override string GetInfoString() => m_InternalImporterEditor.GetInfoString();
	public override void DrawPreview(Rect previewArea) => m_InternalImporterEditor.DrawPreview(previewArea);
	public override void ReloadPreviewInstances() => m_InternalImporterEditor.ReloadPreviewInstances();
}

#endif