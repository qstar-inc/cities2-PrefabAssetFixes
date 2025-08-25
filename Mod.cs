using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using PrefabAssetFixes.Extensions;
using PrefabAssetFixes.Systems;
using Unity.Entities;

namespace PrefabAssetFixes
{
    public class Mod : IMod
    {
        public static string Id = nameof(PrefabAssetFixes);
        public static string Name = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyTitleAttribute>()
            .Title;
        public static string Version = Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version.ToString(3);

        public static ILog log = LogManager.GetLogger($"{Id}").SetShowsErrorsInUI(false);

        public static Setting m_Setting;
        public static string State = "";
        public static string supportedGameVersion = "1.3.3f1";
        public static ModState modState = ModState.None;

        public void OnLoad(UpdateSystem updateSystem)
        {
            foreach (var item in new LocaleHelper($"{Id}.Locale.json").GetAvailableLanguages())
            {
                GameManager.instance.localizationManager.AddSource(item.LocaleId, item);
            }

            GameManager.instance.localizationManager.onActiveDictionaryChanged +=
                OnActiveDictionaryChanged;

            OnActiveDictionaryChanged();

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();

            AssetDatabase.global.LoadSettings(Id, m_Setting, new Setting(this));

            AssetFixSystem.systemReady = true;
            modState = ModState.Ready;
            UpdateState();

            if (!Game.Version.current.shortVersion.StartsWith("1.3.3f1"))
            {
                log.Info(
                    $"Disabling mod because {Game.Version.current.shortVersion} is not {supportedGameVersion}"
                );
                World
                    .DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AssetFixSystem>()
                    .Enabled = false;
                modState = ModState.Incompatible;
                UpdateState();
                AssetFixSystem.systemDisposed = true;
                return;
            }

            //updateSystem.UpdateAt<AssetFixSystem>(SystemUpdatePhase.Modification3);
        }

        public static void OnActiveDictionaryChanged()
        {
            LocalizationManager lm = GameManager.instance.localizationManager;
            Dictionary<string, string> toUpdate = new();

            Dictionary<string, string> replacements = new()
            {
                { "currentVersion", Game.Version.current.shortVersion },
                { "modVersion", Version },
                { "fixedVersion", supportedGameVersion },
            };

            Regex regex = new(@"\{(\w+)\}", RegexOptions.Compiled);

            foreach (var entry in lm.activeDictionary.entries)
            {
                string newValue = regex.Replace(
                    entry.Value,
                    match =>
                    {
                        var key = match.Groups[1].Value;
                        return replacements.TryGetValue(key, out var replacement)
                            ? replacement
                            : match.Value;
                    }
                );
                if (newValue != entry.Value)
                {
                    toUpdate[entry.Key] = newValue;
                }
            }

            foreach (var item in toUpdate)
            {
                try
                {
                    lm.activeDictionary.Add(item.Key, item.Value);
                }
                catch (Exception) { }
            }
            UpdateState();
        }

        public static void UpdateState()
        {
            ModState ms = modState;
            int changeCount = AssetFixSystem.changeCount;
            if (!AssetFixSystem.firstPass)
            {
                if (changeCount == 0)
                    modState = ModState.SetNone;
                else if (changeCount == 7)
                    modState = ModState.SetAll;
                else
                    modState = ModState.SetSome;
            }
            string state = "";
            switch (ms)
            {
                case ModState.None:
                    break;
                case ModState.Ready:
                    state = $"Ready";
                    break;
                case ModState.Incompatible:
                    state = $"Incompatible";
                    break;
                case ModState.SetNone:
                    state = $"SetNone";
                    break;
                case ModState.SetSome:
                    state = $"SetSome";
                    break;
                case ModState.SetAll:
                    state = "SetAll";
                    break;
                default:
                    break;
            }
            string stateText = LocaleHelper.Translate($"{Id}.Mod.State.{state}");

            if (ms == ModState.SetNone || ms == ModState.SetSome || ms == ModState.SetAll)
            {
                stateText = $"{stateText} ({changeCount})";
            }

            State = stateText;
        }

        public void OnDispose()
        {
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}
