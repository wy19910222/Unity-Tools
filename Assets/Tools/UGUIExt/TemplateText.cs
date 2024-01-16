/*
 * @Author: wangyun
 * @CreateTime: 2022-07-02 14:10:46 489
 * @LastEditor: wangyun
 * @EditTime: 2022-07-02 14:10:46 501
 */

namespace UnityEngine.UI {
	[AddComponentMenu("UI/Effects/Template Text", 15)]
	[RequireComponent(typeof(Text))]
	public class TemplateText : TemplateTextBase {
		private Text m_CompText;
		public Text CompText {
			get {
				if (m_CompText == null) {
					m_CompText = GetComponent<Text>();
				}
				return m_CompText;
			}
		}
		
		[ContextMenu("Apply")]
		public override void Apply() {
			CompText.text = FinalText;
		}
	}
}
