using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using System;
using System.Text;
using CounterStrikeSharp.API.Modules.Entities;
using static CounterStrikeSharp.API.Core.Listeners;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace FlashingHtmlHudFix

{
    public partial class FlashingHtmlHudFix : BasePlugin
    {
        public override string ModuleName => "FlashingHtmlHudFix";
        public override string ModuleVersion => "1.2";
        public override string ModuleAuthor => "Deana + Oz-Lin";

        private CCSGameRules? _gameRules;
        private float _warmupEndTime;
        private bool _warmupEnded;
        private Timer? _timer;
        private bool _warmupStarted;

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnTick>(OnTick);
            RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFullHandler, HookMode.Post);
        }

        private HookResult OnPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (!_warmupStarted)
            {
                _warmupStarted = true;
                InitializeWarmupTime();
            }
            return HookResult.Continue;
        }

        private void OnMapStartHandler(string mapName)
        {
            _gameRules = null;
            _warmupEnded = false;
            _warmupStarted = false;
        }

        private void InitializeGameRules()
        {
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            _gameRules = gameRulesProxy?.GameRules;
        }

        private void InitializeWarmupTime()
        {
            var warmupTime = ConVar.Find("mp_warmuptime")!.GetPrimitiveValue<float>();
            if (warmupTime != null)
            {
                _warmupEndTime = Server.CurrentTime + warmupTime;
            }
            else
            {
                _warmupEndTime = Server.CurrentTime + 90; // default 90 seconds
            }
            _timer = AddTimer(1.0F, () =>
            {
                if (warmupTime <= 0)
                {
                    if (!_warmupEnded)
                    {
                        var remainingTime = _warmupEndTime - Server.CurrentTime;
                        if (remainingTime >= 0)
                        {
                            foreach (var player in Utilities.GetPlayers())
                            {
                                if (player.IsValid)
                                {
                                    player.PrintToCenter($"Warmup {remainingTime:F1}s");
                                }
                            }
                        }
                        else
                        {
                            Server.ExecuteCommand("mp_restartgame 15");
                            _warmupEnded = true;
                            _timer!.Kill();
                        }
                    }
                }
                else
                {
                    warmupTime--;
                }
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void OnTick()
        {
            if (_gameRules == null)
            {
                InitializeGameRules();
            }
            else
            {
                _gameRules.GameRestart = _gameRules.RestartRoundTime < Server.CurrentTime;
            }
        }
    }
}