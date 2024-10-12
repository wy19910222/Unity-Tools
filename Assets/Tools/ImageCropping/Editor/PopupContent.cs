/*
 * @Author: wangyun
 * @CreateTime: 2024-08-08 21:44:43 731
 * @LastEditor: wangyun
 * @EditTime: 2024-08-08 21:44:43 735
 */

using System;
using UnityEditor;
using UnityEngine;

namespace WYTools.ImageCropping {
	public class PopupContent : PopupWindowContent {
		public float Width { get; set; }
		public float Height { get; set; }
		public Action<Rect> OnGUIAction { get; set; }
		public Action OnOpenAction { get; set; }
		public Action OnCloseAction { get; set; }

		public override Vector2 GetWindowSize() => new Vector2(Width, Height);

		public override void OnGUI(Rect rect) => OnGUIAction?.Invoke(rect);

		public override void OnOpen() => OnOpenAction?.Invoke();

		public override void OnClose() => OnCloseAction?.Invoke();
	}
}
