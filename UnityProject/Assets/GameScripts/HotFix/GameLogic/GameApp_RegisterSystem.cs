using System.Collections.Generic;
using System.Globalization;
using GameBase;
using GameLogic;
using GameFramework;
using GameMain;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Examples;
using Loxodon.Framework.Localizations;
using Loxodon.Framework.Messaging;
using Loxodon.Framework.Services;
using Loxodon.Framework.Views;
using UnityEngine;
using UnityGameFramework.Runtime;

public partial class GameApp
{
    private List<ILogicSys> m_ListLogicMgr;

    ISubscription<WindowStateEventArgs> subscription;

    private ApplicationContext context;

    private void InitSystem()
    {
        m_ListLogicMgr = new List<ILogicSys>();
        CodeTypes.Instance.Init(s_HotfixAssembly.ToArray());
        EventInterfaceHelper.Init();
        RegisterAllSystem();
        InitSystemSetting();

        //GlobalWindowManager

        UILoadMgr.UIRoot.gameObject.GetOrAddComponent<GlobalWindowManager>();
        context = Context.GetApplicationContext();

        IServiceContainer container = context.GetContainer();

        /* Initialize the data binding service */
        BindingServiceBundle bundle = new BindingServiceBundle(context.GetContainer());
        bundle.Start();

        /* Initialize the ui view locator and register UIViewLocator */
        container.Register<IUIViewLocator>(new ResourcesViewLocator());

        /* Initialize the localization service */
        //CultureInfo cultureInfo = Locale.GetCultureInfoByLanguage (SystemLanguage.English);
        CultureInfo cultureInfo = Locale.GetCultureInfo();
        var localization = Localization.Current;
        localization.CultureInfo = cultureInfo;
        localization.AddDataProvider(new ResourcesDataProvider("LocalizationExamples", new XmlDocumentParser()));

        /* register Localization */
        container.Register<Localization>(localization);

        /* register AccountRepository */
        IAccountRepository accountRepository = new AccountRepository();
        container.Register<IAccountService>(new AccountService(accountRepository));


        /* Enable window state broadcast */
        GlobalSetting.enableWindowStateBroadcast = true;
        /*
         * Use the CanvasGroup.blocksRaycasts instead of the CanvasGroup.interactable
         * to control the interactivity of the view
         */
        GlobalSetting.useBlocksRaycastsInsteadOfInteractable = true;

        /* Subscribe to window state change events */
        subscription = Window.Messenger.Subscribe<WindowStateEventArgs>(e =>
        {
            Debug.LogFormat("The window[{0}] state changed from {1} to {2}", e.Window.Name, e.OldState, e.State);
        });
    }

    /// <summary>
    /// 设置一些通用的系统属性。
    /// </summary>
    private void InitSystemSetting()
    {
    }

    /// <summary>
    /// 注册所有逻辑系统
    /// </summary>
    private void RegisterAllSystem()
    {
    }

    /// <summary>
    /// 注册逻辑系统。
    /// </summary>
    /// <param name="logicSys">ILogicSys</param>
    /// <returns></returns>
    public bool AddLogicSys(ILogicSys logicSys)
    {
        if (m_ListLogicMgr.Contains(logicSys))
        {
            Log.Fatal("Repeat add logic system: {0}", logicSys.GetType().Name);
            return false;
        }

        if (!logicSys.OnInit())
        {
            Log.Fatal("{0} Init failed", logicSys.GetType().Name);
            return false;
        }

        m_ListLogicMgr.Add(logicSys);

        return true;
    }
}