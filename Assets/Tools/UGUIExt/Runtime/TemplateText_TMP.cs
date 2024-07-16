/*
 * @Author: wangyun
 * @CreateTime: 2022-10-26 11:53:33 727
 * @LastEditor: wangyun
 * @EditTime: 2022-12-13 13:48:50 276
 */

using UnityEngine;
using UnityEngine.UI;

namespace TMPro {
	[AddComponentMenu("Mesh/Template Text(TMP)", 15)]
	[RequireComponent(typeof(TMP_Text))]
	public class TemplateText_TMP : TemplateTextBase {
		private TMP_Text m_CompText;
		public TMP_Text CompText {
			get {
				if (m_CompText == null) {
					m_CompText = GetComponent<TMP_Text>();
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
