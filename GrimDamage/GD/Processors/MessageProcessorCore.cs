﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using EvilsoftCommons;
using EvilsoftCommons.DllInjector;
using EvilsoftCommons.Exceptions;
using GrimDamage.GD.Dto;
using GrimDamage.Parser.Service;
using GrimDamage.Settings;
using GrimDamage.Statistics.Service;
using log4net;
// ReSharper disable NotAccessedField.Local

namespace GrimDamage.GD.Processors {
    class MessageProcessorCore : IDisposable {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MessageProcessorCore));
        private RegisterWindow _window;
        private InjectionHelper _injector;
        private ProgressChangedEventHandler _injectorCallbackDelegate;
        private bool _isFirstMessage = true;
        private readonly Action<RegisterWindow.DataAndType> _registerWindowDelegate;
        private readonly List<IMessageProcessor> _processors;
        private readonly AppSettings _appSettings;

        public delegate void HookActivationCallback(object sender, EventArgs e);

        public event HookActivationCallback OnHookActivation;

        public MessageProcessorCore(
            DamageParsingService damageParsingService, 
            PositionTrackerService positionTrackerService,
            GeneralStateService generalStateService,
            AppSettings appSettings
        ) {
            _processors = new List<IMessageProcessor> {
                new GdLogMessageProcessor(appSettings, damageParsingService),
                new PlayerPositionTrackerProcessor(positionTrackerService, appSettings),
                new GdGameEventProcessor(generalStateService),
                new PlayerDetectionProcessor(damageParsingService, appSettings),
                new DetectPlayerHitpointsProcessor(damageParsingService, appSettings),
                new PlayerResistMonitor(damageParsingService, appSettings)
            };

            _registerWindowDelegate = CustomWndProc;
            _injectorCallbackDelegate = InjectorCallback;
            _window = new RegisterWindow("GDDamageWindowClass", _registerWindowDelegate);
            _injector = new InjectionHelper(new BackgroundWorker(), _injectorCallbackDelegate, false, "Grim Dawn", string.Empty, "Hook.dll");
            _appSettings = appSettings;
        }


        private void CustomWndProc(RegisterWindow.DataAndType bt) {
            if (Thread.CurrentThread.Name == null) {
                Thread.CurrentThread.Name = "Core";
                ExceptionReporter.EnableLogUnhandledOnThread();
            }

            if (_isFirstMessage) {
                Logger.Debug("Window message received");
                _isFirstMessage = false;
                OnHookActivation?.Invoke(null, null);
            }


            MessageType type = (MessageType) bt.Type;
            foreach (var processor in _processors) {
                if (processor.Process(type, bt.Data)) {
                    if (_appSettings.LogProcessedMessages) {
                        Logger.Debug($"Processor {processor.GetType().ToString()} handled message");
                    }
                    return;
                }
            }
            
            if (bt.Type == 999111) { // apply-damage experimental
                Logger.Debug("CM:TakeAttack()");
            }
            else if (bt.Type == 929002) {
                Logger.Debug("CharacterBio::GetAllDefenseAttributes()");
            }
            else if (bt.Type == 929022) {

                //Logger.Debug("CharAttributeAccumulator::ExecuteDefense()");
            }
            else if (bt.Type == 929222) {
                int ptr = IOHelper.GetInt(bt.Data, 0);
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
                Logger.Fatal($"DECONSTRUCTION! {ptr}");
            }
            else if (bt.Type == 999001) {
                Logger.Debug("Character::TakeAttack()");
            }
            else if (bt.Type == 929003) {
                Logger.Debug("ItemEquipment::GetAllDefenseAttributes()");
            }
            else if (bt.Type == 929001) {
                int entityId = IOHelper.GetInt(bt.Data, 0);
                float fire = IOHelper.GetFloat(bt.Data, 4);
                float cold = IOHelper.GetFloat(bt.Data, 8);
                float lightning = IOHelper.GetFloat(bt.Data, 12);
                float poison = IOHelper.GetFloat(bt.Data, 16);
                float pierce = IOHelper.GetFloat(bt.Data, 20);
                float bleed = IOHelper.GetFloat(bt.Data, 24);
                float vitality = IOHelper.GetFloat(bt.Data, 28);
                float chaos = IOHelper.GetFloat(bt.Data, 32);
                float aether = IOHelper.GetFloat(bt.Data, 36);
                Logger.Debug($"CombatAttributeAccumulator::ProcessDefenseMethod() =>  {entityId} with Fire:{fire}, Cold:{cold}, Lightning:{lightning}, Poison:{poison}, Pierce:{pierce}, Bleed:{bleed}, Vitality:{vitality}, Chaos:{chaos}, Aether:{aether}");
            }
            else if (bt.Type == 10101012) { // apply-damage experimental
                // This produces exact matches to "^y    Damage 0,474187463521957 to Defender 0x101028 (Vitality)" // Just not the defender
                // ^bShield: Reduced (75,5949020385742) Damage by (5,33183132285327E-315%) percent, remaining damage (12)
                // Lists the 75, but not the 12
                float f = IOHelper.GetFloat(bt.Data, 0);
                int playStatsDamageType = IOHelper.GetInt(bt.Data, 4);
                int combatAttributeType = IOHelper.GetInt(bt.Data, 8);
                Logger.Debug($"ApplyDamage({f}, {playStatsDamageType}, {combatAttributeType})");
            }
            else if (bt.Type == 999003) {
                //Logger.Debug($"SkillManager::GetDefenseAttributes called for {IOHelper.GetInt(bt.Data, 0)}");
                int entityId = IOHelper.GetInt(bt.Data, 0);
                float fire = IOHelper.GetFloat(bt.Data, 4);
                float cold = IOHelper.GetFloat(bt.Data, 8);
                float lightning = IOHelper.GetFloat(bt.Data, 12);
                float poison = IOHelper.GetFloat(bt.Data, 16);
                float pierce = IOHelper.GetFloat(bt.Data, 20);
                float bleed = IOHelper.GetFloat(bt.Data, 24);
                float vitality = IOHelper.GetFloat(bt.Data, 28);
                float chaos = IOHelper.GetFloat(bt.Data, 32);
                float aether = IOHelper.GetFloat(bt.Data, 36);
                Logger.Debug($"SkillManager::GetDefenseAttributes called for {entityId} with Fire:{fire}, Cold:{cold}, Lightning:{lightning}, Poison:{poison}, Pierce:{pierce}, Bleed:{bleed}, Vitality:{vitality}, Chaos:{chaos}, Aether:{aether}");
            }

            else if (bt.Type == 999002) {
                int entityId = IOHelper.GetInt(bt.Data, 0);
                float fire = IOHelper.GetFloat(bt.Data, 4);
                float cold = IOHelper.GetFloat(bt.Data, 8);
                float lightning = IOHelper.GetFloat(bt.Data, 12);
                float poison = IOHelper.GetFloat(bt.Data, 16);
                float pierce = IOHelper.GetFloat(bt.Data, 20);
                float bleed = IOHelper.GetFloat(bt.Data, 24);
                float vitality = IOHelper.GetFloat(bt.Data, 28);
                float chaos = IOHelper.GetFloat(bt.Data, 32);
                float aether = IOHelper.GetFloat(bt.Data, 36);
                Logger.Debug($"Character::GetAllDefenseAttributes called for {entityId} with Fire:{fire}, Cold:{cold}, Lightning:{lightning}, Poison:{poison}, Pierce:{pierce}, Bleed:{bleed}, Vitality:{vitality}, Chaos:{chaos}, Aether:{aether}");
            }
            else {

                Logger.Warn($"Got a message of type {bt.Type}");
            }
        }

        private void InjectorCallback(object sender, ProgressChangedEventArgs e) {
            //Logger.Debug("Injector callback");
        }

        public void Dispose() {
            _injector?.Dispose();
            _injector = null;
        }
    }
}
