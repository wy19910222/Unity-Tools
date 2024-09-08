/*
 * @Author: wangyun
 * @CreateTime: 2024-09-07 17:49:51 625
 * @LastEditor: wangyun
 * @EditTime: 2024-09-09 04:30:35 178
 */

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UI;

namespace WYTools.UITools {
	public static class Utility {
		[InitializeOnLoadMethod]
		private static void InitializeOnLoadMethod() {
			SceneView.duringSceneGui += OnSceneGUI;
			DragAndDrop.AddDropHandler(SceneDropHandler);
			DragAndDrop.AddDropHandler(HierarchyDropHandler);
		}

		private static bool IsUIToolsDisplayed => SceneView.lastActiveSceneView.TryGetOverlay("UITools", out Overlay uiTools) && uiTools.displayed;

		private static void OnSceneGUI(SceneView sceneView) {
			Event e = Event.current;
			if (!e.control) {
				return;
			}
			
			if (!IsUIToolsDisplayed) {
				return;
			}

			switch (e.type) {
				// case EventType.DragPerform: {
				// 	List<Object> needHandleList = GetSpriteAndRectTransformFromDragAndDrop();
				// 	if (needHandleList.Count > 0) {
				// 		RectTransform canvasTrans = GetContainerUnderMouse(sceneView, out Vector3 mouseLocalPosition);
				// 		if (canvasTrans) {
				// 			DropToTransform(needHandleList, canvasTrans, mouseLocalPosition);
				// 			DragAndDrop.AcceptDrag();
				// 			e.Use();
				// 		}
				// 	}
				// 	break;
				// }
				case EventType.KeyDown: {
					Vector3 offset = Vector3.zero;
					switch (e.keyCode) {
						case KeyCode.UpArrow:
							offset = Vector3.up;
							break;
						case KeyCode.DownArrow:
							offset = Vector3.down;
							break;
						case KeyCode.LeftArrow:
							offset = Vector3.left;
							break;
						case KeyCode.RightArrow:
							offset = Vector3.right;
							break;
					}

					bool isHandled = false;
					foreach (Transform trans in Selection.transforms) {
						if (trans is RectTransform) {
							trans.localPosition += offset;
							isHandled = true;
						}
					}
					if (isHandled) {
						e.Use();
					}
					break;
				}
			}
		}
		
		public static DragAndDropVisualMode SceneDropHandler(Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform _, bool perform) {
			// 只处理放的操作，拖的响应不变
			if (perform) {
				// 按住Ctrl键才响应
				if (!Event.current.control) {
					return DragAndDropVisualMode.None;
				}
			
				// UITools工具开着才响应
				if (!IsUIToolsDisplayed) {
					return DragAndDropVisualMode.None;
				}
			
				List<Object> needHandleList = GetSpriteAndRectTransformFromDragAndDrop();
				// 有需要操作的对象才操作
				if (needHandleList.Count <= 0) {
					return DragAndDropVisualMode.None;
				}
			
				// 获取到鼠标所指的Canvas，并计算鼠标相对于Canvas的位置
				RectTransform canvasTrans = GetCanvasTransformUnderMouse(SceneView.lastActiveSceneView, out Vector3 mouseLocalPosition);
				if (canvasTrans) {
					// 创建对象并放到指定层级和位置
					DropToTransform(needHandleList, canvasTrans, mouseLocalPosition);
					return DragAndDropVisualMode.Copy;
				}
			}
			return DragAndDropVisualMode.None;
		}
		
		private static readonly MethodInfo s_FindObjectFromInstanceIdMI = typeof(Object).GetMethod("FindObjectFromInstanceID", BindingFlags.Static | BindingFlags.NonPublic);
		public static DragAndDropVisualMode HierarchyDropHandler(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform _, bool perform) {
			// 按住Ctrl键才响应
			if (!Event.current.control) {
				return DragAndDropVisualMode.None;
			}
			
			// UITools工具开着才响应
			if (!IsUIToolsDisplayed) {
				return DragAndDropVisualMode.None;
			}
			
			List<Object> needHandleList = GetSpriteAndRectTransformFromDragAndDrop();
			// 有需要操作的对象才操作
			if (needHandleList.Count <= 0) {
				return DragAndDropVisualMode.None;
			}

			// 因为需要放到Canvas下面，所以只有拖到某个节点上才操作
			if (s_FindObjectFromInstanceIdMI.Invoke(null, new object[] {dropTargetInstanceID}) is GameObject go) {
				if (perform) {
					Transform trans = null;
					int siblingIndex = -1;
					// 拖到某个节点前面，说明是放在父节点下当前节点前
					if ((dropMode & HierarchyDropFlags.DropAbove) != 0) {
						trans = go.transform.parent;
						siblingIndex = trans.GetSiblingIndex();
					}
					// 拖到某个节点后面，说明是放在父节点下当前节点后
					else if ((dropMode & HierarchyDropFlags.DropBetween) != 0) {
						trans = go.transform;
						siblingIndex = trans.GetSiblingIndex() + 1;
						trans = trans.parent;
					}
					// 拖到某个节点，说明是放在当前节点下最后位置f
					else if ((dropMode & HierarchyDropFlags.DropUpon) != 0) {
						trans = go.transform;
					}
					// 父对象是RectTransform才允许拖放
					if (trans is RectTransform parent) {
						// 创建对象并放到指定层级
						DropToTransform(needHandleList, parent, Vector3.zero, siblingIndex);
						return DragAndDropVisualMode.Copy;
					}
				} else {
					// 搜索状态只能放到目标节点下
					if ((dropMode & HierarchyDropFlags.SearchActive) == 0 || (dropMode & HierarchyDropFlags.DropUpon) != 0) {
						return DragAndDropVisualMode.Copy;
					}
				}
			}
			return DragAndDropVisualMode.None;
		}
		
