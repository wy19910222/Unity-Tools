/*
 * @Author: wangyun
 * @CreateTime: 2024-07-06 23:21:59 651
 * @LastEditor: wangyun
 * @EditTime: 2024-07-06 23:21:59 655
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ImageCropping : EditorWindow {
	[MenuItem("Tools/Image Cropping")]
	public static void ShowWindow() {
		ImageCropping window = GetWindow<ImageCropping>();
		window.minSize = new Vector2(400F, 300F);
		window.Show();
	}

	private enum ResizeType {
		NONE = 0,
		TOP_LEFT = 1,
		TOP = 2,
		TOP_RIGHT = 3,
		LEFT = 4,
		CENTER = 5,
		RIGHT = 6,
		BOTTOM_LEFT = 7,
		BOTTOM = 8,
		BOTTOM_RIGHT = 9,
	}

	private enum RectType {
		[InspectorName("左上宽高")]
		SIZE = 0,
		[InspectorName("对角坐标")]
		MIN_MAX = 1,
		[InspectorName("四边边距")]
		BORDER = 2,
	}

	private enum CornerType {
		[InspectorName("无")]
		NONE = 0,
		[InspectorName("圆角矩形")]
		ROUND_RECT = 1,
		[InspectorName("超椭圆")]
		MI_LOGO = 2,
	}

	private const float RULER_THICKNESS = 18F;	// 标尺的宽度
	private const float SCROLL_BAR_THICKNESS = 16F;	// 滚动条的宽度
	private const float SCROLL_BAR_PRECISION = 1000F;	// 缩放精度（最小为缩放到1像素）
	private const float SCALE_VISIBLE_H_BAR_WIDTH_MIN = 175F;	// 当横向滚动条小于该值时不显示缩放比例标签
	private const float SCALE_LABEL_WIDTH = 54F;	// 缩放比例标签的宽度
	private const float CANVAS_BORDER_DEFAULT_THICKNESS = 10F;	// 当纹理尺寸大于画布，默认缩放到离画布边缘的距离
	private const float CANVAS_BORDER_ZOOMED_THICKNESS = 10F;	// 当纹理尺寸大于画布，默认缩放到离画布边缘的距离
	private const float BLANK_DELTA_WITH_CANVAS = 100F;	// 当纹理尺寸大于画布，空白部分相对于画布宽度
	private const float SCALE_MAX = 32F;	// 最大缩放倍数（最小为缩放到1像素）
	private const float AUTO_SCROLL_SPEED = 500F;	// 鼠标拖动裁剪框到边缘外时，自动滚动的速度

	private static readonly Color SCROLL_BAR_BG_COLOR = new Color(0.25F, 0.25F, 0.25F);
	private static readonly Color PREVIEW_MASK_COLOR = new Color(0.2F, 0.2F, 0.2F, 0.7F);
	private static readonly Color CROPPING_CORNERED_COLOR = new Color(0, 1F, 1F, 0.3F);

	[SerializeField] private Texture2D m_Tex;
	[SerializeField] private RectInt m_CroppingRect;
	
	[SerializeField] private CornerType m_CornerType;
	[SerializeField] private float m_RoundRadius;
	[SerializeField] private float m_HyperEllipticPower;
	[SerializeField] private float m_Softness;
	[SerializeField] private float m_EdgeMove;

	[SerializeField] private RectType m_RectType;
	[SerializeField] private float m_Scale = 1;
	[SerializeField] private float m_ContentX;
	[SerializeField] private float m_ContentY;
	
	[SerializeField] private bool m_QuickCroppingTop = true;
	[SerializeField] private bool m_QuickCroppingBottom = true;
	[SerializeField] private bool m_QuickCroppingLeft = true;
	[SerializeField] private bool m_QuickCroppingRight = true;
	
	[SerializeField] private bool m_IsPreview;
	[SerializeField] private RenderTexture m_PreviewTex;

	private Rect m_CanvasRect;
	private readonly List<(Rect, ResizeType)> m_ResizeRects = new List<(Rect, ResizeType)>();
	private ResizeType m_ResizeType;
	private Vector2 m_DragPrevPos;

	private Texture2D m_BlankTex;
	private Material m_Mat;
	private void OnEnable() {
		m_Mat = new Material(Shader.Find("Hidden/ImageCropping/Unlit"));
		m_BlankTex = new Texture2D(1, 1);
		m_BlankTex.SetPixel(0, 0, Color.clear);
		m_BlankTex.Apply();
	}

	private double timeSinceStartup;
	private void Update() {
		double newTimeSinceStartup = EditorApplication.timeSinceStartup;
		float deltaTime = (float) (newTimeSinceStartup - timeSinceStartup);
		timeSinceStartup = newTimeSinceStartup;
		HandleAutoScroll(deltaTime);
	}

	private void OnGUI() {
		DrawTargetField();
		DrawCroppingRectField();
		DrawQuickCroppingField();
		DrawCornerField();
		GUILayout.Space(5F);
		DrawPreviewBtnField();
		DrawCanvasField();
		DrawZoomField();
		GUILayout.Space(5F);
		DrawSaveField();
	}

	#region OnGUI

	private void DrawTargetField() {
		Texture2D newTex = EditorGUILayout.ObjectField("原图", m_Tex, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Texture2D;
		if (newTex != m_Tex) {
			Undo.RecordObject(this, "ImageCropping.Target");
			m_Tex = newTex;
			if (m_Tex) {
				int texWidth = m_Tex.width;
				int texHeight = m_Tex.height;
				m_CroppingRect = new RectInt(0, 0, texWidth, texHeight);
				m_CornerType = CornerType.NONE;
				m_RoundRadius = Mathf.Min(texWidth, texHeight) * 0.2F;
				m_HyperEllipticPower = 3;
				m_IsPreview = false;
				if (texWidth > m_CanvasRect.width || texHeight > m_CanvasRect.height) {
					float targetWidth = m_CanvasRect.width - CANVAS_BORDER_DEFAULT_THICKNESS - CANVAS_BORDER_DEFAULT_THICKNESS;
					float targetHeight = m_CanvasRect.height - CANVAS_BORDER_DEFAULT_THICKNESS - CANVAS_BORDER_DEFAULT_THICKNESS;
					m_Scale = Mathf.Min(targetWidth / texWidth, targetHeight / texHeight);
					m_ContentX = (m_CanvasRect.width - targetWidth) * 0.5F;
					m_ContentY = (m_CanvasRect.height - targetHeight) * 0.5F;
				} else {
					m_Scale = 1;
					m_ContentX = m_CanvasRect.width * 0.5F;
					m_ContentY = m_CanvasRect.height * 0.5F;
				}
			}
		}
	}

	private void DrawCroppingRectField() {
		float labelWidth = EditorGUIUtility.labelWidth;
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("裁剪区域", GUILayout.Width(labelWidth - 2F));
		foreach (RectType type in Enum.GetValues(typeof(RectType))) {
			bool selected = type == m_RectType;
			if (GUILayout.Toggle(selected, GetEnumInspectorName(type), "Button")) {
				m_RectType = type;
			}
		}
		EditorGUILayout.EndHorizontal();
		RectInt rect = m_CroppingRect;
		float space = labelWidth + 4F;
		EditorGUI.BeginChangeCheck();
		switch (m_RectType) {
			case RectType.SIZE:
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(space);
				rect = EditorGUILayout.RectIntField(rect);
				rect.width = Mathf.Max(rect.width, 0);
				rect.height = Mathf.Max(rect.height, 0);
				EditorGUILayout.EndHorizontal();
				break;
			case RectType.MIN_MAX:
				EditorGUIUtility.labelWidth = 34F;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(space);
				rect.xMin = Mathf.Min(EditorGUILayout.IntField("XMin", rect.xMin), rect.xMax);
				rect.yMin = Mathf.Min(EditorGUILayout.IntField("YMin", rect.yMin), rect.yMax);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(space);
				rect.xMax = Mathf.Max(EditorGUILayout.IntField("XMax", rect.xMax), rect.xMin);
				rect.yMax = Mathf.Max(EditorGUILayout.IntField("YMax", rect.yMax), rect.yMin);
				EditorGUILayout.EndHorizontal();
				EditorGUIUtility.labelWidth = labelWidth;
				break;
			case RectType.BORDER:
				EditorGUIUtility.labelWidth = 13F;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(space);
				int prevX = rect.x;
				rect.x = Mathf.Min(EditorGUILayout.IntField("L", prevX), rect.width + prevX);
				rect.width += prevX - rect.x;
				int texHeight = m_Tex ? m_Tex.height : rect.height;
				rect.height = Mathf.Max(texHeight - rect.y - EditorGUILayout.IntField("T", texHeight - rect.y - rect.height), 0);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(space);
				int texWidth = m_Tex ? m_Tex.width : rect.width;
				rect.width = Mathf.Max(texWidth - rect.x - EditorGUILayout.IntField("R", texWidth - rect.x - rect.width), 0);
				int prevY = rect.y;
				rect.y = Mathf.Min(EditorGUILayout.IntField("B", prevY), rect.height + prevY);
				rect.height += prevY - rect.y;
				EditorGUILayout.EndHorizontal();
				EditorGUIUtility.labelWidth = labelWidth;
				break;
		}
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(this, "ImageCropping.CroppingRect");
			m_CroppingRect = rect;
			UpdatePreviewTex();
		}
	}

	private void DrawQuickCroppingField() {
		string dirText = string.Empty;
		if (m_QuickCroppingTop) {
			dirText += "上";
		}
		if (m_QuickCroppingBottom) {
			dirText += "下";
		}
		if (m_QuickCroppingLeft) {
			dirText += "左";
		}
		if (m_QuickCroppingRight) {
			dirText += "右";
		}
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("快捷裁剪", GUILayout.Width(EditorGUIUtility.labelWidth - 2F));
		EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(dirText));
		if (GUILayout.Button("裁剪" + dirText + "空白", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2 + 4F))) {
			if (m_Tex) {
				Undo.RecordObject(this, "ImageCropping.CroppingRect");
				Color[] colors = GetTexturePixels(m_Tex);
				int width = m_Tex.width;
				int height = m_Tex.height;
				int trimX = 0;
				for (int x = 0; x < width; x++) {
					bool willBreak = false;
					for (int y = 0; y < height; y++) {
						if (colors[y * width + x].a > 0) {
							trimX = x;
							willBreak = true;
							break;
						}
					}
					if (willBreak) {
						break;
					}
				}
				int trimY = 0;
				for (int y = 0; y < height; y++) {
					bool willBreak = false;
					for (int x = 0; x < width; x++) {
						if (colors[y * width + x].a > 0) {
							trimY = y;
							willBreak = true;
							break;
						}
					}
					if (willBreak) {
						break;
					}
				}
				int trimWidth = width - trimX;
				for (int x = width - 1; x >= 0; x--) {
					bool willBreak = false;
					for (int y = 0; y < height; y++) {
						if (colors[y * width + x].a > 0) {
							trimWidth = x + 1 - trimX;
							willBreak = true;
							break;
						}
					}
					if (willBreak) {
						break;
					}
				}
				int trimHeight = height - trimY;
				for (int y = height - 1; y >= 0; y--) {
					bool willBreak = false;
					for (int x = 0; x < width; x++) {
						if (colors[y * width + x].a > 0) {
							trimHeight = y + 1 - trimY;
							willBreak = true;
							break;
						}
					}
					if (willBreak) {
						break;
					}
				}
				m_CroppingRect = new RectInt(trimX, trimY, trimWidth, trimHeight);
				UpdatePreviewTex();
			}
		}
		EditorGUI.EndDisabledGroup();
		
		EditorGUILayout.BeginVertical();
		bool newQuickCornerTop = GUILayout.Toggle(m_QuickCroppingTop, "上", "Button");
		if (newQuickCornerTop != m_QuickCroppingTop) {
			Undo.RecordObject(this, "ImageCropping.QuickCroppingOption");
			m_QuickCroppingTop = newQuickCornerTop;
		}
		bool newQuickCornerBottom = GUILayout.Toggle(m_QuickCroppingBottom, "下", "Button");
		if (newQuickCornerBottom != m_QuickCroppingBottom) {
			Undo.RecordObject(this, "ImageCropping.QuickCroppingOption");
			m_QuickCroppingBottom = newQuickCornerBottom;
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.BeginVertical();
		bool newQuickCornerLeft = GUILayout.Toggle(m_QuickCroppingLeft, "左", "Button");
		if (newQuickCornerLeft != m_QuickCroppingLeft) {
			Undo.RecordObject(this, "ImageCropping.QuickCroppingOption");
			m_QuickCroppingLeft = newQuickCornerLeft;
		}
		bool newQuickCornerRight = GUILayout.Toggle(m_QuickCroppingRight, "右", "Button");
		if (newQuickCornerRight != m_QuickCroppingRight) {
			Undo.RecordObject(this, "ImageCropping.QuickCroppingOption");
			m_QuickCroppingRight = newQuickCornerRight;
		}
		EditorGUILayout.EndVertical();
		
		EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(dirText));
		if (GUILayout.Button("重置" + dirText + "边缘", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2 + 4F))) {
			if (m_Tex) {
				Undo.RecordObject(this, "ImageCropping.CroppingRect");
				m_CroppingRect = new RectInt(0, 0, m_Tex.width, m_Tex.height);
				UpdatePreviewTex();
			}
		}
		EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndHorizontal();
	}

	private void DrawCornerField() {
		int width = m_CroppingRect.width;
		int height = m_CroppingRect.height;
		
		CornerType newType = (CornerType) EditorGUILayout.EnumPopup("平滑角", m_CornerType);
		if (newType != m_CornerType) {
			Undo.RecordObject(this, "ImageCropping.CornerType");
			m_CornerType = newType;
			UpdatePreviewTex();
		}

		EditorGUILayout.BeginHorizontal();
		switch (m_CornerType) {
			case CornerType.ROUND_RECT: {
				float roundRadiusMax = Mathf.Min(width, height) * 0.5F;
				float newRoundRadius = Mathf.Clamp(EditorGUILayout.FloatField("     圆角半径", m_RoundRadius), 0, roundRadiusMax);
				if (!Mathf.Approximately(newRoundRadius, m_RoundRadius)) {
					Undo.RecordObject(this, "ImageCropping.RoundRadius");
					m_RoundRadius = newRoundRadius;
					UpdatePreviewTex();
				}
				break;
			}
			case CornerType.MI_LOGO: {
				float newHyperEllipticPower = Mathf.Clamp(EditorGUILayout.FloatField("     幂", m_HyperEllipticPower), 0, 10);
				if (!Mathf.Approximately(newHyperEllipticPower, m_HyperEllipticPower)) {
					Undo.RecordObject(this, "ImageCropping.HyperEllipticPower");
					m_HyperEllipticPower = newHyperEllipticPower;
					UpdatePreviewTex();
				}
				break;
			}
		}
		if (m_CornerType != CornerType.NONE) {
			float softnessMax = (width + height) * 0.5F;
			float newSoftness = Mathf.Clamp(EditorGUILayout.FloatField("     柔和边缘", m_Softness), 0, softnessMax);
			if (!Mathf.Approximately(newSoftness, m_Softness)) {
				Undo.RecordObject(this, "ImageCropping.Softness");
				m_Softness = newSoftness;
				UpdatePreviewTex();
			}
			float edgeMoveMax = (width + height) * 0.5F;
			float newEdgeMove = Mathf.Clamp(EditorGUILayout.FloatField("     移动边缘", m_EdgeMove), -edgeMoveMax, edgeMoveMax);
			if (!Mathf.Approximately(newEdgeMove, m_EdgeMove)) {
				Undo.RecordObject(this, "ImageCropping.EdgeMove");
				m_EdgeMove = newEdgeMove;
				UpdatePreviewTex();
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	private void DrawPreviewBtnField() {
		bool newIsPreview = GUILayout.Toggle(m_IsPreview, "预览", "Button");
		if (newIsPreview != m_IsPreview) {
			Undo.RecordObject(this, "ImageCropping.IsPreview");
			m_IsPreview = newIsPreview;
			UpdatePreviewTex();
		}
	}

	private void DrawCanvasField() {
		Rect rect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

		Rect horizontalRulerRect = new Rect(rect.x + RULER_THICKNESS, rect.y, rect.width - RULER_THICKNESS - SCROLL_BAR_THICKNESS, RULER_THICKNESS);
		Rect verticalRulerRect = new Rect(rect.x, rect.y + RULER_THICKNESS, RULER_THICKNESS, rect.height - RULER_THICKNESS - SCROLL_BAR_THICKNESS);
		Rect verticalBarRect = new Rect(rect.xMax - SCROLL_BAR_THICKNESS, rect.y, SCROLL_BAR_THICKNESS, rect.height - SCROLL_BAR_THICKNESS);
		Rect horizontalBarRect = new Rect(rect.x, rect.yMax - SCROLL_BAR_THICKNESS, rect.width - SCROLL_BAR_THICKNESS, SCROLL_BAR_THICKNESS);

		EditorGUI.DrawRect(horizontalRulerRect, SCROLL_BAR_BG_COLOR);
		EditorGUI.DrawRect(verticalRulerRect, SCROLL_BAR_BG_COLOR);
		EditorGUI.DrawRect(verticalBarRect, SCROLL_BAR_BG_COLOR);
		EditorGUI.DrawRect(horizontalBarRect, SCROLL_BAR_BG_COLOR);

		Rect canvasRect = new Rect(horizontalRulerRect.x, verticalRulerRect.y, horizontalRulerRect.width, verticalRulerRect.height);
		if (Event.current.type == EventType.Repaint) {
			m_CanvasRect = canvasRect;
		}
		if (m_Tex) {
			int texWidth = m_Tex.width;
			int texHeight = m_Tex.height;
			int xMin = Mathf.Min(m_CroppingRect.xMin, 0);
			int xMax = Mathf.Max(m_CroppingRect.xMax, texWidth);
			int yMin = Mathf.Min(m_CroppingRect.yMin, 0);
			int yMax = Mathf.Max(m_CroppingRect.yMax, texHeight);
			float scaledContentWidth = (xMax - xMin) * m_Scale;
			float scaledContentHeight = (yMax - yMin) * m_Scale;
			DrawCanvas(canvasRect, scaledContentWidth, scaledContentHeight);
			DrawScrollBar(horizontalBarRect, verticalBarRect, scaledContentWidth, scaledContentHeight);

			Rect scaleLabelRect = new Rect(horizontalBarRect.x, horizontalBarRect.y, 0, horizontalBarRect.height);
			if (horizontalBarRect.width > SCALE_VISIBLE_H_BAR_WIDTH_MIN) {
				scaleLabelRect.width = SCALE_LABEL_WIDTH;
				horizontalBarRect.x += SCALE_LABEL_WIDTH;
				horizontalBarRect.width -= SCALE_LABEL_WIDTH;
			}
			DrawScaleLabel(scaleLabelRect);

			SetCursorRect();

			HandleMouseEvent(new Rect() {
					xMin = verticalRulerRect.x,
					yMin = horizontalRulerRect.y,
					xMax = horizontalRulerRect.xMax,
					yMax = verticalRulerRect.yMax
			});
		}
		EditorGUILayout.EndVertical();
	}

	private void DrawZoomField() {
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("快捷缩放", GUILayout.Width(EditorGUIUtility.labelWidth - 2F));
		if (GUILayout.Button("100%")) {
			float centerX = m_CanvasRect.width * 0.5F;
			float centerY = m_CanvasRect.height * 0.5F;
			m_ContentX = centerX - (centerX - m_ContentX) / m_Scale;
			m_ContentY = centerY - (centerY - m_ContentY) / m_Scale;
			m_Scale = 1;
		}
		if (GUILayout.Button("原图适应屏幕")) {
			if (m_Tex) {
				int texWidth = m_Tex.width;
				int texHeight = m_Tex.height;
				m_Scale = Mathf.Min((m_CanvasRect.width - CANVAS_BORDER_ZOOMED_THICKNESS - CANVAS_BORDER_ZOOMED_THICKNESS) / texWidth,
						(m_CanvasRect.height - CANVAS_BORDER_ZOOMED_THICKNESS - CANVAS_BORDER_ZOOMED_THICKNESS) / texHeight);
				float texX = (m_CanvasRect.width - texWidth * m_Scale) * 0.5F;
				float texY = (m_CanvasRect.height - texHeight * m_Scale) * 0.5F;
				float borderLeft = m_CroppingRect.xMin * m_Scale;
				float borderTop = (texHeight - m_CroppingRect.yMax) * m_Scale;
				m_ContentX = texX + Mathf.Min(borderLeft, 0);
				m_ContentY = texY + Mathf.Min(borderTop, 0);
			}
		}
		if (GUILayout.Button("整体适应屏幕")) {
			if (m_Tex) {
				int texWidth = m_Tex.width;
				int texHeight = m_Tex.height;
				int xMin = Mathf.Min(m_CroppingRect.xMin, 0);
				int xMax = Mathf.Max(m_CroppingRect.xMax, texWidth);
				int yMin = Mathf.Min(m_CroppingRect.yMin, 0);
				int yMax = Mathf.Max(m_CroppingRect.yMax, texHeight);
				float contentWidth = xMax - xMin;
				float contentHeight = yMax - yMin;
				m_Scale = Mathf.Min((m_CanvasRect.width - CANVAS_BORDER_ZOOMED_THICKNESS - CANVAS_BORDER_ZOOMED_THICKNESS) / contentWidth,
						(m_CanvasRect.height - CANVAS_BORDER_ZOOMED_THICKNESS - CANVAS_BORDER_ZOOMED_THICKNESS) / contentHeight);
				m_ContentX = (m_CanvasRect.width - contentWidth * m_Scale) * 0.5F;
				m_ContentY = (m_CanvasRect.height - contentHeight * m_Scale) * 0.5F;
			}
		}
		EditorGUILayout.EndHorizontal();
	}

	private void DrawSaveField() {
		if (GUILayout.Button("保存", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2 + 4F))) {
			int croppingWidth = m_CroppingRect.width;
			int croppingHeight = m_CroppingRect.height;
			if (croppingWidth <= 0 || croppingHeight <= 0) {
				ShowNotification(EditorGUIUtility.TrTextContent("剪裁区域不能为空！"), 1);
				return;
			}
			string srcFilePath = AssetDatabase.GetAssetPath(m_Tex);
			string directory = File.Exists(srcFilePath) ? srcFilePath[..srcFilePath.LastIndexOfAny(new[] {'/', '\\'})] : "Assets";
			string filePath = EditorUtility.SaveFilePanel("保存裁剪后的图像", directory, m_Tex.name + "_New", "png");
			if (!string.IsNullOrEmpty(filePath)) {
				if (!m_IsPreview) {
					UpdatePreviewTex(true);
				}
				RenderTexture prevRT = RenderTexture.active;
				RenderTexture.active = m_PreviewTex;
				Texture2D tex = new Texture2D(croppingWidth, croppingHeight, TextureFormat.RGBA32, false);
				tex.ReadPixels(new Rect(0, 0, croppingWidth, croppingHeight), 0, 0);
				RenderTexture.active = prevRT;
				if (!m_IsPreview) {
					UpdatePreviewTex();
				}
				
				byte[] bytes = tex.EncodeToPNG();
				File.WriteAllBytes(filePath, bytes);
				AssetDatabase.Refresh();
			}
		}
	}

	#endregion

	#region DrawCanvasField

	private void DrawCanvas(Rect canvasRect, float scaledContentWidth, float scaledContentHeight) {
		GUI.BeginClip(canvasRect);
		bool isShrink = scaledContentWidth <= m_CanvasRect.width && scaledContentHeight <= m_CanvasRect.height;
		if (m_ResizeType == ResizeType.NONE) {
			if (isShrink) {
				m_ContentX = (m_CanvasRect.width - scaledContentWidth) * 0.5F;
				m_ContentY = (m_CanvasRect.height - scaledContentHeight) * 0.5F;
			} else {
				float blankWidth = m_CanvasRect.width - BLANK_DELTA_WITH_CANVAS;
				float blankHeight = m_CanvasRect.height - BLANK_DELTA_WITH_CANVAS;
				if (m_ContentX > blankWidth) {
					m_ContentX = blankWidth;
				} else if (m_ContentX < BLANK_DELTA_WITH_CANVAS - scaledContentWidth) {
					m_ContentX = BLANK_DELTA_WITH_CANVAS - scaledContentWidth;
				}
				if (m_ContentY > blankHeight) {
					m_ContentY = blankHeight;
				} else if (m_ContentY < BLANK_DELTA_WITH_CANVAS - scaledContentHeight) {
					m_ContentY = BLANK_DELTA_WITH_CANVAS - scaledContentHeight;
				}
			}
		}
		Rect texRect = new Rect(
				m_ContentX - Mathf.Min(m_CroppingRect.xMin, 0) * m_Scale,
				m_ContentY - Mathf.Min(m_Tex.height - m_CroppingRect.yMax, 0) * m_Scale,
				m_Tex.width * m_Scale,
				m_Tex.height * m_Scale
		);
		Rect croppingRect = new Rect(
				m_ContentX + Mathf.Max(m_CroppingRect.xMin, 0) * m_Scale,
				m_ContentY + Mathf.Max(m_Tex.height - m_CroppingRect.yMax, 0) * m_Scale,
				m_CroppingRect.width * m_Scale,
				m_CroppingRect.height * m_Scale
		);
		EditorGUI.DrawTextureTransparent(texRect, m_Tex);
		if (m_IsPreview) {
			EditorGUI.DrawRect(texRect, PREVIEW_MASK_COLOR);
		}
		if (m_PreviewTex) {
			GUI.DrawTexture(croppingRect, m_PreviewTex);
		}
		DrawCroppingRect(croppingRect);
		GUI.EndClip();
	}

	private const float DRAGGABLE_EXT_THICKNESS = 5F;	// 拖动响应范围往外扩展距离
	private const float CROPPING_RECT_BOLD_THICKNESS = 3F;	// 粗线宽度
	private const float CROPPING_RECT_BOLD_LENGTH = 10F;	// 粗线长度
	private void DrawCroppingRect(Rect rect) {
		m_ResizeRects.Clear();

		float boldLengthH = Mathf.Min(CROPPING_RECT_BOLD_LENGTH, rect.width * 0.5F);
		float boldLengthV = Mathf.Min(CROPPING_RECT_BOLD_LENGTH, rect.height * 0.5F);

		#region 边框
		Rect topLineRect = new Rect(rect.x + boldLengthH, rect.y, rect.width - boldLengthH - boldLengthH, 1);
		EditorGUI.DrawRect(topLineRect, Color.cyan);
		topLineRect.y -= DRAGGABLE_EXT_THICKNESS;
		topLineRect.height += DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((topLineRect, ResizeType.TOP));

		Rect bottomLineRect = new Rect(rect.x + boldLengthH, rect.yMax - 1, rect.width - boldLengthH - boldLengthH, 1);
		EditorGUI.DrawRect(bottomLineRect, Color.cyan);
		bottomLineRect.height += DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((bottomLineRect, ResizeType.BOTTOM));

		Rect leftLineRect = new Rect(rect.x, rect.y + boldLengthV, 1, rect.height - boldLengthV - boldLengthV);
		EditorGUI.DrawRect(leftLineRect, Color.cyan);
		leftLineRect.x -= DRAGGABLE_EXT_THICKNESS;
		leftLineRect.width += DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((leftLineRect, ResizeType.LEFT));

		Rect rightLineRect = new Rect(rect.xMax - 1, rect.y + boldLengthV, 1, rect.height - boldLengthV - boldLengthV);
		EditorGUI.DrawRect(rightLineRect, Color.cyan);
		rightLineRect.width += DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((rightLineRect, ResizeType.RIGHT));
		#endregion

		#region 12条加粗线段
		Rect topLeftRect = new Rect(rect.x, rect.y, boldLengthH, CROPPING_RECT_BOLD_THICKNESS);
		EditorGUI.DrawRect(topLeftRect, Color.cyan);
		topLeftRect.x -= DRAGGABLE_EXT_THICKNESS;
		topLeftRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		topLeftRect.y -= DRAGGABLE_EXT_THICKNESS;
		topLeftRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((topLeftRect, ResizeType.TOP_LEFT));

		Rect topRightRect = new Rect(rect.xMax - boldLengthH, rect.y, boldLengthH, CROPPING_RECT_BOLD_THICKNESS);
		EditorGUI.DrawRect(topRightRect, Color.cyan);
		topRightRect.x -= DRAGGABLE_EXT_THICKNESS;
		topRightRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		topRightRect.y -= DRAGGABLE_EXT_THICKNESS;
		topRightRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((topRightRect, ResizeType.TOP_RIGHT));

		Rect topCenterRect = new Rect(rect.x + (rect.width - boldLengthH) / 2, rect.y, boldLengthH, CROPPING_RECT_BOLD_THICKNESS);
		EditorGUI.DrawRect(topCenterRect, Color.cyan);
		topCenterRect.y -= DRAGGABLE_EXT_THICKNESS;
		topCenterRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((topCenterRect, ResizeType.TOP));

		Rect bottomLeftRect = new Rect(rect.x, rect.yMax - CROPPING_RECT_BOLD_THICKNESS, boldLengthH, CROPPING_RECT_BOLD_THICKNESS);
		EditorGUI.DrawRect(bottomLeftRect, Color.cyan);
		bottomLeftRect.x -= DRAGGABLE_EXT_THICKNESS;
		bottomLeftRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		bottomLeftRect.y -= DRAGGABLE_EXT_THICKNESS;
		bottomLeftRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((bottomLeftRect, ResizeType.BOTTOM_LEFT));

		Rect bottomRightRect = new Rect(rect.xMax - boldLengthH, rect.yMax - CROPPING_RECT_BOLD_THICKNESS, boldLengthH, CROPPING_RECT_BOLD_THICKNESS);
		EditorGUI.DrawRect(bottomRightRect, Color.cyan);
		bottomRightRect.x -= DRAGGABLE_EXT_THICKNESS;
		bottomRightRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		bottomRightRect.y -= DRAGGABLE_EXT_THICKNESS;
		bottomRightRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((bottomRightRect, ResizeType.BOTTOM_RIGHT));

		Rect bottomCenterRect = new Rect(rect.x + (rect.width - boldLengthH) / 2, rect.yMax - CROPPING_RECT_BOLD_THICKNESS, boldLengthH, CROPPING_RECT_BOLD_THICKNESS);
		EditorGUI.DrawRect(bottomCenterRect, Color.cyan);
		bottomCenterRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((bottomCenterRect, ResizeType.BOTTOM));

		Rect leftTopRect = new Rect(rect.x, rect.y, CROPPING_RECT_BOLD_THICKNESS, boldLengthV);
		EditorGUI.DrawRect(leftTopRect, Color.cyan);
		leftTopRect.x -= DRAGGABLE_EXT_THICKNESS;
		leftTopRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		leftTopRect.y -= DRAGGABLE_EXT_THICKNESS;
		leftTopRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((leftTopRect, ResizeType.TOP_LEFT));

		Rect leftBottomRect = new Rect(rect.x, rect.yMax - boldLengthV, CROPPING_RECT_BOLD_THICKNESS, boldLengthV);
		EditorGUI.DrawRect(leftBottomRect, Color.cyan);
		leftBottomRect.x -= DRAGGABLE_EXT_THICKNESS;
		leftBottomRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		leftBottomRect.y -= DRAGGABLE_EXT_THICKNESS;
		leftBottomRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((leftBottomRect, ResizeType.BOTTOM_LEFT));

		Rect leftCenterRect = new Rect(rect.x, rect.y + (rect.height - boldLengthV) / 2, CROPPING_RECT_BOLD_THICKNESS, boldLengthV);
		EditorGUI.DrawRect(leftCenterRect, Color.cyan);
		leftCenterRect.x -= DRAGGABLE_EXT_THICKNESS;
		leftCenterRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((leftCenterRect, ResizeType.LEFT));

		Rect rightTopRect = new Rect(rect.xMax - CROPPING_RECT_BOLD_THICKNESS, rect.y, CROPPING_RECT_BOLD_THICKNESS, boldLengthV);
		EditorGUI.DrawRect(rightTopRect, Color.cyan);
		rightTopRect.x -= DRAGGABLE_EXT_THICKNESS;
		rightTopRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		rightTopRect.y -= DRAGGABLE_EXT_THICKNESS;
		rightTopRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((rightTopRect, ResizeType.TOP_RIGHT));

		Rect rightBottomRect = new Rect(rect.xMax - CROPPING_RECT_BOLD_THICKNESS, rect.yMax - boldLengthV, CROPPING_RECT_BOLD_THICKNESS, boldLengthV);
		EditorGUI.DrawRect(rightBottomRect, Color.cyan);
		rightBottomRect.x -= DRAGGABLE_EXT_THICKNESS;
		rightBottomRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		rightBottomRect.y -= DRAGGABLE_EXT_THICKNESS;
		rightBottomRect.height += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((rightBottomRect, ResizeType.BOTTOM_RIGHT));

		Rect rightCenterRect = new Rect(rect.xMax - CROPPING_RECT_BOLD_THICKNESS, rect.y + (rect.height - boldLengthV) / 2, CROPPING_RECT_BOLD_THICKNESS, boldLengthV);
		EditorGUI.DrawRect(rightCenterRect, Color.cyan);
		rightCenterRect.x -= DRAGGABLE_EXT_THICKNESS;
		rightCenterRect.width += DRAGGABLE_EXT_THICKNESS + DRAGGABLE_EXT_THICKNESS;
		m_ResizeRects.Add((rightCenterRect, ResizeType.RIGHT));
		#endregion

		switch (m_ResizeType) {
			case ResizeType.NONE:
				break;
		}
	}

	private void DrawScrollBar(Rect horizontalScrollBarRect, Rect verticalScrollBarRect, float scaledContentWidth, float scaledContentHeight) {
		if (m_ContentX < 0 || m_ContentY < 0 || m_ContentX + scaledContentWidth > m_CanvasRect.width || m_ContentY + scaledContentHeight > m_CanvasRect.height) {
			float blankWidth = m_CanvasRect.width - BLANK_DELTA_WITH_CANVAS;
			float blankHeight = m_CanvasRect.height - BLANK_DELTA_WITH_CANVAS;
			float totalWidth = blankWidth + scaledContentWidth + blankWidth;
			float hBarSize = m_CanvasRect.width / totalWidth;
			float hBarPos = (blankWidth - m_ContentX) / totalWidth;
			EditorGUI.BeginChangeCheck();
			float newHBarPos = GUI.HorizontalScrollbar(horizontalScrollBarRect, hBarPos * SCROLL_BAR_PRECISION, hBarSize * SCROLL_BAR_PRECISION, 0F, SCROLL_BAR_PRECISION) / SCROLL_BAR_PRECISION;
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(this, "ImageCropping.Scroll");
				m_ContentX = blankWidth - newHBarPos * totalWidth;
			}
			float totalHeight = blankHeight + scaledContentHeight + blankHeight;
			float vBarSize = m_CanvasRect.height / totalHeight;
			float vBarPos = (blankHeight - m_ContentY) / totalHeight;
			EditorGUI.BeginChangeCheck();
			float newVBarPos = GUI.VerticalScrollbar(verticalScrollBarRect, vBarPos * SCROLL_BAR_PRECISION, vBarSize * SCROLL_BAR_PRECISION, 0F, SCROLL_BAR_PRECISION) / SCROLL_BAR_PRECISION;
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(this, "ImageCropping.Scroll");
				m_ContentY = blankHeight - newVBarPos * totalHeight;
			}
		}
	}

	private void DrawScaleLabel(Rect scaleLabelRect) {
		EditorGUI.LabelField(scaleLabelRect, $"{Mathf.Round(m_Scale * 10000) / 100}%", (GUIStyle) "CenteredLabel");
	}

	private void SetCursorRect() {
		switch (m_ResizeType) {
			case ResizeType.TOP_LEFT:
			case ResizeType.BOTTOM_RIGHT:
				EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeUpLeft);
				break;
			case ResizeType.TOP_RIGHT:
			case ResizeType.BOTTOM_LEFT:
				EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeUpRight);
				break;
			case ResizeType.TOP:
			case ResizeType.BOTTOM:
				EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeVertical);
				break;
			case ResizeType.LEFT:
			case ResizeType.RIGHT:
				EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeHorizontal);
				break;
			case ResizeType.CENTER:
				EditorGUIUtility.AddCursorRect(position, MouseCursor.Pan);
				break;
			case ResizeType.NONE:
				foreach ((Rect resizeRect, ResizeType type) element in m_ResizeRects) {
					Rect resizeRect = element.resizeRect;
					resizeRect.x += m_CanvasRect.x;
					resizeRect.y += m_CanvasRect.y;
					switch (element.type) {
						case ResizeType.TOP_LEFT:
						case ResizeType.BOTTOM_RIGHT:
							EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeUpLeft);
							break;
						case ResizeType.TOP_RIGHT:
						case ResizeType.BOTTOM_LEFT:
							EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeUpRight);
							break;
						case ResizeType.TOP:
						case ResizeType.BOTTOM:
							EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeVertical);
							break;
						case ResizeType.LEFT:
						case ResizeType.RIGHT:
							EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);
							break;
					}
				}
				break;
		}
	}

	private void HandleMouseEvent(Rect scrollWheelRect) {
		switch (Event.current.type) {
			case EventType.ScrollWheel: {
				Vector2 mousePos = Event.current.mousePosition;
				if (mousePos.x >= scrollWheelRect.x && mousePos.x <= scrollWheelRect.xMax && mousePos.y >= scrollWheelRect.y && mousePos.y <= scrollWheelRect.yMax) {
					mousePos.x -= m_CanvasRect.x;
					mousePos.y -= m_CanvasRect.y;
					float newScale = Mathf.Clamp(m_Scale * (1 - Event.current.delta.y * 0.02F), 1F / Mathf.Min(m_Tex.width, m_Tex.height), SCALE_MAX);
					m_ContentX = mousePos.x - (mousePos.x - m_ContentX) / m_Scale * newScale;
					m_ContentY = mousePos.y - (mousePos.y - m_ContentY) / m_Scale * newScale;
					m_Scale = newScale;
					Repaint();
				}
				break;
			}
			case EventType.MouseDown: {
				Vector2 mousePos = Event.current.mousePosition;
				if (mousePos.x >= m_CanvasRect.x && mousePos.x <= m_CanvasRect.xMax && mousePos.y >= m_CanvasRect.y && mousePos.y <= m_CanvasRect.yMax) {
					mousePos.x -= m_CanvasRect.x;
					mousePos.y -= m_CanvasRect.y;
					bool isResize = false;
					foreach ((Rect resizeRect, ResizeType type) in m_ResizeRects) {
						if (resizeRect.Contains(mousePos)) {
							isResize = true;
							m_ResizeType = type;
							m_DragPrevPos = mousePos;
							break;
						}
					}
					if (!isResize) {
						m_ResizeType = ResizeType.CENTER;
						m_DragPrevPos = mousePos;
					}
				}
				break;
			}
			case EventType.MouseDrag: {
				if (m_ResizeType != ResizeType.NONE) {
					Vector2 mousePos = Event.current.mousePosition;
					mousePos.x -= m_CanvasRect.x;
					mousePos.y -= m_CanvasRect.y;
					float deltaX = mousePos.x - m_DragPrevPos.x;
					float deltaY = mousePos.y - m_DragPrevPos.y;
					switch (m_ResizeType) {
						case ResizeType.TOP_LEFT:
						case ResizeType.LEFT:
						case ResizeType.BOTTOM_LEFT: {
							int croppingXMin = m_CroppingRect.xMin;
							int newCroppingXMin = Mathf.RoundToInt(croppingXMin + deltaX / m_Scale);
							mousePos.x = m_DragPrevPos.x + (newCroppingXMin - croppingXMin) * m_Scale;

							int overflowValue = 0;
							int croppingXMax = m_CroppingRect.xMax;
							if (newCroppingXMin > croppingXMax) {
								overflowValue = newCroppingXMin - croppingXMax;
								newCroppingXMin = croppingXMax;
							}
							m_CroppingRect.xMin = newCroppingXMin;
							if (croppingXMin < 0 || newCroppingXMin < 0) {
								m_ContentX += (Mathf.Min(newCroppingXMin, 0) - Mathf.Min(croppingXMin, 0)) * m_Scale;
							}

							if (overflowValue != 0) {
								newCroppingXMin += overflowValue;
								m_CroppingRect.xMax = newCroppingXMin;
								m_ResizeType += ResizeType.RIGHT - ResizeType.LEFT;
							}
							break;
						}
						case ResizeType.TOP_RIGHT:
						case ResizeType.RIGHT:
						case ResizeType.BOTTOM_RIGHT: {
							int croppingXMax = m_CroppingRect.xMax;
							int newCroppingXMax = Mathf.RoundToInt(croppingXMax + deltaX / m_Scale);
							mousePos.x = m_DragPrevPos.x + (newCroppingXMax - croppingXMax) * m_Scale;

							int overflowValue = 0;
							int croppingXMin = m_CroppingRect.xMin;
							if (newCroppingXMax < croppingXMin) {
								overflowValue = newCroppingXMax - croppingXMin;
								newCroppingXMax = croppingXMin;
							}
							m_CroppingRect.xMax = newCroppingXMax;

							if (overflowValue != 0) {
								newCroppingXMax += overflowValue;
								m_CroppingRect.xMin = newCroppingXMax;
								m_ResizeType += ResizeType.LEFT - ResizeType.RIGHT;
								if (croppingXMin < 0 || newCroppingXMax < 0) {
									m_ContentX += (Mathf.Min(newCroppingXMax, 0) - Mathf.Min(croppingXMin, 0)) * m_Scale;
								}
							}
							break;
						}
					}
					switch (m_ResizeType) {
						case ResizeType.TOP_LEFT:
						case ResizeType.TOP:
						case ResizeType.TOP_RIGHT: {
							int croppingYMax = m_CroppingRect.yMax;
							int newCroppingYMax = Mathf.RoundToInt(croppingYMax + -deltaY / m_Scale);
							mousePos.y = m_DragPrevPos.y + -(newCroppingYMax - croppingYMax) * m_Scale;

							int overflowValue = 0;
							int croppingYMin = m_CroppingRect.yMin;
							if (newCroppingYMax < croppingYMin) {
								overflowValue = newCroppingYMax - croppingYMin;
								newCroppingYMax = croppingYMin;
							}
							m_CroppingRect.yMax = newCroppingYMax;
							int texHeight = m_Tex.height;
							if (croppingYMax > texHeight || newCroppingYMax > texHeight) {
								m_ContentY -= (Mathf.Max(newCroppingYMax, texHeight) - Mathf.Max(croppingYMax, texHeight)) * m_Scale;
							}

							if (overflowValue != 0) {
								newCroppingYMax += overflowValue;
								m_CroppingRect.yMin = newCroppingYMax;
								m_ResizeType += ResizeType.BOTTOM - ResizeType.TOP;
							}
							break;
						}
						case ResizeType.BOTTOM_LEFT:
						case ResizeType.BOTTOM:
						case ResizeType.BOTTOM_RIGHT: {
							int croppingYMin = m_CroppingRect.yMin;
							int newCroppingYMin = Mathf.RoundToInt(croppingYMin + -deltaY / m_Scale);
							mousePos.y = m_DragPrevPos.y + -(newCroppingYMin - croppingYMin) * m_Scale;

							int overflowValue = 0;
							int croppingYMax = m_CroppingRect.yMax;
							if (newCroppingYMin > croppingYMax) {
								overflowValue = newCroppingYMin - croppingYMax;
								newCroppingYMin = croppingYMax;
							}
							m_CroppingRect.yMin = newCroppingYMin;

							if (overflowValue != 0) {
								newCroppingYMin += overflowValue;
								m_CroppingRect.yMax = newCroppingYMin;
								m_ResizeType += ResizeType.TOP - ResizeType.BOTTOM;
								int texHeight = m_Tex.height;
								if (croppingYMax > texHeight || newCroppingYMin > texHeight) {
									m_ContentY -= (Mathf.Max(newCroppingYMin, texHeight) - Mathf.Max(croppingYMax, texHeight)) * m_Scale;
								}
							}
							break;
						}
					}
					if (m_ResizeType is ResizeType.CENTER) {
						m_ContentX += deltaX;
						m_ContentY += deltaY;
					}
					m_DragPrevPos = mousePos;
					UpdatePreviewTex();
					Repaint();
				}
				break;
			}
			case EventType.MouseUp:
			case EventType.Ignore: {
				if (m_ResizeType != ResizeType.NONE) {
					m_ResizeType = ResizeType.NONE;
					Repaint();
				}
				break;
			}
		}
	}

	#endregion

	private float deltaScrollX;
	private float deltaScrollY;

	private void HandleAutoScroll(float deltaTime) {
		if (m_ResizeType != ResizeType.NONE) {
			bool beyondLeft = m_DragPrevPos.x < 0;
			bool beyondRight = m_DragPrevPos.x > m_CanvasRect.width;
			if (beyondLeft || beyondRight) {
				float speed = beyondLeft ? -AUTO_SCROLL_SPEED : AUTO_SCROLL_SPEED;
				float scrollX = speed * deltaTime + deltaScrollX;
				int deltaCroppingX = Mathf.RoundToInt(scrollX / m_Scale);
				float roundedScrollX = deltaCroppingX * m_Scale;
				deltaScrollX = scrollX - roundedScrollX;
				HandleAutoScrollHorizontal(deltaCroppingX);
			}
			bool beyondTop = m_DragPrevPos.y < 0;
			bool beyondBottom = m_DragPrevPos.y > m_CanvasRect.height;
			if (beyondTop || beyondBottom) {
				float speed = beyondTop ? -AUTO_SCROLL_SPEED : AUTO_SCROLL_SPEED;
				float scrollY = speed * deltaTime + deltaScrollY;
				int deltaCroppingY = -Mathf.RoundToInt(scrollY / m_Scale);
				float roundedScrollY = -deltaCroppingY * m_Scale;
				deltaScrollY = scrollY - roundedScrollY;
				HandleAutoScrollVertical(deltaCroppingY);
			}
			if (beyondLeft || beyondRight || beyondTop || beyondBottom) {
				UpdatePreviewTex();
				Repaint();
			}
		} else {
			deltaScrollX = 0;
			deltaScrollY = 0;
		}
	}
	private void HandleAutoScrollHorizontal(int deltaCroppingX) {
		switch (m_ResizeType) {
			case ResizeType.TOP_LEFT:
			case ResizeType.LEFT:
			case ResizeType.BOTTOM_LEFT: {
				int croppingXMin = m_CroppingRect.xMin;
				int newCroppingXMin = croppingXMin + deltaCroppingX;

				int overflowValue = 0;
				int croppingXMax = m_CroppingRect.xMax;
				if (newCroppingXMin > croppingXMax) {
					overflowValue = newCroppingXMin - croppingXMax;
					newCroppingXMin = croppingXMax;
				}
				m_CroppingRect.xMin = newCroppingXMin;
				if (croppingXMin > 0 || newCroppingXMin > 0) {
					m_ContentX -= (Mathf.Max(newCroppingXMin, 0) - Mathf.Max(croppingXMin, 0)) * m_Scale;
				}

				if (overflowValue != 0) {
					m_CroppingRect.xMax += overflowValue;
					m_ResizeType += ResizeType.RIGHT - ResizeType.LEFT;
					m_ContentX -= overflowValue * m_Scale;
				}
				break;
			}
			case ResizeType.TOP_RIGHT:
			case ResizeType.RIGHT:
			case ResizeType.BOTTOM_RIGHT: {
				int croppingXMax = m_CroppingRect.xMax;
				int newCroppingXMax = croppingXMax + deltaCroppingX;

				int overflowValue = 0;
				int croppingXMin = m_CroppingRect.xMin;
				if (newCroppingXMax < croppingXMin) {
					overflowValue = newCroppingXMax - croppingXMin;
					newCroppingXMax = croppingXMin;
				}
				m_CroppingRect.xMax = newCroppingXMax;
				m_ContentX -= (newCroppingXMax - croppingXMax) * m_Scale;

				if (overflowValue != 0) {
					newCroppingXMax += overflowValue;
					m_CroppingRect.xMin = newCroppingXMax;
					m_ResizeType += ResizeType.LEFT - ResizeType.RIGHT;
					if (croppingXMin > 0 || newCroppingXMax > 0) {
						m_ContentX -= (Mathf.Max(newCroppingXMax, 0) - Mathf.Max(croppingXMin, 0)) * m_Scale;
					}
				}
				break;
			}
		}
	}
	private void HandleAutoScrollVertical(int deltaCroppingY) {
		switch (m_ResizeType) {
			case ResizeType.TOP_LEFT:
			case ResizeType.TOP:
			case ResizeType.TOP_RIGHT: {
				int croppingYMax = m_CroppingRect.yMax;
				int newCroppingYMax = croppingYMax + deltaCroppingY;

				int overflowValue = 0;
				int croppingYMin = m_CroppingRect.yMin;
				if (newCroppingYMax < croppingYMin) {
					overflowValue = newCroppingYMax - croppingYMin;
					newCroppingYMax = croppingYMin;
				}
				m_CroppingRect.yMax = newCroppingYMax;
				int texHeight = m_Tex.height;
				if (croppingYMax < texHeight || newCroppingYMax < texHeight) {
					m_ContentY += (Mathf.Max(newCroppingYMax, 0) - Mathf.Max(croppingYMax, 0)) * m_Scale;
				}

				if (overflowValue != 0) {
					m_CroppingRect.yMin += overflowValue;
					m_ResizeType += ResizeType.BOTTOM - ResizeType.TOP;
					m_ContentY += overflowValue * m_Scale;
				}
				break;
			}
			case ResizeType.BOTTOM_LEFT:
			case ResizeType.BOTTOM:
			case ResizeType.BOTTOM_RIGHT: {
				int croppingYMin = m_CroppingRect.yMin;
				int newCroppingYMin = croppingYMin + deltaCroppingY;

				int overflowValue = 0;
				int croppingYMax = m_CroppingRect.yMax;
				if (newCroppingYMin > croppingYMax) {
					overflowValue = newCroppingYMin - croppingYMax;
					newCroppingYMin = croppingYMax;
				}
				m_CroppingRect.yMin = newCroppingYMin;
				m_ContentY += (newCroppingYMin - croppingYMin) * m_Scale;

				if (overflowValue != 0) {
					newCroppingYMin += overflowValue;
					m_CroppingRect.yMax = newCroppingYMin;
					m_ResizeType += ResizeType.TOP - ResizeType.BOTTOM;
					int texHeight = m_Tex.height;
					if (croppingYMax < texHeight || newCroppingYMin < texHeight) {
						m_ContentY += (Mathf.Max(newCroppingYMin, 0) - Mathf.Max(croppingYMax, 0)) * m_Scale;
					}
				}
				break;
			}
		}
	}

	private static readonly int MODE = Shader.PropertyToID("_Mode");
	private static readonly int SCALE_AND_OFFSET = Shader.PropertyToID("_ScaleAndOffset");
	private static readonly int CORNER_MODE = Shader.PropertyToID("_CornerMode");
	private static readonly int ROUND_RADIUS_X = Shader.PropertyToID("_RoundRadiusX");
	private static readonly int ROUND_RADIUS_Y = Shader.PropertyToID("_RoundRadiusY");
	private static readonly int HYPER_ELLIPTIC_POWER = Shader.PropertyToID("_HyperEllipticPower");
	private static readonly int SOFTNESS = Shader.PropertyToID("_Softness");
	private static readonly int EDGE_MOVE = Shader.PropertyToID("_EdgeMove");
	private void UpdatePreviewTex(bool forcePreview = false) {
		int width = m_CroppingRect.width;
		int height = m_CroppingRect.height;
		if (!m_PreviewTex || m_PreviewTex.width != width || m_PreviewTex.height != height) {
			m_PreviewTex = RenderTexture.GetTemporary(width, height);
			m_PreviewTex.filterMode = m_Tex.filterMode;
		}
		Graphics.Blit(m_BlankTex, m_PreviewTex);

		if (m_IsPreview || forcePreview) {
			m_Mat.SetInt(MODE, 1);
			m_Mat.color = Color.white;
			m_Mat.mainTexture = m_Tex;
			m_Mat.SetVector(SCALE_AND_OFFSET, m_Tex ? new Vector4(
					(float) width / m_Tex.width,
					(float) height / m_Tex.height,
					(float) m_CroppingRect.x / m_Tex.width,
					(float) m_CroppingRect.y / m_Tex.height
			) : new Vector4(1, 1, 0, 0));
		} else {
			m_Mat.SetInt(MODE, 0);
			m_Mat.color = CROPPING_CORNERED_COLOR;
		}
		m_Mat.SetInt(CORNER_MODE, (int) m_CornerType);
		m_Mat.SetFloat(ROUND_RADIUS_X, m_RoundRadius / width);
		m_Mat.SetFloat(ROUND_RADIUS_Y, m_RoundRadius / height);
		m_Mat.SetFloat(HYPER_ELLIPTIC_POWER, m_HyperEllipticPower);
		m_Mat.SetFloat(SOFTNESS, Mathf.Clamp01((m_Softness + m_Softness) / (width + height)));
		m_Mat.SetFloat(EDGE_MOVE, Mathf.Clamp((m_EdgeMove + m_EdgeMove) / (width + height), -1, 1));
		
		Graphics.Blit(null, m_PreviewTex, m_Mat);
	}

	private static Color[] GetTexturePixels(Texture2D tex) {
		if (!tex.isReadable) {
			RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0);
			Graphics.Blit(tex, rt);

			RenderTexture prevRT = RenderTexture.active;
			RenderTexture.active = rt;
			Texture2D tempTex = new Texture2D(tex.width, tex.height, tex.format, false);
			tempTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
			tempTex.Apply();
			RenderTexture.active = prevRT;
			
			tex = tempTex;
		}
		return tex.GetPixels();
	}

	public static string GetEnumInspectorName(object enumValue) {
		Type enumType = enumValue.GetType();
		string enumName = Enum.GetName(enumType, enumValue);
		FieldInfo field = enumType.GetField(enumName);
		object[] attrs = field.GetCustomAttributes(false);
		foreach (var attr in attrs) {
			if (attr is InspectorNameAttribute inspectorName) {
				enumName = inspectorName.displayName;
				break;
			}
		}
		return enumName;
	}
}
