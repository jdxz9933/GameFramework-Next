// ================================================
//描 述:
//作 者:JDXZ9933
//创建时间:2025-05-12 00-14-35
//修改作者:JDXZ9933
//修改时间:2025-05-12 00-14-35
//版 本:#Version# 
// ===============================================

using System.Collections;
using System.Collections.Generic;
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

//         protected override void OnInit(object userData)
//         {
//             base.OnInit(userData);
//             GetBindComponents(gameObject);
//
// /*--------------------Auto generate start button listener.Do not modify!--------------------*/
//             m_Btn_Test.onClick.AddListener(Btn_TestEvent);
// /*--------------------Auto generate end button listener.Do not modify!----------------------*/
//         }

        private void Btn_TestEvent()
        {
        }

/*--------------------Auto generate footer.Do not add anything below the footer!------------*/
        protected override void OnCreate(IBundle bundle)
        {
            GetBindComponents(gameObject);
            m_ViewModel = new GameMainViewModel();
        }
    }
}