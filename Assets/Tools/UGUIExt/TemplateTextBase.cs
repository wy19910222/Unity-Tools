/*
 * @Author: wangyun
 * @CreateTime: 2023-02-18 17:16:15 439
 * @LastEditor: wangyun
 * @EditTime: 2023-02-18 17:16:15 444
 */

using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;

namespace UnityEngine.UI {
	public class TemplateTextBase : MonoBehaviour {
		[TextArea(3, 10)]
		[SerializeField]
		protected string m_Text = string.Empty;
		
		public string text {
			get => m_Text;
			set {
				if (string.IsNullOrEmpty(value)) {
					if (!string.IsNullOrEmpty(m_Text)) {
						m_Text = string.Empty;
						Apply();
					}
				} else {
					if (m_Text != value) {
						m_Text = value;
						Apply();
					}
				}
			}
		}

		[SerializeField, HideInInspector]
		private List<string> m_VariateNames = new List<string>();
		[SerializeField, HideInInspector]
		private List<string> m_VariateValues = new List<string>();
		public Dictionary<string, object> Variates {
			get {
				Dictionary<string, object> dict = new Dictionary<string, object>();
				for (int i = 0, kLen = m_VariateNames.Count; i < kLen; ++i) {
					dict[m_VariateNames[i]] = m_VariateValues[i];
				}
				return dict;
			}
			set {
				m_VariateNames.Clear();
				m_VariateValues.Clear();
				if (value != null) {
					m_VariateNames.AddRange(value.Keys);
					foreach (var _value in value.Values) {
						m_VariateValues.Add(_value + string.Empty);
					}
				}
				Apply();
			}
		}

		public string GetVariate(string varName) {
			int index = m_VariateNames.IndexOf(varName);
			return index == -1 ? null: m_VariateValues[index];
		}
		public void SetVariate(string varName, object varValue) {
			SetVariate(varName, varValue + string.Empty);
		}
		public void SetVariate(string varName, string varValue) {
			int index = m_VariateNames.IndexOf(varName);
			if (index == -1) {
				m_VariateNames.Add(varName);
				m_VariateValues.Add(varValue);
			} else {
				m_VariateNames[index] = varName;
				m_VariateValues[index] = varValue;
			}
			Apply();
		}

		public string FinalText {
			get {
				Dictionary<string, string> dict = new Dictionary<string, string>();
				for (int i = 0, kLen = m_VariateNames.Count; i < kLen; ++i) {
					dict[m_VariateNames[i]] = m_VariateValues[i];
				}
				string[] parts = Regex.Split(text, "{(.*?)=(.*?)}");
				int partCount = parts.Length;
				StringBuilder sb = new StringBuilder(partCount - partCount / 3);
				for (int i = 0; i < partCount - 1; ++i) {
					sb.Append(parts[i]);
					string varName = parts[++i];
					string varValue = parts[++i];
					dict.TryGetValue(varName, out string overrideValue);
					sb.Append(overrideValue ?? varValue);
				}
				sb.Append(parts[partCount - 1]);
				return sb.ToString();
			}
		}

		public virtual void Apply() {
		}

		private void OnValidate() {
			Apply();
		}
	}
}
