/*
 * @Author: wangyun
 * @CreateTime: 2022-05-02 01:13:30 495
 * @LastEditor: wangyun
 * @EditTime: 2023-08-14 21:21:57 496
 */

#if UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using UObject = UnityEngine.Object;

public class UITools {
#if UNITY_2021_2_OR_NEWER
	// [Overlay(typeof(SceneView), "UITools", true)]
	// public class UIToolsOverlay : Overlay {
	// 	private IMGUIContainer m_Container;
	// 	private Label m_LblTitle;
	// 	
	// 	public string displayTitle {
	// 		get => m_LblTitle?.text;
	// 		set {
	// 			if (m_LblTitle != null) {
	// 				m_LblTitle.text = value;
	// 				m_LblTitle.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
	// 			}
	// 		}
	// 	}
	// 	public override VisualElement CreatePanelContent() {
	// 		m_Container = new IMGUIContainer();
	// 		m_Container.onGUIHandler = () => {
	// 			Instance.DrawButtons(m_Container.contentRect.height < 100F);
	// 		};
	// 		m_Container.RegisterCallback<AttachToPanelEvent, UIToolsOverlay>((evt, overlay) => {
	// 			if (!overlay.collapsed && evt.target is VisualElement _ve) {
	// 				var tit = _ve.parent.parent.Q<Label>("overlay-header__title");
	// 				overlay.m_LblTitle = tit;
	// 				overlay.m_LblTitle.parent.style.justifyContent = Justify.Center;
	// 				displayTitle = "";
	// 				if (containerWindow is SceneView sceneView) {
	// 					OnModeChange2D(sceneView.in2DMode);
	// 				}
	// 			} else {
	// 				overlay.m_LblTitle = null;
	// 			}
	// 		}, this);
	// 		return m_Container;
	// 	}
	//
	// 	public override void OnCreated() {
	// 		if (containerWindow is SceneView sceneView) {
	// 			// sceneView.modeChanged2D += OnModeChange2D;
	// 			FieldInfo fi = typeof(SceneView).GetField("modeChanged2D", BindingFlags.Instance | BindingFlags.NonPublic);
	// 			if (fi != null && fi.FieldType == typeof(Action<bool>)) {
	// 				if (fi.GetValue(sceneView) is Action<bool> modeChanged2D) {
	// 					modeChanged2D += OnModeChange2D;
	// 				} else {
	// 					modeChanged2D = OnModeChange2D;
	// 				}
	// 				fi.SetValue(sceneView, modeChanged2D);
	// 			}
	// 			displayed = true;
	// 		}
	// 	}
	//
	// 	public override void OnWillBeDestroyed() {
	// 		if (containerWindow is SceneView sceneView) {
	// 			// sceneView.modeChanged2D -= OnModeChange2D;
	// 			FieldInfo fi = typeof(SceneView).GetField("modeChanged2D", BindingFlags.Instance | BindingFlags.NonPublic);
	// 			if (fi != null && fi.GetValue(sceneView) is Action<bool> modeChanged2D) {
	// 				modeChanged2D -= OnModeChange2D;
	// 				fi.SetValue(sceneView, modeChanged2D);
	// 			}
	// 		}
	// 	}
	//
	// 	private void OnModeChange2D(bool in2DMode) {
	// 		if (m_Container != null && displayed) {
	// 			m_Container.parent.parent.style.display = in2DMode ? DisplayStyle.Flex : DisplayStyle.None;
	// 		}
	// 	}
	// }
	
	[Overlay(typeof(SceneView), "UITools", true)]
	public class UIToolsOverlay : ToolbarOverlay {
		public UIToolsOverlay() : base("UITools/Tools") {
		}
	}

