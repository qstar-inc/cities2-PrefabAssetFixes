using System;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Game.Modding;
using Game.Settings;
using Game.UI;
using PrefabAssetFixes.Systems;
using Unity.Entities;
using UnityEngine.Device;

namespace PrefabAssetFixes
{
    [FileLocation(nameof(PrefabAssetFixes))]
    public class Setting : ModSetting
    {
        [Exclude]
        public AssetFixSystem assetFixSystem =
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AssetFixSystem>();

        public const string OptionsTab = "Options";
        public const string VanillaTab = "Vanilla";

        public const string AboutTab = "About";
        public const string InfoGroup = "Info";

        public Setting(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        private bool _prisonVan;

        [SettingsUISection(OptionsTab, VanillaTab)]
        public bool PrisonVan
        {
            get => _prisonVan;
            set
            {
                _prisonVan = value;
                assetFixSystem.FixPrisonBus01(value);
            }
        }

        private bool _prison;

        [SettingsUISection(OptionsTab, VanillaTab)]
        public bool Prison
        {
            get => _prison;
            set
            {
                _prison = value;
                assetFixSystem.FixPrison01(value);
            }
        }

        [SettingsUISection(AboutTab, InfoGroup)]
        public string ModState => Mod.State;

        [SettingsUISection(AboutTab, InfoGroup)]
        public string NameText => Mod.Name;

        [SettingsUISection(AboutTab, InfoGroup)]
        public string VersionText =>
#if DEBUG
            $"{Mod.Version} - DEV";
#else
            Mod.Version;
#endif

        [SettingsUISection(AboutTab, InfoGroup)]
        public string AuthorText => "StarQ";

        [SettingsUIButtonGroup("Social")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool BMaCLink
        {
            set
            {
                try
                {
                    Application.OpenURL($"https://buymeacoffee.com/starq");
                }
                catch (Exception e)
                {
                    Mod.log.Info(e);
                }
            }
        }

        public override void SetDefaults()
        {
            Prison = true;
            PrisonVan = true;
        }
    }
}
