using System.Collections;
using System.Collections.Generic;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Views;
using UnityEngine;

namespace GameLogic
{
    public static class UIExtension
    {
        public static void ShowWindow(this UIWindowType windowType)
        {
            var locator = Context.GetApplicationContext().GetService<IUIViewLocator>();
            var window = locator.LoadWindow(windowType.ToString());
            window.Create();
            window.Show();
        }
    }
}