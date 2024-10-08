﻿/*
 * @Author: wangyun
 * @CreateTime: 2024-06-21 16:02:40 059
 * @LastEditor: wangyun
 * @EditTime: 2024-06-27 21:32:42 639
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace WYTools.AudioFactory {
	public class AudioClipper : EditorWindow {
		public enum DraggingType {
			NONE,
			START_TIME,
			END_TIME,
			START_END_TIMES
		}
		
		[MenuItem("Tools/WYTools/Audio Clipper")]
		public static void ShowWindow() {
			AudioClipper window = GetWindow<AudioClipper>();
			window.minSize = new Vector2(420F, 400F);
			window.Show();
		}
		
		private const int WAVEFORM_WIDTH = 2048;
		private const int WAVEFORM_HEIGHT = 128;
		private const float BACKGROUND_CELL_WIDTH_MIN = 34;	// 需要显示时间，不能太小
		private const float BACKGROUND_CELL_HEIGHT = 16;
		private const float RULER_LABEL_HEIGHT = 16;
		private const float RULER_LINE_HEIGHT = 8;
		private const float DRAGGABLE_EXT_THICKNESS = 10F;	// 拖动响应范围往外扩展距离
		
		private static readonly Color COLOR_RULER_LONG = Color.gray;
		private static readonly Color COLOR_RULER_SHORT = new Color(0.5F, 0.5F, 0.5F, 0.5F);
		private static readonly Color COLOR_CHANNEL_BORDER = Color.white;
		private static readonly Color COLOR_BACKGROUND = Color.black;
		private static readonly Color COLOR_BACKGROUND_GRID = new Color(1, 0.5F, 0, 0.2F);
		private static readonly Color COLOR_WAVEFORM = new Color(1, 0.5F, 0);
		private static readonly Color COLOR_UNSELECTED = new Color(0, 0, 0, 0.7F);
		private static readonly Color COLOR_SELECTED_DRAGGING = new Color(1, 1, 1, 0.15F);
		private static readonly Color COLOR_SELECTOR = new Color(0.5F, 1, 1);
		private static readonly Color COLOR_SELECTOR_DRAGGING = new Color(0, 1, 0.5F);
		private static readonly Color COLOR_CURRENT = new Color(1, 0, 0);

		private static readonly string[] FILE_FORMATS = { "WAV", "MP3", "OGG" };
		private static readonly int[] BITS_PER_SAMPLES = { 8, 16, 24, 32 };
		private static readonly int[] MP3_QUALITIES = { 64, 96, 128, 160, 256, 320 };

		private static GUIStyle m_RulerStyle;
		private static Texture2D m_TexStop;

		[SerializeField] private AudioClip m_Clip;
		[SerializeField] private float m_Duration;
		[SerializeField] private float m_MaxVolume;
		
		[SerializeField] private float m_ViewStartTime;
		[SerializeField] private float m_ViewEndTime;
		[SerializeField] private float m_ClipStartTime;
		[SerializeField] private float m_ClipEndTime;
		[SerializeField] private float m_VolumeScale = 1;
		
		[SerializeField] private bool m_Trim;
		[SerializeField] private float m_TrimThreshold;
		
		[SerializeField] private AudioClip m_ClippedClip;
		[SerializeField] private AudioSource m_AudioSource;
		[SerializeField] private Texture2D[] m_WaveformTextures = Array.Empty<Texture2D>();

		[SerializeField] private string m_FileFormat = "WAV";
		[SerializeField] private int m_BitsPerSample = 16;
		[SerializeField] private int m_Mp3Quality = 128;
		[SerializeField] private float m_OggQuality = 0.4F;
		
		private readonly Stack<Texture2D> m_TexturePool = new Stack<Texture2D>();
		
		private DraggingType m_DraggingType;
		private Vector2 m_DragPrevPos;

		private void Awake() {
			UpdateWaveformTexture();
			m_AudioSource = EditorUtility.CreateGameObjectWithHideFlags("[AudioClipper]", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
		}

		private void OnEnable() {
			if (!m_TexStop) {
				m_TexStop = CreateTexStop();
			}
			Undo.undoRedoPerformed += () => {
				UpdateWaveformTexture();
				Repaint();
			};
		}

		private void OnDisable() {
			Undo.undoRedoPerformed -= Repaint;
		}

		private void OnDestroy() {
			DestroyImmediate(m_AudioSource);
			m_Clip = null;
			m_ClippedClip = null;
			m_AudioSource = null;
			m_WaveformTextures = null;
			m_TexturePool.Clear();
			Resources.UnloadUnusedAssets();
		}

		private bool m_WillRepaint;
		private void Update() {
			if (m_AudioSource.isPlaying) {
				Repaint();
				m_WillRepaint = true;
			} else if (m_WillRepaint) {
				Repaint();
				m_WillRepaint = false;
			}
		}

		private void ShowButton(Rect rect) {
			if (GUI.Button(rect, EditorGUIUtility.IconContent("_Help"), "IconButton")) {
				PopupWindow.Show(rect, new PopupContent(260, 100, popupRect => {
					popupRect.x += 6;
					EditorGUI.LabelField(
							popupRect,
							"拖动左「边界线」可调整开始时间。\n" +
							"拖动右「边界线」可调整结束时间。\n" +
							"拖动中间「高亮区域」可整体调整选中时间段。\n" +
							"拖动时按住「Ctrl键」可忽略吸附效果。\n" +
							"滚动鼠标「滚轮」可缩放波形图。\n" +
							"调整后的音量可保存至文件。"
					);
				}));
			}
		}

		private void OnGUI() {
			if (m_RulerStyle == null) {
				m_RulerStyle = CreateRulerStyle();
			}

			DrawAudioClipField();
			DrawAudioClipInfo();

			GUILayout.Space(10);
			
			DrawWaveformField();
			DrawTime();
			GUILayout.Space(-EditorGUIUtility.singleLineHeight - 2);
			DrawPreviewButtons();
			
			GUILayout.Space(10);
			
			DrawTrimField();
			
			GUILayout.Space(10);
			
			DrawSaveField();
		}

		#region OnGUI
		private void DrawAudioClipField() {
			AudioClip newClip = EditorGUILayout.ObjectField("声音源文件", m_Clip, typeof(AudioClip), false) as AudioClip;
			if (newClip != m_Clip) {
				Undo.RecordObject(this, $"AudioClipper.Clip {(newClip != null ? newClip.name : "null")}");
				m_Clip = newClip;
				m_ClippedClip = null;
				m_AudioSource.clip = null;
				m_MaxVolume = GetMaxVolume(m_Clip);
				m_Trim = false;
				m_TrimThreshold = 0;
				if (newClip != null) {
					m_Duration = newClip.length;
					m_ViewStartTime = 0;
					m_ViewEndTime = m_Duration;
					m_ClipStartTime = 0;
					m_ClipEndTime = m_Duration;
					m_VolumeScale = 1;
					string srcPathLower = AssetDatabase.GetAssetPath(newClip).ToUpper();
					if (srcPathLower.EndsWith(".MP3")) {
						m_FileFormat = "MP3";
					} else if (srcPathLower.EndsWith(".OGG")) {
						m_FileFormat = "OGG";
					} else {
						m_FileFormat = "WAV";
					}
					m_BitsPerSample = 16;
					m_Mp3Quality = 128;
					m_OggQuality = 0.4F;
				} else {
					m_Duration = 0;
					m_ViewStartTime = 0;
					m_ViewEndTime = m_Duration;
					m_ClipStartTime = 0;
					m_ClipEndTime = m_Duration;
					m_VolumeScale = 1;
					m_FileFormat = "WAV";
					m_BitsPerSample = 16;
					m_Mp3Quality = 128;
					m_OggQuality = 0.4F;
				}
				UpdateWaveformTexture();
			}
		}

		private void DrawAudioClipInfo() {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"时长: {((m_Clip ? m_Duration + "s" : "-"))}", GUILayout.MinWidth(0F));
			EditorGUILayout.LabelField($"声道: {(m_Clip ? m_Clip.channels + "" : "-")}", GUILayout.MinWidth(0F));
			EditorGUILayout.LabelField($"采样率: {(m_Clip ? m_Clip.frequency + "" : "-")}", GUILayout.MinWidth(0F));
			EditorGUILayout.EndHorizontal();
		}

		private void DrawWaveformField() {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(4);
			Rect fieldRect = EditorGUILayout.BeginVertical();
			DrawWaveformScaler(m_Clip ? m_Duration : 0);
			float viewDuration = m_Clip ? m_ViewEndTime - m_ViewStartTime : 5.09F;
			float viewEndTime = m_Clip ? m_ViewEndTime : 5.09F;
			float blockDuration = GetRulerBlockDuration(fieldRect, m_ViewStartTime, viewEndTime);
			DrawWaveformRuler(fieldRect.width, m_ViewStartTime, viewEndTime, blockDuration);
			Rect waveformRect = DrawWaveform(fieldRect.width, m_ViewStartTime, viewEndTime, blockDuration, m_WaveformTextures);
			if (m_Clip) {
				DrawSelector(fieldRect, waveformRect, viewDuration, blockDuration);
			}
			EditorGUILayout.EndVertical();
			GUILayout.Space(4);
			EditorGUILayout.EndHorizontal();
		}

		private void DrawTime() {
			EditorGUILayout.BeginHorizontal();
			float prevLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 50F;
			EditorGUI.BeginChangeCheck();
			float clipStartTime = Mathf.Clamp(EditorGUILayout.FloatField("开始时间", m_ClipStartTime, GUILayout.Width(120F)), 0, m_ClipEndTime);
			GUILayout.FlexibleSpace();
			float clipEndTime = Mathf.Clamp(EditorGUILayout.FloatField("结束时间", m_ClipEndTime, GUILayout.Width(120F)), m_ClipStartTime, m_Duration);
			if (EditorGUI.EndChangeCheck()) {
				string undoGroupName = $"AudioClipper.Time {clipStartTime}-{clipEndTime}";
				Undo.RecordObject(this, undoGroupName);
				Undo.SetCurrentGroupName(undoGroupName);
				m_ClipStartTime = clipStartTime;
				m_ClipEndTime = clipEndTime;
				m_ClippedClip = null;
				m_AudioSource.clip = null;
			}
			EditorGUIUtility.labelWidth = prevLabelWidth;
			EditorGUILayout.EndHorizontal();
		}

		private void DrawPreviewButtons() {
			Color prevContentColor = GUI.contentColor;
			GUI.contentColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			float audioSourceTime = m_AudioSource.time;
			bool stopDisabled = !m_AudioSource.clip || audioSourceTime <= 0 || audioSourceTime >= m_Duration;
			EditorGUI.BeginDisabledGroup(stopDisabled);
			if (GUILayout.Button(EditorGUIUtility.TrIconContent(m_TexStop))) {
				StopPlayClippedAudio();
			}
			EditorGUI.EndDisabledGroup();
			
			if (GUILayout.Button(EditorGUIUtility.IconContent("PlayButton On"))) {
				PlayClippedAudio();
			}
			
			EditorGUI.BeginDisabledGroup(!m_AudioSource.isPlaying);
			if (GUILayout.Button(EditorGUIUtility.IconContent("PauseButton On"))) {
				PausePlayClippedAudio();
			}
			EditorGUI.EndDisabledGroup();
			
			bool newLoop = GUILayout.Toggle(m_AudioSource.loop, EditorGUIUtility.IconContent("preAudioLoopOff"), "Button");
			if (newLoop != m_AudioSource.loop) {
				Undo.RecordObject(m_AudioSource, $"AudioClipper.Loop {newLoop}");
				m_AudioSource.loop = newLoop;
			}
			
			Rect rect = EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(EditorGUIUtility.IconContent("SceneviewAudio On"))) {
				const float BORDER = 2;
				PopupWindow.Show(rect, new PopupContent(200, EditorGUIUtility.singleLineHeight, popupRect => {
					popupRect.x += BORDER;
					Vector2 pivotPoint = new Vector2(popupRect.x, popupRect.y + popupRect.height / 2);
					float scale = (popupRect.height - BORDER - BORDER) / popupRect.height;
					popupRect.width = (popupRect.width - BORDER - BORDER) / scale;
					EditorGUI.BeginChangeCheck();
					GUIUtility.ScaleAroundPivot(Vector2.one * scale, pivotPoint);
					float newVolume = EditorGUI.Slider(popupRect, m_VolumeScale, 0, 2 / m_MaxVolume);
					GUIUtility.ScaleAroundPivot(Vector2.one / scale, pivotPoint);
					if (EditorGUI.EndChangeCheck()) {
						string undoGroupName = $"AudioClipper.VolumeScale {newVolume}";
						Undo.RecordObject(this, undoGroupName);
						Undo.SetCurrentGroupName(undoGroupName);
						m_VolumeScale = newVolume;
						m_ClippedClip = null;
						UpdateWaveformTexture();
						Repaint();
					}
				}));
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			
			GUI.contentColor = prevContentColor;
		}

		private void DrawTrimField() {
			EditorGUILayout.BeginHorizontal();
			bool newTrim = GUILayout.Toggle(m_Trim, "裁剪首尾空白", "Button", GUILayout.Width(100F));
			if (newTrim != m_Trim) {
				string undoGroupName = $"AudioClipper.Trim {newTrim}";
				Undo.RecordObject(this, undoGroupName);
				m_Trim = newTrim;
				if (m_Trim) {
					(m_ClipStartTime, m_ClipEndTime) = GetClipRangeForTrim(m_Clip, m_TrimThreshold);
				} else {
					m_TrimThreshold = 0;
				}
			}
			
			GUILayout.Space(20F);
			
			EditorGUI.BeginDisabledGroup(!m_Trim);
			EditorGUI.BeginChangeCheck();
			float prevLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 30F;
			float newTrimThreshold = EditorGUILayout.Slider("阈值", m_TrimThreshold, 0, m_MaxVolume);
			if (EditorGUI.EndChangeCheck()) {
				string undoGroupName = $"AudioClipper.TrimThreshold {newTrimThreshold}";
				Undo.RecordObject(this, undoGroupName);
				Undo.SetCurrentGroupName(undoGroupName);
				m_TrimThreshold = newTrimThreshold;
				(m_ClipStartTime, m_ClipEndTime) = GetClipRangeForTrim(m_Clip, m_TrimThreshold);
			}
			EditorGUIUtility.labelWidth = prevLabelWidth;
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		}

		private void DrawSaveField() {
			int fileFormatIndex = Array.IndexOf(FILE_FORMATS, m_FileFormat);
			int newFileFormatIndex = EditorGUILayout.Popup("保存为", fileFormatIndex, FILE_FORMATS);
			if (newFileFormatIndex != fileFormatIndex) {
				Undo.RecordObject(this, $"AudioClipper.FileFormat {FILE_FORMATS[newFileFormatIndex]}");
				m_FileFormat = FILE_FORMATS[newFileFormatIndex];
			}

			if (m_FileFormat is "WAV" || m_FileFormat is "MP3") {
				int bitsPerSampleIndex = Array.IndexOf(BITS_PER_SAMPLES, m_BitsPerSample);
				int newBitsPerSampleIndex = EditorGUILayout.Popup("位深度", bitsPerSampleIndex, Array.ConvertAll(BITS_PER_SAMPLES, b=> b + ""));
				if (newBitsPerSampleIndex != bitsPerSampleIndex) {
					Undo.RecordObject(this, $"AudioClipper.BitsPerSample {BITS_PER_SAMPLES[newBitsPerSampleIndex]}");
					m_BitsPerSample = BITS_PER_SAMPLES[newBitsPerSampleIndex];
				}
			}
			switch (m_FileFormat) {
				case "MP3":
					int mp3QualityIndex = Array.IndexOf(MP3_QUALITIES, m_Mp3Quality);
					int newMp3QualityIndex = EditorGUILayout.Popup("比特率", mp3QualityIndex, Array.ConvertAll(MP3_QUALITIES, q=> q + "Kbps"));
					if (newMp3QualityIndex != mp3QualityIndex) {
						Undo.RecordObject(this, $"AudioClipper.Mp3Quality {MP3_QUALITIES[newMp3QualityIndex]}");
						m_Mp3Quality = MP3_QUALITIES[newMp3QualityIndex];
					}
					break;
				case "OGG":
					EditorGUILayout.BeginHorizontal();
					int oggQualityPercent = Mathf.RoundToInt(m_OggQuality * 100);
					int newOggQualityPercent = EditorGUILayout.IntSlider("品质", oggQualityPercent, 0, 100);
					EditorGUILayout.LabelField("%", GUILayout.Width(12F));
					if (newOggQualityPercent != oggQualityPercent) {
						Undo.RecordObject(this, $"AudioClipper.Mp3Quality {newOggQualityPercent * 0.01F}");
						m_OggQuality = newOggQualityPercent * 0.01F;
					}
					EditorGUILayout.EndHorizontal();
					break;
			}
			if (GUILayout.Button("保存片段", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2 + 2))) {
				WriteClippedAudio();
			}
		}
		#endregion

		#region Waveform
		private void DrawWaveformScaler(float duration) {
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.MinMaxSlider(ref m_ViewStartTime, ref m_ViewEndTime, 0, duration);
			if (EditorGUI.EndChangeCheck()) {
				UpdateWaveformTexture();
			}
		}
		
		private float GetRulerBlockDuration(Rect fieldRect, float viewStartTime, float viewEndTime) {
			float viewDuration = viewEndTime - viewStartTime;
			int blocksMax = Mathf.FloorToInt(fieldRect.width / BACKGROUND_CELL_WIDTH_MIN);
			float[] gaps = {0.001F, 0.002F, 0.005F, 0.01F, 0.02F, 0.05F, 0.1F, 0.2F, 0.5F};
			float blockDuration = 0;
			for (int i = 0, length = gaps.Length; i < length; i++) {
				float gap = gaps[i];
				if (gap * blocksMax > viewDuration) {
					blockDuration = gap * 0.5F;
					break;
				}
			}
			if (blockDuration == 0) {
				// 向上取证，让大格子为整秒，小格子为整半秒
				blockDuration = Mathf.Ceil(viewDuration / blocksMax) * 0.5F;
			}
			return blockDuration;
		}

		private void DrawWaveformRuler(float fieldWidth, float viewStartTime, float viewEndTime, float blockDuration) {
			int blockIndexStart = Mathf.CeilToInt(viewStartTime / blockDuration);
			int blockIndexEnd = Mathf.FloorToInt(viewEndTime / blockDuration);
			if (blockIndexStart == 0) {
				blockIndexStart++;	// 最左端不显示时间（因为显示不下）
			}
			if (blockIndexEnd > m_Duration / blockDuration - 0.5F) {
				blockIndexEnd--;	// 最右端不显示时间（因为显示不下）
			}
			float viewDuration = viewEndTime - viewStartTime;
			Rect rulerRect = GUILayoutUtility.GetRect(fieldWidth, RULER_LABEL_HEIGHT + RULER_LINE_HEIGHT);
			for (int i = blockIndexStart; i <= blockIndexEnd; i++) {
				float time = blockDuration * i;
				float timeOnView = time - viewStartTime;
				float xOnField = timeOnView / viewDuration * fieldWidth;
				if ((i & 1) == 1) {
					Rect shortLineRect = new Rect(rulerRect.x + xOnField, rulerRect.y + RULER_LABEL_HEIGHT + RULER_LINE_HEIGHT * 0.3F, 1, RULER_LINE_HEIGHT * 0.7F);
					EditorGUI.DrawRect(shortLineRect, COLOR_RULER_SHORT);
				} else if (i != 0) {
					Rect labelRect = new Rect(rulerRect.x + xOnField - BACKGROUND_CELL_WIDTH_MIN * 0.5F, rulerRect.y, BACKGROUND_CELL_WIDTH_MIN, RULER_LABEL_HEIGHT);
					EditorGUI.LabelField(labelRect, $"{(blockDuration * i).ToString(blockDuration < 0.005F ? "F3" : "F2")}", m_RulerStyle);
					Rect longLineRect = new Rect(rulerRect.x + xOnField, rulerRect.y + RULER_LABEL_HEIGHT, 1, RULER_LINE_HEIGHT);
					EditorGUI.DrawRect(longLineRect, COLOR_RULER_LONG);
				}
			}
		}

		private Rect DrawWaveform(float fieldWidth, float viewStartTime, float viewEndTime, float blockDuration, IReadOnlyList<Texture2D> waveformTextures) {
			int realWaveformCount = waveformTextures.Count;
			int waveformCount = Mathf.Max(realWaveformCount, 1);
			
			Rect waveformRect = GUILayoutUtility.GetRect(fieldWidth, WAVEFORM_HEIGHT * waveformCount);
			for (int i = 0; i < waveformCount; i++) {
				// 画背景色
				float yTop = waveformRect.y + WAVEFORM_HEIGHT * i;
				Rect rect = new Rect(waveformRect.x, yTop, waveformRect.width, WAVEFORM_HEIGHT);
				EditorGUI.DrawRect(rect, COLOR_BACKGROUND);
				float yCenter = yTop + WAVEFORM_HEIGHT * 0.5F;
				// 画网格横线
				for (int j = 0; j * BACKGROUND_CELL_HEIGHT * 2 < WAVEFORM_HEIGHT; j++) {
					Rect lineRect = new Rect(waveformRect.x, yCenter + j * BACKGROUND_CELL_HEIGHT, waveformRect.width, 1);
					EditorGUI.DrawRect(lineRect, COLOR_BACKGROUND_GRID);
					lineRect.y = yCenter - j * BACKGROUND_CELL_HEIGHT;
					EditorGUI.DrawRect(lineRect, COLOR_BACKGROUND_GRID);
				}
				// 画轨道分界
				if (i > 0) {
					Rect lineRect = new Rect(waveformRect.x, yTop, waveformRect.width, 1);
					EditorGUI.DrawRect(lineRect, COLOR_CHANNEL_BORDER);
				}
			}
			// 画网格纵线
			int blockIndexStart = Mathf.CeilToInt(viewStartTime / blockDuration);
			int blockIndexEnd = Mathf.FloorToInt(viewEndTime / blockDuration);
			if (blockIndexStart == 0) {
				blockIndexStart++;	// 最左端不显示时间（因为显示不下）
			}
			if (blockIndexEnd > m_Duration / blockDuration - 0.5F) {
				blockIndexEnd--;	// 最右端不显示时间（因为显示不下）
			}
			float viewDuration = viewEndTime - viewStartTime;
			for (int i = blockIndexStart; i <= blockIndexEnd; i++) {
				float time = blockDuration * i;
				float timeOnView = time - viewStartTime;
				float xOnField = timeOnView / viewDuration * fieldWidth;
				Rect lineRect = new Rect(waveformRect.x + xOnField, waveformRect.y, 1, waveformRect.height);
				EditorGUI.DrawRect(lineRect, COLOR_BACKGROUND_GRID);
			}
			// 画波形图
			for (int i = 0; i < realWaveformCount; i++) {
				Rect rect = new Rect(waveformRect.x, waveformRect.y + WAVEFORM_HEIGHT * i, waveformRect.width, WAVEFORM_HEIGHT);
				GUI.DrawTexture(rect, waveformTextures[i]);
			}
			return waveformRect;
		}

		private void DrawSelector(Rect fieldRect, Rect waveformRect, float viewDuration, float blockDuration) {
			// 高亮的选中区域
			float clipStartPercentOnField = (m_ClipStartTime - m_ViewStartTime) / (m_ViewEndTime - m_ViewStartTime);
			float clipEndPercentOnField = (m_ClipEndTime - m_ViewStartTime) / (m_ViewEndTime - m_ViewStartTime);
			int selectedStartXOnField = Mathf.RoundToInt(Mathf.Max(clipStartPercentOnField, 0) * waveformRect.width);
			float selectedEndXOnField = Mathf.RoundToInt(Mathf.Min(clipEndPercentOnField, 1) * waveformRect.width);
			if (selectedEndXOnField > selectedStartXOnField) {
				Rect unselectedRect1 = new Rect(waveformRect.x, waveformRect.y, selectedStartXOnField, waveformRect.height);
				EditorGUI.DrawRect(unselectedRect1, COLOR_UNSELECTED);
				Rect unselectedRect2 = new Rect(waveformRect.x + selectedEndXOnField, waveformRect.y, waveformRect.width - selectedEndXOnField, waveformRect.height);
				EditorGUI.DrawRect(unselectedRect2, COLOR_UNSELECTED);
				Rect selectedRect = new Rect(waveformRect.x + selectedStartXOnField, waveformRect.y, selectedEndXOnField - selectedStartXOnField, waveformRect.height);
				if (m_DraggingType == DraggingType.START_END_TIMES) {
					EditorGUI.DrawRect(selectedRect, COLOR_SELECTED_DRAGGING);
				}
				selectedRect.y += selectedRect.height - EditorGUIUtility.singleLineHeight;
				selectedRect.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.LabelField(selectedRect, EditorGUIUtility.TrTextContent($"时长: {m_ClipEndTime - m_ClipStartTime}s"), "CenteredLabel");
			}

			// 选中区域的边界线
			Rect clipStartLineRect = new Rect(waveformRect.x + waveformRect.width * clipStartPercentOnField, waveformRect.y, 1, waveformRect.height);
			bool clipStartLineVisible = clipStartPercentOnField >= 0 && clipStartPercentOnField <= 1;
			if (clipStartLineVisible) {
				EditorGUI.DrawRect(clipStartLineRect, m_DraggingType == DraggingType.START_TIME ? COLOR_SELECTOR_DRAGGING : COLOR_SELECTOR);
			}
			Rect clipEndLineRect = new Rect(waveformRect.x + waveformRect.width * clipEndPercentOnField, waveformRect.y, 1, waveformRect.height);
			bool clipEndLineVisible = clipEndPercentOnField >= 0 && clipEndPercentOnField <= 1;
			if (clipEndLineVisible) {
				EditorGUI.DrawRect(clipEndLineRect, m_DraggingType == DraggingType.END_TIME ? COLOR_SELECTOR_DRAGGING : COLOR_SELECTOR);
			}

			// 试听进度线
			if (m_AudioSource.clip && m_AudioSource.time > 0 && m_AudioSource.time < viewDuration) {
				float currentTime = m_ClipStartTime + m_AudioSource.time;
				float currentPercentOnField = (currentTime - m_ViewStartTime) / (m_ViewEndTime - m_ViewStartTime);
				if (currentPercentOnField >= 0 && currentPercentOnField <= 1) {
					Rect currentLineRect = new Rect(waveformRect.x + waveformRect.width * currentPercentOnField, waveformRect.y, 1, waveformRect.height);
					EditorGUI.DrawRect(currentLineRect, COLOR_CURRENT);
					Color prevColor = GUI.contentColor;
					GUI.contentColor = COLOR_CURRENT;
					Rect currentLabelRect = new Rect(currentLineRect.x + 2, currentLineRect.y, 40, EditorGUIUtility.singleLineHeight - 2);
					EditorGUI.LabelField(currentLabelRect, $"{currentTime:F3}");
					GUI.contentColor = prevColor;
				}
			}

			switch (m_DraggingType) {
				case DraggingType.START_TIME:
				case DraggingType.END_TIME:
					EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeHorizontal);
					break;
				case DraggingType.START_END_TIMES:
					EditorGUIUtility.AddCursorRect(position, MouseCursor.Pan);
					break;
				default:
					Rect cursorRectStartEnd = new Rect(waveformRect.x + selectedStartXOnField + DRAGGABLE_EXT_THICKNESS, waveformRect.y,
							selectedEndXOnField - selectedStartXOnField - DRAGGABLE_EXT_THICKNESS - DRAGGABLE_EXT_THICKNESS, waveformRect.height);
					EditorGUIUtility.AddCursorRect(cursorRectStartEnd, MouseCursor.Pan);
					if (clipStartLineVisible) {
						Rect cursorRectStart = clipStartLineRect;
						cursorRectStart.x -= DRAGGABLE_EXT_THICKNESS;
						cursorRectStart.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
						cursorRectStart.xMax = Mathf.Min(cursorRectStart.xMax, (clipStartLineRect.x + clipEndLineRect.x) * 0.5F);
						EditorGUIUtility.AddCursorRect(cursorRectStart, MouseCursor.ResizeHorizontal);
					}
					if (clipEndLineVisible) {
						Rect cursorRectEnd = clipEndLineRect;
						cursorRectEnd.x -= DRAGGABLE_EXT_THICKNESS;
						cursorRectEnd.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
						cursorRectEnd.xMin = Mathf.Max(cursorRectEnd.xMin, (clipStartLineRect.x + clipEndLineRect.x) * 0.5F);
						EditorGUIUtility.AddCursorRect(cursorRectEnd, MouseCursor.ResizeHorizontal);
					}
					break;
			}

			switch (Event.current.type) {
				case EventType.MouseDown: {
					Vector2 mousePos = Event.current.mousePosition;
					if (mousePos.y >= fieldRect.y && mousePos.y <= fieldRect.y + fieldRect.height) {
						float middle = (clipStartLineRect.x + clipEndLineRect.x) * 0.5F;
						float temp1 = Mathf.Min(clipStartLineRect.x + DRAGGABLE_EXT_THICKNESS, middle);
						if (!clipStartLineVisible) {
							temp1 = waveformRect.x;
						}
						float temp2 = Mathf.Max(clipEndLineRect.x - DRAGGABLE_EXT_THICKNESS, middle);
						if (!clipEndLineVisible) {
							temp2 = waveformRect.x + waveformRect.width;
						}
						if (mousePos.x > clipStartLineRect.x - DRAGGABLE_EXT_THICKNESS && mousePos.x < temp1) {
							if (clipStartLineVisible) {
								m_DraggingType = DraggingType.START_TIME;
								m_DragPrevPos = mousePos;
							}
						} else if (mousePos.x >= temp1 && mousePos.x <= temp2) {
							m_DraggingType = DraggingType.START_END_TIMES;
							m_DragPrevPos = mousePos;
						} else if (mousePos.x >= temp2 && mousePos.x < clipEndLineRect.x + DRAGGABLE_EXT_THICKNESS) {
							if (clipEndLineVisible) {
								m_DraggingType = DraggingType.END_TIME;
								m_DragPrevPos = mousePos;
							}
						}
						Repaint();
					}
					break;
				}
				case EventType.MouseDrag: {
					if (m_DraggingType != DraggingType.NONE) {
						Vector2 mousePos = Event.current.mousePosition;
						float deltaX = mousePos.x - m_DragPrevPos.x;
						switch (m_DraggingType) {
							case DraggingType.START_TIME: {
								float newClipStartLineX = clipStartLineRect.x + deltaX;
								float newClipStartTime = (newClipStartLineX - waveformRect.x) / waveformRect.width * (m_ViewEndTime - m_ViewStartTime) + m_ViewStartTime;
								if (!Event.current.control) {
									float blockCountF = newClipStartTime / blockDuration;
									float blockCountI = Mathf.RoundToInt(blockCountF);
									if (Mathf.Abs(blockCountF - blockCountI) < 0.2F) {
										newClipStartTime = blockDuration * blockCountI;
									} else if (m_Duration / blockDuration - blockCountF < 0.2F) {
										newClipStartTime = m_Duration;
									}
								}
								newClipStartTime = Mathf.Clamp(newClipStartTime, 0, m_ClipEndTime);
								newClipStartLineX = (newClipStartTime - m_ViewStartTime) / (m_ViewEndTime - m_ViewStartTime) * waveformRect.width + waveformRect.x;
								mousePos.x = m_DragPrevPos.x + newClipStartLineX - clipStartLineRect.x;
								if (!Mathf.Approximately(newClipStartTime, m_ClipStartTime)) {
									string undoGroupName = $"AudioClipper.Time {newClipStartTime}-{m_ClipEndTime}";
									Undo.RecordObject(this, undoGroupName);
									Undo.SetCurrentGroupName(undoGroupName);
									m_ClipStartTime = newClipStartTime;
									m_Trim = false;
									m_TrimThreshold = 0;
								}
								break;
							}
							case DraggingType.START_END_TIMES: {
								float deltaTime = Mathf.Clamp(deltaX / waveformRect.width * (m_ViewEndTime - m_ViewStartTime), -m_ClipStartTime, m_Duration - m_ClipEndTime);
								float startTime = m_ClipStartTime + deltaTime;
								float endTime = m_ClipEndTime + deltaTime;
								if (!Mathf.Approximately(startTime, m_ClipStartTime) || !Mathf.Approximately(endTime, m_ClipEndTime)) {
									string undoGroupName = $"AudioClipper.Time {startTime}-{endTime}";
									Undo.RecordObject(this, undoGroupName);
									Undo.SetCurrentGroupName(undoGroupName);
									m_ClipStartTime = startTime;
									m_ClipEndTime = endTime;
									m_Trim = false;
									m_TrimThreshold = 0;
								}
								break;
							}
							case DraggingType.END_TIME: {
								float newClipEndLineX = clipEndLineRect.x + deltaX;
								float newClipEndTime = (newClipEndLineX - waveformRect.x) / waveformRect.width * (m_ViewEndTime - m_ViewStartTime) + m_ViewStartTime;
								if (!Event.current.control) {
									float blockCountF = newClipEndTime / blockDuration;
									float blockCountI = Mathf.RoundToInt(blockCountF);
									if (Mathf.Abs(blockCountF - blockCountI) < 0.2F) {
										newClipEndTime = blockDuration * blockCountI;
									} else if (m_Duration / blockDuration - blockCountF < 0.2F) {
										newClipEndTime = m_Duration;
									}
								}
								newClipEndTime = Mathf.Clamp(newClipEndTime, m_ClipStartTime, m_Duration);
								newClipEndLineX = (newClipEndTime - m_ViewStartTime) / (m_ViewEndTime - m_ViewStartTime) * waveformRect.width + waveformRect.x;
								mousePos.x = m_DragPrevPos.x + newClipEndLineX - clipEndLineRect.x;
								if (!Mathf.Approximately(newClipEndTime, m_ClipEndTime)) {
									string undoGroupName = $"AudioClipper.Time {m_ClipStartTime}-{newClipEndTime}";
									Undo.RecordObject(this, undoGroupName);
									Undo.SetCurrentGroupName(undoGroupName);
									m_ClipEndTime = newClipEndTime;
									m_Trim = false;
									m_TrimThreshold = 0;
								}
								break;
							}
						}
						m_DragPrevPos = mousePos;
						m_ClippedClip = null;
						m_AudioSource.clip = null;
						Repaint();
					}
					break;
				}
				case EventType.MouseUp:
				case EventType.Ignore: {
					if (m_DraggingType != DraggingType.NONE) {
						m_DraggingType = DraggingType.NONE;
						Repaint();
					}
					break;
				}
				case EventType.ScrollWheel: {
					if (m_DraggingType == DraggingType.NONE) {
						Vector2 mousePos = Event.current.mousePosition;
						if (mousePos.y >= fieldRect.y && mousePos.y <= fieldRect.y + fieldRect.height && 
								mousePos.x >= fieldRect.x && mousePos.x <= fieldRect.x + fieldRect.width) {
							float scrollValue = Event.current.delta.y;
							float mouseTime = Mathf.Lerp(m_ViewStartTime, m_ViewEndTime, (mousePos.x - fieldRect.x) / fieldRect.width);
							m_ViewStartTime = Mathf.Max(Mathf.LerpUnclamped(mouseTime, m_ViewStartTime, 1 + scrollValue * 0.1F), 0);
							m_ViewEndTime = Mathf.Min(Mathf.LerpUnclamped(mouseTime, m_ViewEndTime, 1 + scrollValue * 0.1F), m_Duration);
							UpdateWaveformTexture();
							Repaint();
						}
					}
					break;
				}
			}
		}
		#endregion

		#region Preview
		private void PlayClippedAudio() {
			if (m_Clip != null) {
				if (m_ClipEndTime > m_ClipStartTime) {
					if (m_ClippedClip == null) {
						m_ClippedClip = ClipAudio(m_Clip, m_ClipStartTime, m_ClipEndTime, m_VolumeScale);
					}
					m_AudioSource.clip = m_ClippedClip;
					m_AudioSource.Play();
				} else {
					Debug.LogError("Clipped empty!");
				}
			} else {
				Debug.LogError("Clip is none!");
			}
		}

		private void PausePlayClippedAudio() {
			m_AudioSource.Pause();
		}

		private void StopPlayClippedAudio() {
			m_AudioSource.Stop();
		}
		#endregion

		#region WaveformTexture
		private void UpdateWaveformTexture() {
			foreach (Texture2D waveformTexture in m_WaveformTextures) {
				if (waveformTexture && waveformTexture.height == WAVEFORM_HEIGHT) {
					m_TexturePool.Push(waveformTexture);
				}
			}
			m_WaveformTextures = m_Clip == null ? Array.Empty<Texture2D>() : GenerateWaveTextures(m_Clip, m_ViewStartTime, m_ViewEndTime);
		}

		private Texture2D[] GenerateWaveTextures(AudioClip clip, float startTime, float endTime, int width = WAVEFORM_WIDTH, int height = WAVEFORM_HEIGHT) {
			int channels = clip.channels;
			int frequency = clip.frequency;
			int startSample = (int) (startTime * frequency);
			int endSample = (int) (endTime * frequency);
			int lengthSamples = endSample - startSample;
			int lengthSamplesExt = Mathf.Min(lengthSamples + 1, clip.samples);	// 再加一个采样点，避免放大到单点连线时，最右端采样点缺失导致少一段
			
			Texture2D[] waveformTextures = new Texture2D[channels];
			if (lengthSamples <= 0) {
				// 如果长度为0，则返回空白图片
				Color[] colors = new Color[width * height];
				for (int channelIndex = 0; channelIndex < channels; channelIndex++) {
					Texture2D waveformTexture = m_TexturePool.Count > 0 ? m_TexturePool.Pop() : new Texture2D(width, height, TextureFormat.RGBA32, false) {hideFlags = HideFlags.HideAndDontSave};
					waveformTexture.SetPixels(colors);
					waveformTexture.Apply();
					waveformTextures[channelIndex] = waveformTexture;
				}
				return waveformTextures;
			}
			
			// 绘制波形图
			float[] data = new float[Mathf.Min(lengthSamples + 1, clip.samples) * channels];
			clip.GetData(data, startSample);
			for (int channelIndex = 0; channelIndex < channels; channelIndex++) {
				Texture2D waveformTexture = m_TexturePool.Count > 0 ? m_TexturePool.Pop() : new Texture2D(width, height, TextureFormat.RGBA32, false) {hideFlags = HideFlags.HideAndDontSave};
				Color[] colors = new Color[width * height];
				float pixelPerSample = (float) width / lengthSamples;
				if (pixelPerSample < 1) {
					// 遍历横向像素，每个横向像素绘制从最低采样点到最高采样点的柱状图
					for (int x = 0; x < width; x++) {
						float sampleValueMin = 1;
						float sampleValueMax = -1;
						int sampleStart = Mathf.FloorToInt(x / pixelPerSample);
						int sampleEnd = Mathf.Min(Mathf.FloorToInt((x + 1) / pixelPerSample), lengthSamples - 1);
						for (int sampleIndex = sampleStart; sampleIndex <= sampleEnd; sampleIndex++) {
							float sampleValue = data[sampleIndex * channels + channelIndex] * m_VolumeScale;
							if (sampleValue > sampleValueMax) sampleValueMax = sampleValue;
							if (sampleValue < sampleValueMin) sampleValueMin = sampleValue;
						}
						int yMax = Mathf.Clamp((int) ((sampleValueMax * 0.5F + 0.5F) * height), 0, height - 1);
						int yMin = Mathf.Clamp((int) ((sampleValueMin * 0.5F + 0.5F) * height), 0, height - 1);
						for (int y = yMin; y <= yMax; y++) {
							int colorIndex = y * width + x;
							colors[colorIndex] = COLOR_WAVEFORM;
						}
					}
				} else {
					// 遍历采样点，绘制采样点，再把采样点连起来
					int colorLength = width * height;
					for (int sampleIndex = 0, prevX = 0, prevY = 0; sampleIndex < lengthSamplesExt; sampleIndex++) {
						float sampleValue = data[sampleIndex * channels + channelIndex] * m_VolumeScale;
						int x = Mathf.RoundToInt(pixelPerSample * sampleIndex);
						int y = Mathf.Clamp((int) ((sampleValue * 0.5F + 0.5F) * height), 0, height - 1);
						int colorIndex = y * width + x;
						if (colorIndex < colorLength) {
							colors[colorIndex] = COLOR_WAVEFORM;
						}
						// 当每个采样点占地超过2r+1像素时，将采样点绘制成边长为2r+1的方块
						int[] radius = { 3, 2, 1 };
						foreach (int _radius in radius) {
							if (pixelPerSample > _radius + _radius + 1) {
								for (int dy = -_radius; dy <= _radius; dy++) {
									for (int dx = -_radius; dx <= _radius; dx++) {
										int _colorIndex = (y + dy) * width + x + dx;
										if (_colorIndex >= 0 && _colorIndex < colorLength) {
											colors[_colorIndex] = COLOR_WAVEFORM;
										}
									}
								}
								break;
							}
						}
						// 将每个采样点连接起来
						if (sampleIndex > 0) {
							if (Mathf.Abs(x - prevX) > Mathf.Abs(y - prevY)) {
								for (int _x = prevX + 1; _x < x; _x++) {
									int _y = (int) Mathf.Lerp(prevY, y, (float) (_x - prevX) / (x - prevX));
									int _colorIndex = _y * width + _x;
									colors[_colorIndex] = COLOR_WAVEFORM;
								}
							} else {
								(int yMin, int yMax) = y - prevY < 0 ? (y, prevY) : (prevY, y);
								for (int _y = yMin + 1; _y < yMax; _y++) {
									int _x = (int) Mathf.Lerp(prevX, x, (float) (_y - prevY) / (y - prevY));
									int _colorIndex = _y * width + _x;
									colors[_colorIndex] = COLOR_WAVEFORM;
								}
							}
						}
						prevX = x;
						prevY = y;
					}
				}
				waveformTexture.SetPixels(colors);
				waveformTexture.Apply();
				waveformTextures[channelIndex] = waveformTexture;
			}
			return waveformTextures;
		}
		#endregion

		#region Trim
		private float GetMaxVolume(AudioClip clip) {
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

		private (float startTime, float endTime) GetClipRangeForTrim(AudioClip clip, float trimThreshold) {
			if (clip) {
				float startTime = 0;
				float endTime = clip.length;
				int samples = clip.samples;
				int channels = clip.channels;
				float[] data = new float[samples * channels];
				if (clip.GetData(data, 0)) {
					float frequency = clip.frequency;
					for (int i = 0; i < samples; i++) {
						bool b = false;
						for (int j = 0; j < channels; j++) {
							if (Mathf.Abs(data[channels * i + j]) >= trimThreshold) {
								b = true;
								break;
							}
						}
						if (b) {
							startTime = i / frequency;
							break;
						}
					}
					for (int i = samples - 1; i >= 0; i--) {
						bool b = false;
						for (int j = 0; j < channels; j++) {
							if (Mathf.Abs(data[channels * i + j]) >= trimThreshold) {
								b = true;
								break;
							}
						}
						if (b) {
							endTime = i / frequency;
							break;
						}
					}
				}
				return (startTime, endTime);
			}
			return (0, 0);
		}
		#endregion

		#region Write
		private void WriteClippedAudio() {
			if (m_Clip != null) {
				if (m_ClipEndTime > m_ClipStartTime) {
					if (m_ClippedClip == null) {
						m_ClippedClip = ClipAudio(m_Clip, m_ClipStartTime, m_ClipEndTime, m_VolumeScale);
					}
					string srcFilePath = AssetDatabase.GetAssetPath(m_Clip);
					string directory = File.Exists(srcFilePath) ? srcFilePath.Substring(0, srcFilePath.LastIndexOfAny(new[] {'/', '\\'})) : "Assets";
					string filePath = EditorUtility.SaveFilePanel("保存剪辑后的音频", directory, m_Clip.name + "_New", m_FileFormat.ToLower());
					if (!string.IsNullOrEmpty(filePath)) {
						string pathUpper = filePath.ToUpper();
						if (pathUpper.EndsWith("WAV")) {
							AudioClipWriter.WriteWAV(filePath, m_ClippedClip, m_BitsPerSample);
						} else if (pathUpper.EndsWith("MP3")) {
							AudioClipWriter.WriteMP3(filePath, m_ClippedClip, m_BitsPerSample, m_Mp3Quality);
						} else if (pathUpper.EndsWith("OGG")) {
							AudioClipWriter.WriteOGG(filePath, m_ClippedClip, m_OggQuality);
						} else {
							Debug.LogError("Unsupported file format.");
						}
						AssetDatabase.Refresh();
					}
				} else {
					Debug.LogError("Clipped empty!");
				}
			} else {
				Debug.LogError("Clip is none!");
			}
		}
		#endregion

		#region Clip
		private static AudioClip ClipAudio(AudioClip clip, float startTime, float endTime, float volumeScale) {
			if (!clip) {
				throw new NullReferenceException("Clip is none.");
			}
			if (startTime > endTime) {
				throw new ArgumentException("Argument 'end' is less than 'start'.");
			}
			if (startTime < 0) {
				throw new OverflowException("Argument 'start' is less than 0.");
			}
			if (endTime > clip.length) {
				throw new OverflowException("Argument 'end' is greater than original length.");
			}
			volumeScale = Mathf.Max(volumeScale, 0);
			
			int channels = clip.channels;
			int frequency = clip.frequency;
			int startSample = (int) (startTime * frequency);
			int endSample = (int) (endTime * frequency);
			int lengthSamples = endSample - startSample;
			
			int newDataLength = lengthSamples * channels;
			float[] newData = new float[newDataLength];
			clip.GetData(newData, startSample);
			for (int i = 0; i < newDataLength; i++) {
				newData[i] *= volumeScale;
			}

			AudioClip clippedAudioClip = AudioClip.Create($"{clip.name}_Clipped", lengthSamples, channels, frequency, false);
			clippedAudioClip.SetData(newData, 0);
			clippedAudioClip.hideFlags = HideFlags.HideAndDontSave;

			return clippedAudioClip;
		}
		#endregion

		#region init
		private static Texture2D CreateTexStop() {
			const int WIDTH = 16;
			const int HEIGHT = 16;
			const int ICON_WIDTH = 10;
			const int ICON_HEIGHT = 10;
			const int OFFSET_X = WIDTH - ICON_WIDTH >> 1;
			const int OFFSET_Y = HEIGHT - ICON_HEIGHT >> 1;
				
			Color colorTransparent = new Color(1, 1, 1, 0);
			Color colorMain = new Color(1, 1, 1, 0.9F);
			Color colorCorner = new Color(1, 1, 1, 0.6F);

			Texture2D texStop = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGBA32, false) {
				hideFlags = HideFlags.HideAndDontSave
			};
			Color[] colors = texStop.GetPixels();
			for (int i = 0, length = colors.Length; i < length; i++) {
				colors[i] = colorTransparent;
			}
			for (int y = 0; y < ICON_WIDTH; y++) {
				for (int x = 0; x < ICON_HEIGHT; x++) {
					colors[(OFFSET_Y + y) * WIDTH + OFFSET_X + x] = colorMain;
				}
			}
			colors[OFFSET_Y * WIDTH + OFFSET_X] = colorCorner;
			colors[OFFSET_Y * WIDTH + OFFSET_X + ICON_WIDTH - 1] = colorCorner;
			colors[(OFFSET_Y + ICON_HEIGHT - 1) * WIDTH + OFFSET_X] = colorCorner;
			colors[(OFFSET_Y + ICON_HEIGHT - 1) * WIDTH + OFFSET_X + ICON_WIDTH - 1] = colorCorner;
			
			texStop.SetPixels(colors);
			texStop.Apply();
			return texStop;
		}

		private static GUIStyle CreateRulerStyle() {
			return new GUIStyle("CenteredLabel") {
				fontSize = 10
			};
		}
		#endregion
	}
}
