using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using PrefabAssetFixes.Systems;
using Unity.Entities;

namespace PrefabAssetFixes
{
    public class Mod : IMod
    {
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
            //log.Info(nameof(OnLoad));

            //if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            //    log.Info($"Current mod asset at {asset.path}");
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AssetFixSystem>();

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

            AssetDatabase.global.LoadSettings(
                nameof(PrefabAssetFixes),
                m_Setting,
                new Setting(this)
            );
        }

        public void OnDispose()
        {
            //log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}
