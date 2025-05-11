using UnityEngine.UI;
using UnityEngine;

namespace GameLogic
{
	public partial class GameMainWindow
	{
		private UIButtonSuper m_Btn_Test;
		private Text m_Txt_Test;

		private void GetBindComponents(GameObject go)
		{
			ComponentAutoBindTool autoBindTool = go.GetComponent<ComponentAutoBindTool>();

			m_Btn_Test = autoBindTool.GetBindComponent<UIButtonSuper>(0);
			m_Txt_Test = autoBindTool.GetBindComponent<Text>(1);
		}
	}
}
