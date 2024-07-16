using System.Collections.Generic;

namespace UnityEngine.UI {
	[AddComponentMenu("UI/Effects/Outline Mellow", 15)]
	public class OutlineMellow : Outline {
		private const float SQRT_2 = 1.414213562F;
		private const float ANGLE_OFFSET = Mathf.PI / 4;
		
		[Range(0, 5)]
		public int level; //生成4+4*level倍面片

		public override void ModifyMesh(VertexHelper vh) {
			if (level == 0) {
				base.ModifyMesh(vh);
				return;
			}
			
			if (!IsActive())
				return;

			var verts = new List<UIVertex>();
			vh.GetUIVertexStream(verts);

			var multiple = 4 + 4 * level;
			var neededCapacity = verts.Count * (multiple+ 1);
			if (verts.Capacity < neededCapacity)
				verts.Capacity = neededCapacity;

			var start = 0;
			var end = verts.Count;
			ApplyShadowZeroAlloc(verts, effectColor, start, end, effectDistance.x, effectDistance.y);
			start = end;
			end = verts.Count;
			ApplyShadowZeroAlloc(verts, effectColor, start, end, effectDistance.x, -effectDistance.y);
			start = end;
			end = verts.Count;
			ApplyShadowZeroAlloc(verts, effectColor, start, end, -effectDistance.x, effectDistance.y);
			start = end;
			end = verts.Count;
			ApplyShadowZeroAlloc(verts, effectColor, start, end, -effectDistance.x, -effectDistance.y);

			if ((level & 1) != 0) {
				var x = effectDistance.x * SQRT_2;
				var y = effectDistance.y * SQRT_2;
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, x, 0);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, 0, -y);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, -x, 0);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, 0, y);
			}
			
			// 每多少角度复制一份
			var angleDelta = Mathf.PI * 2 / multiple;
			// 只需要计算一个角，然后上下左右镜像出另外7份就好
			for (int i = 1, length = level >> 1; i <= length; ++i) {
				var _x = SQRT_2 * Mathf.Cos(ANGLE_OFFSET + angleDelta * i);
				var _y = SQRT_2 * Mathf.Sin(ANGLE_OFFSET + angleDelta * i);
				
				var x1 = effectDistance.x * _x;
				var y1 = effectDistance.y * _y;
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, x1, y1);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, x1, -y1);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, -x1, y1);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, -x1, -y1);
				
				var x2 = effectDistance.x * _y;
				var y2 = effectDistance.y * _x;
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, x2, y2);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, x2, -y2);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, -x2, y2);
				start = end;
				end = verts.Count;
				ApplyShadowZeroAlloc(verts, effectColor, start, end, -x2, -y2);
			}

			vh.Clear();
			vh.AddUIVertexTriangleStream(verts);
			verts.Clear();
		}
	}
}
