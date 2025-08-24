using System.Reflection;
using Colossal.IO.AssetDatabase;
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

        public static ILog log = LogManager
            .GetLogger($"{nameof(PrefabAssetFixes)}")
            .SetShowsErrorsInUI(false);

        public static Setting m_Setting;
        public static string State = "";

        public void OnLoad(UpdateSystem updateSystem)
        {
            State = "Ready";

            foreach (var item in new LocaleHelper($"{Id}.Locale.json").GetAvailableLanguages())
            {
                GameManager.instance.localizationManager.AddSource(item.LocaleId, item);
            }

            AssetFixSystem afs =
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AssetFixSystem>();

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();

            AssetDatabase.global.LoadSettings(Id, m_Setting, new Setting(this));

            afs.systemReady = true;
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