		private static List<Object> GetSpriteAndRectTransformFromDragAndDrop() {
			List<Object> needHandleList = new List<Object>();
			Object[] objectReferences = DragAndDrop.objectReferences;
			string[] paths = DragAndDrop.paths;
			for (int i = 0, length = objectReferences.Length; i < length; i++) {
				switch (objectReferences[i]) {
					case Sprite sprite:
						needHandleList.Add(sprite);
						break;
					case Texture2D _:
						string path = paths[i];
						if (!string.IsNullOrEmpty(path)) {
							Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
							if (sprite) {
								needHandleList.Add(sprite);
							}
						}
						break;
					case GameObject go:
						if (go.transform is RectTransform) {
							needHandleList.Add(go);
						}
						break;
				}
			}
			return needHandleList;
		}
		
		private static RectTransform GetCanvasTransformUnderMouse(SceneView sceneView, out Vector3 mouseLocalPosition) {
			Vector2 mousePos = Event.current.mousePosition;
			mousePos.y = sceneView.camera.pixelHeight - mousePos.y;
			Ray ray = sceneView.camera.ScreenPointToRay(mousePos);
			
			List<RectTransform> transList = new List<RectTransform>();
			List<Vector3> localPositionList = new List<Vector3>();
			List<int> sortingList = new List<int>();
			List<string> siblingIndexList = new List<string>();
			foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>()) {
				if (canvas.transform is RectTransform trans && canvas.gameObject.activeInHierarchy) {
					Vector3 forward = trans.forward;
					float dot = Vector3.Dot(trans.position - ray.origin, forward);
					float cosine = Vector3.Dot(ray.direction, forward);
					float distance = dot / cosine;
					Vector3 point = ray.origin + ray.direction * distance;
					Vector3 localPosition = trans.InverseTransformPoint(point);
					Rect rect = trans.rect;
					if (rect.Contains(localPosition)) {
						transList.Add(trans);
						localPositionList.Add(localPosition);
						sortingList.Add(canvas.sortingOrder);
						string siblingIndex = string.Empty;
						Transform temp = trans;
						while (temp != null) {
							siblingIndex = temp.GetSiblingIndex() + siblingIndex;
							temp = temp.parent;
						}
						siblingIndexList.Add(siblingIndex);
					}
				}
			}
			if (transList.Count <= 0) {
				mouseLocalPosition = Vector3.zero;
				return null;
			}
			
			int selectedIndex = 0;
			int selectedSorting = sortingList[0];
			string selectedSiblingIndex = siblingIndexList[0];
			for (int i = 1, length = transList.Count; i < length; ++i) {
				int sorting = sortingList[i];
				string siblingIndex = siblingIndexList[i];
				if (sorting > selectedSorting ||
						sorting == selectedSorting && string.CompareOrdinal(siblingIndex, selectedSiblingIndex) >= 0) {
					selectedIndex = i;
					selectedSorting = sorting;
					selectedSiblingIndex = siblingIndex;
				}
			}
			mouseLocalPosition = localPositionList[selectedIndex];
			return transList[selectedIndex];
		}
		
		private static void DropToTransform(IReadOnlyList<Object> objs, Transform parent, Vector3 mouseLocalPosition, int startSiblingIndex = -1) {
			for (int i = 0, length = objs.Count; i < length; ++i) {
				switch (objs[i]) {
					case Sprite sprite: {
						GameObject newGo = new GameObject(sprite.name, typeof(Image));
						Undo.RegisterCreatedObjectUndo(newGo, "UITools.CreateImageByDragAndDrop");
						if (newGo.transform is RectTransform trans) {
							Undo.SetTransformParent(trans, parent, "UITools.SetTransformParent");
							if (startSiblingIndex != -1) {
								trans.SetSiblingIndex(startSiblingIndex++);
							}
							trans.localPosition = mouseLocalPosition;
							Image image = newGo.GetComponent<Image>();
							image.raycastTarget = false;
							Undo.RecordObject(image, "UITools.ChangeSprite");
							image.sprite = sprite;
							image.SetNativeSize();
						}
						break;
					}
					case GameObject go: {
						GameObject newGo = GameObject.Instantiate(go);
						if (newGo) {
							newGo.name = go.name;
							Undo.RegisterCreatedObjectUndo(newGo, "UITools.CreateImageByDragAndDrop");
							if (newGo.transform is RectTransform trans) {
								Undo.SetTransformParent(trans, parent, "UITools.SetTransformParent");
								if (startSiblingIndex != -1) {
									trans.SetSiblingIndex(startSiblingIndex++);
								}
								trans.localPosition = mouseLocalPosition;
							}
						}
						break;
					}
				}
			}
		}
	}
}
