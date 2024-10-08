﻿/*
 * @Author: wangyun
 * @CreateTime: 2024-06-29 22:21:22 141
 * @LastEditor: wangyun
 * @EditTime: 2024-06-29 22:21:22 145
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace WYTools.AudioFactory {
	public class AudioVolumeUnify : EditorWindow {
		[MenuItem("Tools/WYTools/Audio Volume Unify")]
		public static void ShowWindow() {
			AudioVolumeUnify window = GetWindow<AudioVolumeUnify>();
			window.minSize = new Vector2(360F, 200F);
			window.Show();
		}

		private const float List_THUMB_WIDTH = 14;
		private const float List_ADD_BUTTON_WIDTH = 30;

		private const float List_VOLUME_WIDTH = 70;
		private const float List_VOLUME_SCALE_WIDTH = 80;
		private const float List_SET_BASE_BUTTON_WIDTH = 40;
		private const float List_PLAY_BUTTON_WIDTH = 40;

		private static readonly int[] BITS_PER_SAMPLES = { 8, 16, 24, 32 };
		private static readonly int[] MP3_QUALITIES = { 64, 96, 128, 160, 256 };
		private static readonly Color TIME_SCALE_COLOR = new Color(0.85F, 0.45F, 0.1F);

		[Serializable]
		public class AudioInfo {
			[SerializeField] public AudioClip clip;
			[SerializeField] public float volume;
		}

		[SerializeField] private float m_UnifiedVolume = 1;
		[SerializeField] private List<AudioInfo> m_InfoList = new List<AudioInfo>();
		[SerializeField] private int m_BitsPerSample = 16;
		[SerializeField] private int m_Mp3Quality = 128;
		[SerializeField] private float m_OggQuality = 0.4F;

		[SerializeField] private AudioSource m_AudioSource;

		private GUIStyle m_TimeScaleLabelStyle;
		private ReorderableList m_List;
		private Vector2 m_ScrollPos = Vector2.zero;

		private readonly Dictionary<AudioClip, AudioInfo> m_TempDict = new Dictionary<AudioClip, AudioInfo>();

		private void Awake() {
			m_AudioSource = EditorUtility.CreateGameObjectWithHideFlags("[AudioVolumeUnify]", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
		}

		private void OnEnable() {
			if (m_TimeScaleLabelStyle == null) {
				m_TimeScaleLabelStyle = new GUIStyle() {
					alignment = TextAnchor.MiddleLeft,
					normal = {
						textColor = TIME_SCALE_COLOR
					}
				};
			}
			if (m_List == null) {
				m_List = new ReorderableList(m_InfoList, typeof(AudioInfo), true, true, false, false) {
					drawHeaderCallback = DrawListHeader,
					drawElementCallback = DrawListElement,
					elementHeight = 20, footerHeight = 0
				};
			}
			Undo.undoRedoPerformed += Repaint;
		}

		private void OnDisable() {
			Undo.undoRedoPerformed -= Repaint;
		}

		// 试听结束后需要刷新按钮
		private bool m_WillRepaint;
		private void Update() {
			if (m_AudioSource.isPlaying) {
				m_WillRepaint = true;
			} else if (m_WillRepaint) {
				Repaint();
				m_WillRepaint = false;
			}
		}

		private void ShowButton(Rect rect) {
			if (GUI.Button(rect, EditorGUIUtility.IconContent("_Help"), "IconButton")) {
				PopupWindow.Show(rect, new PopupContent(300, 114, popupRect => {
					popupRect.x += 6;
					popupRect.y += 2;
					popupRect.width -= 12;
					popupRect.height -= 4;
					EditorGUI.LabelField(
							popupRect,
							new GUIContent(
									"本工具用于将多个音频统一音量，支持WAV、MP3、OGG三种格式。\n" +
									"点击「基准」按钮，将该音频设置为统一音量。\n" +
									"点击「试听」按钮，以统一音量试听该音频。\n" +
									"输出音频文件格式与原音频文件格式相同。\n" +
									"Mp3格式由于编码原因，每次写入音量会发生细微变化，并且首尾会多出零点零几秒的空白。"),
							"WordWrappedLabel"
					);
				}));
			}
		}

		private void OnGUI() {
			DrawList();

			GUILayout.Space(5);

			DrawUnifiedVolume();

			GUILayout.Space(5);

			DrawSaveField();
		}

		private void DrawList() {
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.ExpandHeight(false));
			m_List.DoLayoutList();
			EditorGUILayout.EndScrollView();

			GUILayout.Space(-2F);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("清空列表")) {
				Undo.RecordObject(this, "AudioVolumeUnify.ListClear");
				m_InfoList.Clear();
			}

			if (GUILayout.Button("选中对象加入列表")) {
				Undo.RecordObject(this, "AudioVolumeUnify.ListAddRange");
				List<AudioClip> newAudioList = new List<AudioClip>();
				foreach (Object obj in Selection.objects) {
					if (obj is AudioClip clip) {
						newAudioList.Add(clip);
					}
				}
				if (newAudioList.Count > 0) {
					foreach (AudioInfo info in m_InfoList) {
						if (info.clip) {
							m_TempDict[info.clip] = info;
						}
					}
					int countNew = 0;
					foreach (AudioClip clip in newAudioList) {
						if (m_TempDict.ContainsKey(clip)) {
							m_TempDict[clip].volume = GetMaxVolume(clip);
						} else {
							m_TempDict[clip] = new AudioInfo() {clip = clip, volume = GetMaxVolume(clip)};
							countNew++;
						}
					}
					m_InfoList.Clear();
					foreach (KeyValuePair<AudioClip, AudioInfo> pair in m_TempDict) {
						m_InfoList.Add(pair.Value);
					}
					m_TempDict.Clear();
					ShowNotification(EditorGUIUtility.TrTextContent($"新加入{countNew}个对象。"), 1);
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawListHeader(Rect rect) {
			// Header比Element左右各宽1像素，在这里对齐一下
			rect.x += 1;
			rect.width -= 2;

			float thumbWidth = m_List.draggable ? List_THUMB_WIDTH : 0;
			float headerWidth = rect.width + 7;	// 右边空白处也用起来
			// 左端拖拽区域宽度 + 音频对象宽度 + 音量和音量缩放的宽度 + “基准”“试听”两个按钮宽度 + 右端添加按钮宽度
			float audioWidth = headerWidth - thumbWidth - List_ADD_BUTTON_WIDTH
					- List_PLAY_BUTTON_WIDTH - List_SET_BASE_BUTTON_WIDTH - List_VOLUME_WIDTH - List_VOLUME_SCALE_WIDTH;

			Rect audioRect = new Rect(rect.x + thumbWidth, rect.y, audioWidth, rect.height);
			EditorGUI.LabelField(audioRect, "音频");

			Rect volumeRect = new Rect(rect.x + thumbWidth + audioWidth, rect.y, List_VOLUME_WIDTH, rect.height);
			EditorGUI.LabelField(volumeRect, "音量");

			Rect volumeScaleRect = new Rect(rect.x + thumbWidth + audioWidth + List_VOLUME_WIDTH, rect.y, List_VOLUME_SCALE_WIDTH, rect.height);
			EditorGUI.LabelField(volumeScaleRect, "音量缩放");

			Rect tailRect = new Rect(rect.x + headerWidth - List_ADD_BUTTON_WIDTH, rect.y - 1, List_ADD_BUTTON_WIDTH, rect.height + 2);
			if (GUI.Button(tailRect, "+")) {
				Undo.RecordObject(this, "AudioVolumeUnify.ListAdd");
				m_InfoList.Add(new AudioInfo());
			}
		}

		private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused) {
			// Element上下各空1像素
			rect.y += 1;
			rect.height -= 2;

			float elementWidth = rect.width + 7;	// 右边空白处也用起来
			// 左端音频对象宽度 + 音量和音量缩放的宽度 + “基准”“试听”两个按钮宽度 + 右端添加按钮宽度（删除按钮与添加按钮居中对齐但窄2像素，所以这里按添加按钮算宽度）
			float audioWidth = elementWidth - List_ADD_BUTTON_WIDTH
					- List_PLAY_BUTTON_WIDTH - List_SET_BASE_BUTTON_WIDTH - List_VOLUME_WIDTH - List_VOLUME_SCALE_WIDTH;

			AudioInfo info = m_InfoList[index];
			float x = rect.x;
			Rect audioRect = new Rect(x, rect.y, audioWidth - 2, rect.height);
			AudioClip newClip = EditorGUI.ObjectField(audioRect, info.clip, typeof(AudioClip), true) as AudioClip;
			if (newClip != info.clip) {
				Undo.RecordObject(this, "AudioVolumeUnify.ListModify");
				info.clip = newClip;
				info.volume = GetMaxVolume(newClip);
			}

			x += audioWidth;
			Rect volumeRect = new Rect(x, rect.y, List_VOLUME_WIDTH, rect.height);
			EditorGUI.LabelField(volumeRect, info.volume + "");

			x += List_VOLUME_WIDTH;
			Rect volumeScaleRect = new Rect(x, rect.y, List_VOLUME_SCALE_WIDTH, rect.height);
			if (info.volume != 0) {
				EditorGUI.LabelField(volumeScaleRect, info.volume == 0 ? "" : "× " + m_UnifiedVolume / info.volume, m_TimeScaleLabelStyle);
			}

			x += List_VOLUME_SCALE_WIDTH;
			Rect setBaseRect = new Rect(x, rect.y, List_SET_BASE_BUTTON_WIDTH, rect.height);
			bool isBase = Mathf.Approximately(info.volume, m_UnifiedVolume);
			bool newIsBase = GUI.Toggle(setBaseRect, isBase, "基准", "Button");
			if (newIsBase && !isBase) {
				Undo.RecordObject(this, "AudioVolumeUnify.UnifiedVolume");
				m_UnifiedVolume = info.volume;
			}

			x += List_SET_BASE_BUTTON_WIDTH;
			Rect playRect = new Rect(x, rect.y, List_PLAY_BUTTON_WIDTH, rect.height);
			if (m_AudioSource.isPlaying && m_AudioSource.clip == info.clip) {
				if (GUI.Button(playRect, "停止")) {
					StopAudio();
				}
			} else {
				if (GUI.Button(playRect, "试听")) {
					PlayAudio(info.clip, info.volume == 0 ? 1 : m_UnifiedVolume / info.volume);
				}
			}

			Rect tailRect = new Rect(rect.x + elementWidth - List_ADD_BUTTON_WIDTH + 1, rect.y, List_ADD_BUTTON_WIDTH - 2, rect.height);
			if (GUI.Button(tailRect, "×")) {
				Undo.RecordObject(this, "AudioVolumeUnify.ListRemove");
				EditorApplication.delayCall += () => {
					m_InfoList.RemoveAt(index);
					Repaint();
				};
			}
		}

		private void DrawUnifiedVolume() {
			float prevFieldWidth = EditorGUIUtility.fieldWidth;
			EditorGUIUtility.fieldWidth = 80F;
			EditorGUI.BeginChangeCheck();
			float newUnifiedVolume = EditorGUILayout.Slider("统一音量", m_UnifiedVolume, 0, 1);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(this, "AudioVolumeUnify.UnifiedVolume");
				m_UnifiedVolume = newUnifiedVolume;
			}
			EditorGUIUtility.fieldWidth = prevFieldWidth;
		}

		private void DrawSaveField() {
			List<string> filePaths = m_InfoList.ConvertAll(info => AssetDatabase.GetAssetPath(info.clip));
			bool wavExist = false;
			bool mp3Exist = false;
			bool oggExist = false;
			foreach (string filePath in filePaths) {
				string filePathUpper = filePath.ToUpper();
				if (filePathUpper.EndsWith(".WAV")) {
					wavExist = true;
				} else if (filePathUpper.EndsWith(".MP3")) {
					mp3Exist = true;
				} else if (filePathUpper.EndsWith(".OGG")) {
					oggExist = true;
				}
			}

			if (wavExist || mp3Exist) {
				int bitsPerSampleIndex = Array.IndexOf(BITS_PER_SAMPLES, m_BitsPerSample);
				int newBitsPerSampleIndex =
						EditorGUILayout.Popup("位深度", bitsPerSampleIndex, Array.ConvertAll(BITS_PER_SAMPLES, b => b + ""));
				if (newBitsPerSampleIndex != bitsPerSampleIndex) {
					Undo.RecordObject(this, $"AudioClipper.BitsPerSample {BITS_PER_SAMPLES[newBitsPerSampleIndex]}");
					m_BitsPerSample = BITS_PER_SAMPLES[newBitsPerSampleIndex];
				}
			}

			if (mp3Exist) {
				int mp3QualityIndex = Array.IndexOf(MP3_QUALITIES, m_Mp3Quality);
				int newMp3QualityIndex =
						EditorGUILayout.Popup("MP3平均比特率", mp3QualityIndex, Array.ConvertAll(MP3_QUALITIES, q => q + "Kbps"));
				if (newMp3QualityIndex != mp3QualityIndex) {
					Undo.RecordObject(this, $"AudioClipper.Mp3Quality {MP3_QUALITIES[newMp3QualityIndex]}");
					m_Mp3Quality = MP3_QUALITIES[newMp3QualityIndex];
				}
			}

			if (oggExist) {
				EditorGUILayout.BeginHorizontal();
				int oggQualityPercent = Mathf.RoundToInt(m_OggQuality * 100);
				int newOggQualityPercent = EditorGUILayout.IntSlider("OGG品质", oggQualityPercent, 0, 100);
				EditorGUILayout.LabelField("%", GUILayout.Width(12F));
				if (newOggQualityPercent != oggQualityPercent) {
					Undo.RecordObject(this, $"AudioClipper.Mp3Quality {newOggQualityPercent * 0.01F}");
					m_OggQuality = newOggQualityPercent * 0.01F;
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("生成副本")) {
				WriteAll(true);
			}
			if (GUILayout.Button("直接覆盖", GUILayout.Width(80F))) {
				WriteAll(false);
				foreach (AudioInfo info in m_InfoList) {
					if (info.clip) {
						info.volume = GetMaxVolume(info.clip);
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void PlayAudio(AudioClip clip, float volumeScale) {
			if (clip != null) {
				m_AudioSource.clip = clip;
				m_AudioSource.volume = volumeScale;
				m_AudioSource.Play();
			} else {
				Debug.LogError("Clip is none!");
			}
		}

		private void StopAudio() {
			m_AudioSource.Stop();
			m_AudioSource.clip = null;
			m_AudioSource.volume = 1;
		}

		private void WriteAll(bool clone) {
			int count = 0;
			foreach (AudioInfo info in m_InfoList) {
				if (Write(info.clip, info.volume == 0 ? 0 : m_UnifiedVolume / info.volume, clone)) {
					count++;
				}
			}
			// 刷新
			AssetDatabase.Refresh();
			string text = $"缩放完成，{count}个资源被{(clone ? "复制" : "改动")}。";
			ShowNotification(EditorGUIUtility.TrTextContent(text), 1);
			Debug.Log(text);
		}

		private bool Write(AudioClip clip, float timeScale, bool clone) {
			string filePath = AssetDatabase.GetAssetPath(clip);
			if (!string.IsNullOrEmpty(filePath)) {
				string outputPath = filePath;
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
				if (AudioClipWriter.GetClipData(clip, out float[] data)) {
					for (int i = 0, length = data.Length; i < length; i++) {
						data[i] *= timeScale;
					}
					string outputPathUpper = outputPath.ToUpper();
					if (outputPathUpper.EndsWith("WAV")) {
						AudioClipWriter.WriteWAV(outputPath, data, m_BitsPerSample, clip.channels, clip.frequency);
						return true;
					} else if (outputPathUpper.EndsWith("MP3")) {
						AudioClipWriter.WriteMP3(outputPath, data, m_BitsPerSample, clip.channels, clip.frequency, m_Mp3Quality);
						return true;
					} else if (outputPathUpper.EndsWith("OGG")) {
						AudioClipWriter.WriteOGG(outputPath, data, clip.channels, clip.frequency, m_OggQuality);
						return true;
					} else {
						Debug.LogError("Unsupported file format.");
					}
				}
			}
			return false;
		}

		private static float GetMaxVolume(AudioClip clip) {
			if (clip) {
				float[] data = new float[clip.samples * clip.channels];
				if (clip.GetData(data, 0)) {
					float maxVolume = 0;
					foreach (float value in data) {
						float absValue = Mathf.Abs(value);
						if (absValue > maxVolume) {
							maxVolume = absValue;
						}
					}
					return maxVolume;
				}
			}
			return 1;
		}
	}
}