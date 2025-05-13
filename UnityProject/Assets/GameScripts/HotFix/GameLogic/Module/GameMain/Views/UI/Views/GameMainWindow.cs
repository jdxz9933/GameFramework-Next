// ================================================
//描 述:
//作 者:JDXZ9933
//创建时间:2025-05-13 00-00-27
//修改作者:JDXZ9933
//修改时间:2025-05-13 00-00-27
//版 本:#Version# 
// ===============================================

using System.Collections;
using System.Collections.Generic;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Views;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
	/// <summary>
	/// Please modify the description.
	/// </summary>
	public partial class GameMainWindow : Window
	{
        private GameMainViewModel m_ViewModel;
		protected override void OnCreate(IBundle bundle) {
			 GetBindComponents(gameObject);

            m_ViewModel = new GameMainViewModel();
			var bindingSet = this.CreateBindingSet(m_ViewModel);
			// bindingSet.Bind().For(v => v.OnCloseRequest).To(vm => vm.CloseRequest);
/*--------------------Auto generate start button listener.Do not modify!--------------------*/
			bindingSet.Bind(m_Btn_Test).For(v => v.onClick).To(vm => vm.Btn_TestCommand).OneWay();
			bindingSet.Bind(m_Txt_Test).For(v => v.text).To(vm => vm.TestTxt).OneWay();
/*--------------------Auto generate end button listener.Do not modify!----------------------*/
			bindingSet.Build();
		}

	}
}
