using System.Collections.Generic;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using PrefabAssetFixes.Systems;
using StarQ.Shared.Extensions;

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

        //public static string supportedGameVersion = "1.5.5f1";
        public static ModState modState = ModState.None;

        public void OnLoad(UpdateSystem updateSystem)
        {
            LogHelper.Init(Id, log);
            LocaleHelper.Init(Id, Name, GetReplacements);

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();

            AssetDatabase.global.LoadSettings(Id, m_Setting, new Setting(this));

            AssetFixSystem.systemReady = true;

            updateSystem.UpdateBefore<AssetFixSystem, Game.Serialization.ElectricityGraphSystem>(
                SystemUpdatePhase.Deserialize
            );
            modState = ModState.Ready;
            UpdateState();

            //if (!Game.Version.current.shortVersion.StartsWith(supportedGameVersion))
            //{
            //    LogHelper.SendLog(
            //        $"{Game.Version.current.shortVersion} is not {supportedGameVersion}"
            //    );
            //    //World
            //    //    .DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AssetFixSystem>()
            //    //    .Enabled = false;
            //    modState = ModState.Incompatible;
            //    UpdateState();
            //    //AssetFixSystem.systemDisposed = true;
            //}
        }

        public void OnDispose()
        {
            m_Setting?.UnregisterInOptionsUI();
            m_Setting = null;
        }

        public static Dictionary<string, string> GetReplacements()
        {
            return new()
            {
                //{ "CurrentVersion", Game.Version.current.shortVersion },
                //{ "ModVersion", Version },
                //{ "FixedVersion", supportedGameVersion },
            };
        }

        public static void UpdateState()
        {
            ModState ms = modState;
            int changeCount = AssetFixSystem.changeCount;
            int vanillaCount = AssetFixSystem.totalCount;

            if (!AssetFixSystem.firstPass)
            {
                if (changeCount == 0)
                    modState = ModState.SetNone;
                else if (changeCount == vanillaCount)
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
                //case ModState.Incompatible:
                //    state = $"Incompatible";
                //    break;
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
            string stateText = LocaleHelper.Translate($"{Id}.State.{state}");

            if (ms == ModState.SetNone || ms == ModState.SetSome || ms == ModState.SetAll)
            {
                stateText = $"{stateText} ({changeCount})";
            }

            State = stateText;
        }
    }
}
