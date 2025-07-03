using System;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Game.Modding;
using Game.Settings;
using PrefabAssetFixes.Systems;
using Unity.Entities;
using UnityEngine.Device;

namespace PrefabAssetFixes
{
    [FileLocation(nameof(PrefabAssetFixes))]
    [SettingsUIGroupOrder(FunctionalGroup, VisualGroup)]
    [SettingsUIShowGroupName(FunctionalGroup, VisualGroup)]
    public class Setting : ModSetting
    {
        [Exclude]
        public AssetFixSystem assetFixSystem =
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AssetFixSystem>();

        public const string OptionsTab = "Options";
        public const string FunctionalGroup = "Functional Fixes";
        public const string VisualGroup = "Visual Fixes";

        public const string AboutTab = "About";
        public const string InfoGroup = "Info";

        public Setting(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        [Exclude]
        private bool _prisonVan;

        [SettingsUISection(OptionsTab, FunctionalGroup)]
        public bool PrisonVan
        {
            get => _prisonVan;
            set
            {
                _prisonVan = value;
                assetFixSystem.FixPrisonBus01(value);
            }
        }

        [Exclude]
        private bool _prison;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool Prison
        {
            get => _prison;
            set
            {
                _prison = value;
                assetFixSystem.FixPrison01(value);
            }
        }

        [Exclude]
        private bool _storage;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool Storage
        {
            get => _storage;
            set
            {
                _storage = value;
                assetFixSystem.FixStorageMissing(value, Recycling);
            }
        }

        [Exclude]
        private bool _recycling;

        [SettingsUISection(OptionsTab, FunctionalGroup)]
        public bool Recycling
        {
            get => _recycling;
            set
            {
                _recycling = value;
                assetFixSystem.FixStorageMissing(Storage, value);
            }
        }

        [Exclude]
        private bool _hospital;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool Hospital
        {
            get => _hospital;
            set
            {
                _hospital = value;
                assetFixSystem.FixHostipal01(value);
            }
        }

        [Exclude]
        private bool _hoveringPoles;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool HoveringPoles
        {
            get => _hoveringPoles;
            set
            {
                _hoveringPoles = value;
                assetFixSystem.FixHoveringPoles(value);
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
            Storage = false;
            Hospital = true;
            Recycling = true;
            HoveringPoles = true;
        }
    }
}
