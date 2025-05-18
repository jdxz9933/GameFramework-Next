using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Loxodon.Framework.Commands;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.ViewModels;
using PropertyChanged;
using UnityEngine;

namespace GameLogic
{
    [AddINotifyPropertyChangedInterface]
    public class GameMainViewModel
    {
        private SimpleCommand _btn_TestCommand;

        public GameMainViewModel()
        {
            _btn_TestCommand = new SimpleCommand(Btn_TestEvent);
        }

        private void Btn_TestEvent()
        {
            TestTxt = "Test1234";
            FightContext context = new FightContext();
            Context.AddContext(nameof(FightContext), context);
            context.StartFight().Forget();
        }

        public ICommand Btn_TestCommand => _btn_TestCommand;

        public string TestTxt { get; set; }
    }
}