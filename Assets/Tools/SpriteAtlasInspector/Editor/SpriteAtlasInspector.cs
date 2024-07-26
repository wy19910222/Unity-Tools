/*
 * @Author: wangyun
 * @CreateTime: 2023-06-21 20:21:52 397
 * @LastEditor: wangyun
 * @EditTime: 2023-06-21 20:21:52 402
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;

using UObject = UnityEngine.Object;

namespace WYTools.SpriteAtlasInspector {
	[CanEditMultipleObjects]
	[CustomEditor(typeof(SpriteAtlas))]
	public class SpriteAtlasInspector : Editor {
		private Editor m_InternalEditor;

		private SpriteAtlas Target => target as SpriteAtlas;

		private void OnEnable() {
			Type editorType = typeof(Editor).Assembly.GetType("UnityEditor.U2D.SpriteAtlasInspector");
			m_InternalEditor = CreateEditor(targets, editorType);
		}

		private void OnDisable() {
			DestroyImmediate(m_InternalEditor);
		}

		public override void OnInspectorGUI() {
			if (m_InternalEditor) {
				m_InternalEditor.OnInspectorGUI();
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

		public override bool HasPreviewGUI() => m_InternalEditor.HasPreviewGUI();
		public override GUIContent GetPreviewTitle() => m_InternalEditor.GetPreviewTitle();
		public override Texture2D RenderStaticPreview(string assetPath, UObject[] subAssets, int width, int height) =>
			m_InternalEditor.RenderStaticPreview(assetPath, subAssets, width, height);
		public override void OnPreviewGUI(Rect r, GUIStyle background) => m_InternalEditor.OnPreviewGUI(r, background);
		public override void OnInteractivePreviewGUI(Rect r, GUIStyle background) => m_InternalEditor.OnInteractivePreviewGUI(r, background);
		public override void OnPreviewSettings() => m_InternalEditor.OnPreviewSettings();
		public override string GetInfoString() => m_InternalEditor.GetInfoString();
		public override void DrawPreview(Rect previewArea) => m_InternalEditor.DrawPreview(previewArea);
		public override void ReloadPreviewInstances() => m_InternalEditor.ReloadPreviewInstances();
	}
}