	[EditorToolbarElement("UITools/Tools", typeof(SceneView))]
	public class UIToolsStrip : VisualElement {
		public UIToolsStrip() {
			IMGUIContainer container = new IMGUIContainer();
			container.style.marginLeft = 2;
			container.style.marginTop = -1;
			container.onGUIHandler = () => {
				StyleEnum<FlexDirection> direction = container.parent.parent.resolvedStyle.flexDirection;
				bool isHorizontal = direction == FlexDirection.Row || direction == FlexDirection.RowReverse;
				Instance.DrawButtons(isHorizontal);
			};
			Add(container);
		}
	}
#else
	public class UIToolsWindow : EditorWindow {
		[MenuItem("Tools/UITools")]
		private static void Init() {
			UIToolsWindow window = CreateWindow<UIToolsWindow>("UITools");
			window.minSize = new Vector2(30F, 22F);
			window.Show();
		}
		private void OnGUI() {
			Instance.DrawButtons(position.height < 100F);
		}
	}
#endif

	private static UITools s_Instance;
	// ReSharper disable once ConvertToNullCoalescingCompoundAssignment
	public static UITools Instance => s_Instance ?? (s_Instance = new UITools {
		alignLeft = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "align_left.png"),
		alignMiddle = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "align_middle.png"),
		alignRight = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "align_right.png"),
		alignTop = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "align_top.png"),
		alignCenter = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "align_center.png"),
		alignBottom = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "align_bottom.png"),
		sameWidth = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "same_width.png"),
		sameHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "same_height.png"),
		fitWidth = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "fit_width.png"),
		fitHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "fit_height.png"),
		groupPack = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "group_pack.png"),
		groupUnpack = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "group_unpack.png"),
		averageGapH = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "average_gap_h.png"),
		averageGapV = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOT_PATH + "average_gap_v.png"),
	});

	private static string m_ROOT_PATH = "Assets/Editor/UITools/Res/";
	private static string ROOT_PATH {
		get {
			string[] guids = AssetDatabase.FindAssets("t:Texture2D align_left");
			if (guids.Length > 0) {
				foreach (var guid in guids) {
					string path = AssetDatabase.GUIDToAssetPath(guid);
					if (path.IndexOf("Res/") > -1) {
						m_ROOT_PATH = path.Substring(0, path.LastIndexOf('/') + 1);
						break;
					}
				}
			}
			return m_ROOT_PATH;
		}
	}

	private static GUIStyle s_BtnStyle;
	private static readonly GUILayoutOption[] s_BtnVerticalOptions = {
		GUILayout.MinWidth(36F),
		GUILayout.Height(22F),
		GUILayout.ExpandWidth(true)
	};
	private static readonly GUILayoutOption[] s_BtnHorizontalOptions = {
		GUILayout.Width(36F),
		GUILayout.MinHeight(22F),
		GUILayout.ExpandHeight(true)
	};

	private Texture2D alignLeft;
	private Texture2D alignMiddle;
	private Texture2D alignRight;
	private Texture2D alignTop;
	private Texture2D alignCenter;
	private Texture2D alignBottom;
	private Texture2D sameWidth;
	private Texture2D sameHeight;
	private Texture2D fitWidth;
	private Texture2D fitHeight;
	private Texture2D groupPack;
	private Texture2D groupUnpack;
	private Texture2D averageGapH;
	private Texture2D averageGapV;

	private void DrawButtons(bool isHorizontal) {
		if (s_BtnStyle == null) {
			s_BtnStyle = new GUIStyle("ButtonMid") {
				margin = new RectOffset(0, 0, 0, 0),
				padding = new RectOffset(0, 0, 0, 0),
				border = new RectOffset(0, 0, 0, 0),
				overflow = new RectOffset(0, 0, 0, 0),
			};
		}

		GUILayoutOption[] btnOptions;
		if (isHorizontal) {
			EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
			btnOptions = s_BtnHorizontalOptions;
		} else {
			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
			btnOptions = s_BtnVerticalOptions;
		}
		GUI.contentColor = EditorGUIUtility.isProSkin ? Color.white : Color.gray;
		
		DrawAlignBtn(alignLeft, "左对齐", 0, 0, s_BtnStyle, btnOptions);
		GUILayout.Space(-1F);
		DrawAlignBtn(alignMiddle, "水平居中", 0, 0.5F, s_BtnStyle, btnOptions);
		GUILayout.Space(-1F);
		DrawAlignBtn(alignRight, "右对齐", 0, 1, s_BtnStyle, btnOptions);
		GUILayout.Space(1F);
		DrawAlignBtn(alignTop, "上对齐", 1, 1, s_BtnStyle, btnOptions);
		GUILayout.Space(-1F);
		DrawAlignBtn(alignCenter, "竖直居中", 1, 0.5F, s_BtnStyle, btnOptions);
		GUILayout.Space(-1F);
		DrawAlignBtn(alignBottom, "下对齐", 1, 0, s_BtnStyle, btnOptions);

		GUILayout.Space(3F);

		DrawResizeBtn(sameWidth, "相同宽度", 0, s_BtnStyle, btnOptions);
		GUILayout.Space(-1F);
		DrawResizeBtn(sameHeight, "相同高度", 1, s_BtnStyle, btnOptions);

		GUILayout.Space(3F);

		DrawFitBtn(fitWidth, "水平贴合", 0, s_BtnStyle, btnOptions);
		GUILayout.Space(-1F);
		DrawFitBtn(fitHeight, "竖直贴合", 1, s_BtnStyle, btnOptions);

		GUILayout.Space(3F);

		DrawGroupPackBtn(groupPack, "成组", s_BtnStyle, btnOptions);
		GUILayout.Space(-1F);
		DrawGroupUnpackBtn(groupUnpack, "解组", s_BtnStyle, btnOptions);

		GUILayout.Space(3F);

		DrawGapBtn(averageGapH, "平均间距", 0, s_BtnStyle, btnOptions);
		GUILayout.Space(-1F);
		DrawGapBtn(averageGapV, "平均行距", 1, s_BtnStyle, btnOptions);

		if (isHorizontal) {
			EditorGUILayout.EndHorizontal();
		} else {
			EditorGUILayout.EndVertical();
		}
	}

	private static void DrawAlignBtn(Texture tex, string name, int axis, float alignPivot, GUIStyle style, params GUILayoutOption[] btnOptions) {
		if (GUILayout.Button(new GUIContent(tex, name + "（按住shift同时设置轴点）"), style, btnOptions)) {
			bool holdShift = (Event.current.modifiers & EventModifiers.Shift) != 0;
			Align(axis, alignPivot, holdShift);
		}
	}

	private static void DrawResizeBtn(Texture tex, string name, int axis, GUIStyle style, params GUILayoutOption[] btnOptions) {
		if (GUILayout.Button(new GUIContent(tex, name), style, btnOptions)) {
			SameSize(axis);
		}
	}

	private static void DrawFitBtn(Texture tex, string name, int axis, GUIStyle style, params GUILayoutOption[] btnOptions) {
		if (GUILayout.Button(new GUIContent(tex, name + "（按住shift保持世界坐标）"), style, btnOptions)) {
			bool holdShift = (Event.current.modifiers & EventModifiers.Shift) != 0;
			Fit(axis, holdShift);
		}
	}

	private static void DrawGroupPackBtn(Texture tex, string name, GUIStyle style, params GUILayoutOption[] btnOptions) {
		if (GUILayout.Button(new GUIContent(tex, name + "（按住shift穿透锚点尚未实现）"), style, btnOptions)) {
			GroupPack();
		}
	}

	private static void DrawGroupUnpackBtn(Texture tex, string name, GUIStyle style, params GUILayoutOption[] btnOptions) {
		if (GUILayout.Button(new GUIContent(tex, name + "（按住shift穿透锚点尚未实现）"), style, btnOptions)) {
			GroupUnpack();
		}
	}

	private static void DrawGapBtn(Texture tex, string name, int axis, GUIStyle style, params GUILayoutOption[] btnOptions) {
		if (GUILayout.Button(new GUIContent(tex, name + "（按住shift重新定义缝隙）"), style, btnOptions)) {
			bool holdShift = (Event.current.modifiers & EventModifiers.Shift) != 0;
			AverageGap(axis, holdShift);
		}
	}

	private static void Align(int axis, float alignPivot, bool alsoSetPivot) {
		(RectTransform based, List<RectTransform> list) = GetBasedAndList();
		if (based) {
			// 获取对齐点
			(float min, float max) = CalculateRangePart(axis, based);
			float basedPosPart = Mathf.Lerp(min, max, alignPivot);
			// 挨个计算deltaPosition并移动位置
			foreach (var trans in list) {
				Undo.RecordObject(trans, "Align");
				if (alsoSetPivot) {
					Vector2 pivot = trans.pivot;
					pivot[axis] = alignPivot;
					trans.pivot = pivot;
				}

				(float _min, float _max) = CalculateRangePart(axis, trans);
				float posPart = Mathf.Lerp(_min, _max, alignPivot);
				Vector3 position = trans.position;
				position[axis] += basedPosPart - posPart;
				trans.position = position;
			}
		}
	}

	private static void SameSize(int axis) {
		(RectTransform based, List<RectTransform> list) = GetBasedAndList();
		if (based) {
			// 获取目标尺寸
			float basedSizePart = based.rect.size[axis];
			// 挨个计算尺寸
			foreach (var trans in list) {
				RectTransform parent = trans.parent as RectTransform;
				float parentSizePart = parent == null ? 0 : parent.rect.size[axis];
				float anchorSizePart = (trans.anchorMax[axis] - trans.anchorMin[axis]) * parentSizePart;
				Vector2 sizeDelta = trans.sizeDelta;
				sizeDelta[axis] = basedSizePart - anchorSizePart;

				Undo.RecordObject(trans, "Resize");
				trans.sizeDelta = sizeDelta;
			}
		}
	}

	private static void Fit(int axis, bool keepPosition) {
		RectTransform activeTrans = Selection.activeTransform as RectTransform;
		if (activeTrans) {
			RectTransform[] totalChildren = activeTrans.GetComponentsInChildren<RectTransform>(true);
			if (totalChildren.Length <= 1) {
				Text text = activeTrans.GetComponent<Text>();
				if (text) {
					ContentSizeFitter fitter = activeTrans.gameObject.AddComponent<ContentSizeFitter>();
					if (axis == 0) {
						fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
					} else if (axis == 1) {
						fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
					}

					Undo.RecordObject(activeTrans, "Resize");
					EditorApplication.delayCall += () => UObject.DestroyImmediate(fitter);
				}

				return;
			}

			// 计算出包围盒范围
			float min = int.MaxValue, max = int.MinValue;
			foreach (var child in totalChildren) {
				if (child != activeTrans) {
					(float _min, float _max) = CalculateRangePart(axis, child, activeTrans);
					if (_min < min) min = _min;
					if (_max > max) max = _max;
				}
			}

			// 记录状态，用于撤销
			Undo.RecordObject(activeTrans, "Fit");
			foreach (Transform child in activeTrans) {
				Undo.RecordObject(child, "Fit");
			}

			// 先把子物体都移出来
			int childCount = activeTrans.childCount;
			Transform[] children = new Transform[childCount];
			for (int index = childCount - 1; index >= 0; --index) {
				Transform child = activeTrans.GetChild(index);
				children[index] = child;
				child.SetParent(activeTrans.parent);
			}

			float sizePart = max - min;
			// 计算并设置pivot或localPosition
			Vector2 pivot = activeTrans.pivot;
			if (keepPosition) {
				float pivotPart = -min / sizePart;
				pivot[axis] = pivotPart;
				activeTrans.pivot = pivot;
			} else {
				Vector3 localPivotPos = Vector3.zero;
				localPivotPos[axis] = min + pivot[axis] * sizePart;
				activeTrans.position = activeTrans.TransformPoint(localPivotPos);
			}

			// 计算sizeDelta
			RectTransform parent = activeTrans.parent as RectTransform;
			float parentSizePart = parent == null ? 0 : parent.rect.size[axis];
			float anchorSizePart = (activeTrans.anchorMax[axis] - activeTrans.anchorMin[axis]) * parentSizePart;
			Vector2 sizeDelta = activeTrans.sizeDelta;
			sizeDelta[axis] = sizePart - anchorSizePart;
			activeTrans.sizeDelta = sizeDelta;

			// 把子物体都移回去
			for (int index = 0; index < childCount; ++index) {
				children[index].SetParent(activeTrans);
			}
		}
	}

	private static void AverageGap(int axis, bool revertOrder) {
		List<RectTransform> list = GetSelectedList();
		int count = list.Count;
		if (count > 2) {
			// 记录每个节点的min和max
			List<(float, float)> ranges = new List<(float, float)>(count);
			foreach (var trans in list) {
				(float min, float max) = CalculateRangePart(axis, trans);
				ranges.Add((min, max));
			}

			// 如果firstMin和lastMax不是外边界，则倒一下顺序，让外边界成为firstMin和lastMax
			var (firstMin, firstMax) = ranges[0];
			var (lastMin, lastMax) = ranges[count - 1];
			if (lastMax - firstMin < firstMax - lastMin) {
				list.Reverse();
				ranges.Reverse();
				(firstMin, firstMax) = ranges[0];
				(lastMin, lastMax) = ranges[count - 1];
			}

			// 计算gap
			float dis = revertOrder ? lastMin - firstMax : lastMax - firstMin;
			foreach (var (min, max) in ranges) {
				dis -= revertOrder ? min - max : max - min;
			}

			float gap = dis / (count - 1);
			// 记录每个节点应该在的位置
			Dictionary<RectTransform, float> transPosDict = new Dictionary<RectTransform, float>(count);
			float tempPos = revertOrder ? firstMax : firstMin;
			for (int index = 0; index < count; ++index) {
				RectTransform trans = list[index];
				(float min, float max) = ranges[index];
				transPosDict.Add(trans, trans.position[axis] + tempPos - (revertOrder ? max : min));
				tempPos += (revertOrder ? min - max : max - min) + gap;
			}

			// 有父子关系的，父节点排前面。设置父节点会影响子节点，所以先设置父节点，再设置子节点。
			list.Sort((tans1, tans2) => tans1.IsChildOf(tans2) ? 1 : tans2.IsChildOf(tans1) ? -1 : 0);
			// 设置每个节点的位置
			foreach (var trans in list) {
				Vector3 pos = trans.position;
				pos[axis] = transPosDict[trans];
				Undo.RecordObject(trans, "AverageGap");
				trans.position = pos;
			}
		}
	}

	private static void GroupPack() {
		List<RectTransform> list = GetSelectedList();
		int count = list.Count;
		if (count > 0) {
			// 只打包与第一个节点有相同父节点的节点
			Transform parent = list[0].parent;
			List<RectTransform> groupChildren = new List<RectTransform>();
			foreach (Transform child in parent) {
				if (child is RectTransform rectChild && list.Contains(rectChild)) {
					groupChildren.Add(rectChild);
				}
			}

			// 新建一个作为组的节点
			GameObject go = new GameObject("Group");
			Undo.RegisterCreatedObjectUndo(go, "GroupPack");
			RectTransform groupTrans = go.AddComponent<RectTransform>();
			groupTrans.SetParent(parent);
			// groupTrans.localPosition = Vector3.zero;
			groupTrans.localRotation = Quaternion.identity;
			groupTrans.localScale = Vector3.one;
			groupTrans.anchorMin = Vector2.zero;
			groupTrans.anchorMax = Vector2.one;
			// groupTrans.sizeDelta = Vector2.zero;

			// 把要打包的节点都移进去
			foreach (RectTransform groupChild in groupChildren) {
				Undo.SetTransformParent(groupChild, groupTrans, "GroupPack");
			}

			// 让组节点贴合子节点
			Selection.activeTransform = groupTrans;
			Fit(0, false);
			Fit(1, false);
		}
	}

	private static void GroupUnpack() {
		List<RectTransform> list = GetSelectedList();
		int count = list.Count;
		if (count > 0) {
			HashSet<Transform> childrenSet = new HashSet<Transform>();
			for (int index = count - 1; index >= 0; --index) {
				RectTransform trans = list[index];
				if (trans.name == "Group") {
					Transform parent = trans.parent;
					int siblingIndex = trans.GetSiblingIndex();
					for (int childIndex = trans.childCount - 1; childIndex >= 0; --childIndex) {
						Transform groupChild = trans.GetChild(childIndex);
						childrenSet.Add(groupChild);
						Undo.SetTransformParent(groupChild, parent, "GroupUnpack");
						groupChild.SetSiblingIndex(siblingIndex);
					}

					Undo.DestroyObjectImmediate(trans.gameObject);
				}
			}
			Selection.objects = new List<Transform>(childrenSet).FindAll(child => child)
				.ConvertAll(child => (UObject) child.gameObject).ToArray();
		}
	}

	private static (float, float) CalculateRangePart(int axis, RectTransform trans, Transform relativeToTrans = null) {
		Rect rect = trans.rect;
		Vector3 globalP0 = trans.TransformPoint(new Vector3(rect.x, rect.y));
		Vector3 globalP1 = trans.TransformPoint(new Vector3(rect.x, rect.y + rect.height));
		Vector3 globalP2 = trans.TransformPoint(new Vector3(rect.x + rect.width, rect.y));
		Vector3 globalP3 = trans.TransformPoint(new Vector3(rect.x + rect.width, rect.y + rect.height));
		Vector3 localP0 = relativeToTrans ? relativeToTrans.InverseTransformPoint(globalP0) : globalP0;
		Vector3 localP1 = relativeToTrans ? relativeToTrans.InverseTransformPoint(globalP1) : globalP1;
		Vector3 localP2 = relativeToTrans ? relativeToTrans.InverseTransformPoint(globalP2) : globalP2;
		Vector3 localP3 = relativeToTrans ? relativeToTrans.InverseTransformPoint(globalP3) : globalP3;
		float min = Mathf.Min(localP0[axis], localP1[axis], localP2[axis], localP3[axis]);
		float max = Mathf.Max(localP0[axis], localP1[axis], localP2[axis], localP3[axis]);
		return (min, max);
	}

	/// <summary>
	/// 选中多个时，以选中的第一个节点为基准
	/// 只选中一个时，以父节点为基准
	/// </summary>
	/// <returns>基准和列表</returns>
	private static (RectTransform, List<RectTransform>) GetBasedAndList() {
		List<RectTransform> list = GetSelectedList();
		if (list.Count > 0) {
			RectTransform based = list.Count > 1 ? list[0] : list[0].parent as RectTransform;
			if (based) {
				// 有父子关系的，父节点排前面。设置父节点会影响子节点，所以先设置父节点，再设置子节点。
				list.Sort((tans1, tans2) => tans1.IsChildOf(tans2) ? 1 : tans2.IsChildOf(tans1) ? -1 : 0);
				return (based, list);
			}
		}
		return (null, null);
	}

	private static List<RectTransform> GetSelectedList() {
		List<RectTransform> list = new List<RectTransform>();
		foreach (var obj in Selection.objects) {
			if (obj is GameObject go && go.transform is RectTransform rectTrans) {
				list.Add(rectTrans);
			}
		}
		return list;
	}
}
