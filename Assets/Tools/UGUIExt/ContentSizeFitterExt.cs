using System;

namespace UnityEngine.UI {
	[AddComponentMenu("Layout/Content Size Fitter Ext", 141)]
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	public class ContentSizeFitterExt : ContentSizeFitter {
		[SerializeField] protected float m_MinWidth;
		public float minWidth {
			get => m_MinWidth;
			set {
				if (!Mathf.Approximately(m_MinWidth, value)) {
					m_MinWidth = value;
					SetDirty();
				}
			}
		}
		
		[SerializeField] protected float m_MinHeight;
		public float minHeight {
			get => m_MinHeight;
			set {
				if (!Mathf.Approximately(m_MinHeight, value)) {
					m_MinHeight = value;
					SetDirty();
				}
			}
		}
		
		[SerializeField] protected float m_MaxWidth;
		public float maxWidth {
			get => m_MaxWidth;
			set {
				if (!Mathf.Approximately(m_MaxWidth, value)) {
					m_MaxWidth = value;
					SetDirty();
				}
			}
		}
		
		[SerializeField] protected float m_MaxHeight;
		public float maxHeight {
			get => m_MaxHeight;
			set {
				if (!Mathf.Approximately(m_MaxHeight, value)) {
					m_MaxHeight = value;
					SetDirty();
				}
			}
		}

		[NonSerialized] private RectTransform m_Rect;
		private RectTransform rectTransform {
			get {
				if (m_Rect == null)
					m_Rect = GetComponent<RectTransform>();
				return m_Rect;
			}
		}

		private DrivenRectTransformTracker m_Tracker;

		private void HandleSelfFittingAlongAxis(int axis) {
			FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
			if (fitting == FitMode.Unconstrained) {
				// Keep a reference to the tracked transform, but don't control its properties:
				m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
				return;
			}

			m_Tracker.Add(this, rectTransform,
					(axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));

			// Set size to min or preferred size
			float size = fitting == FitMode.MinSize ? LayoutUtility.GetMinSize(m_Rect, axis) :
					LayoutUtility.GetPreferredSize(m_Rect, axis);
			switch (axis) {
				case 0:
					if (maxWidth > 0 && size > maxWidth) {
						size = maxWidth;
					}
					if (minWidth > 0 && size < minWidth) {
						size = minWidth;
					}
					break;
				case 1:
					if (maxHeight > 0 && size > maxHeight) {
						size = maxHeight;
					}
					if (minHeight > 0 && size < minHeight) {
						size = minHeight;
					}
					break;
			}
			rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis) axis, size);
		}

		public override void SetLayoutHorizontal() {
			m_Tracker.Clear();
			HandleSelfFittingAlongAxis(0);
		}

		public override void SetLayoutVertical() {
			HandleSelfFittingAlongAxis(1);
		}
	}
}
