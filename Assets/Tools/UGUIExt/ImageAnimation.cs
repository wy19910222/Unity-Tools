/*
 * @Author: wangyun
 * @CreateTime: 2022-08-10 00:53:45 448
 * @LastEditor: wangyun
 * @EditTime: 2022-08-10 00:53:45 453
 */

using System.Collections.Generic;

namespace UnityEngine.UI {
	[AddComponentMenu("UI/Effects/Image Animation", 15)]
	[RequireComponent(typeof(Image))]
	public class ImageAnimation : MonoBehaviour {
		public int frameRate = 30;
		public int currentFrame;
		public float speed = 1;
		public bool loop;
		public List<Sprite> spriteFrames = new List<Sprite>();
		
		public int FrameRate { get => frameRate; set => frameRate = value; }
		public int CurrentFrame { get => currentFrame; set => currentFrame = value; }
		public float Speed { get => speed; set => speed = value; }
		public bool Loop { get => loop; set => loop = value; }
		
		private Image m_ImageSource;
		private float m_Time;

		private void Update() {
			int spriteCount = spriteFrames.Count;
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
					Sprite sprite = null;
					for (int i = currentFrame; i >= 0 && !sprite; --i) {
						sprite = spriteFrames[i];
					}
					if (!m_ImageSource) {
						m_ImageSource = GetComponent<Image>();
					}
					m_ImageSource.sprite = sprite;
				}
			}
		}

		private void OnValidate() {
			frameRate = Mathf.Max(frameRate, 1);
			int spriteCount = spriteFrames.Count;
			if (spriteCount > 0) {
				currentFrame = Mathf.Clamp(currentFrame, 0, spriteCount - 1);
				Sprite sprite = null;
				for (int i = currentFrame; i >= 0 && !sprite; --i) {
					sprite = spriteFrames[i];
				}
				if (!m_ImageSource) {
					m_ImageSource = GetComponent<Image>();
				}
				m_ImageSource.sprite = sprite;
			}
		}
	}
}
