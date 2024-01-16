/*
 * @Author: wangyun
 * @CreateTime: 2022-08-10 00:53:45 448
 * @LastEditor: wangyun
 * @EditTime: 2022-08-10 00:53:45 453
 */

using System.Collections.Generic;

namespace UnityEngine.UI {
	[AddComponentMenu("UI/Effects/Raw Image Animation", 15)]
	[RequireComponent(typeof(RawImage))]
	public class RawImageAnimation : MonoBehaviour {
		public int frameRate = 30;
		public int currentFrame;
		public float speed = 1;
		public bool loop;
		public List<Texture> textureFrames = new List<Texture>();
		
		public int FrameRate { get => frameRate; set => frameRate = value; }
		public int CurrentFrame { get => currentFrame; set => currentFrame = value; }
		public float Speed { get => speed; set => speed = value; }
		public bool Loop { get => loop; set => loop = value; }
		
		private RawImage m_RawImageSource;
		private float m_Time;

		private void Update() {
			int spriteCount = textureFrames.Count;
			if (spriteCount > 0) {
				int playDirection = (int) Mathf.Sign(speed);
				float unsignedSpeed = Mathf.Abs(speed);
				m_Time += Time.deltaTime * unsignedSpeed;
				float interval = 1F / frameRate;
				if (m_Time > interval) {
					m_Time -= interval;
					currentFrame += playDirection;
					if (loop) {
						currentFrame %= spriteCount;
						if (currentFrame < 0) {
							currentFrame += spriteCount;
						}
					} else {
						currentFrame = Mathf.Min(currentFrame, spriteCount - 1);
					}
					Texture tex = null;
					for (int i = currentFrame; i >= 0 && !tex; --i) {
						tex = textureFrames[i];
					}
					if (!m_RawImageSource) {
						m_RawImageSource = GetComponent<RawImage>();
					}
					m_RawImageSource.texture = tex;
				}
			}
		}

		private void OnValidate() {
			frameRate = Mathf.Max(frameRate, 1);
			int spriteCount = textureFrames.Count;
			if (spriteCount > 0) {
				currentFrame = Mathf.Clamp(currentFrame, 0, spriteCount - 1);
				Texture tex = null;
				for (int i = currentFrame; i >= 0 && !tex; --i) {
					tex = textureFrames[i];
				}
				if (!m_RawImageSource) {
					m_RawImageSource = GetComponent<RawImage>();
				}
				m_RawImageSource.texture = tex;
			}
		}
	}
}
