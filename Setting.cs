using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.PSI.Environment;
using Game.Modding;
using Game.Settings;
using PrefabAssetFixes.Extensions;
using PrefabAssetFixes.Systems;
using Unity.Entities;
using UnityEngine.Device;

namespace PrefabAssetFixes
{
    [FileLocation(nameof(PrefabAssetFixes))]
    [SettingsUIGroupOrder(FunctionalGroup, VisualGroup, LogTab)]
    [SettingsUIShowGroupName(FunctionalGroup, VisualGroup)]
    public class Setting : ModSetting
    {
        [Exclude]
        public AssetFixSystem assetFixSystem =
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AssetFixSystem>();

        public const string OptionsTab = "OptionsTab";
        public const string FunctionalGroup = "Functional Fixes";
        public const string VisualGroup = "Visual Fixes";

        public const string AboutTab = "AboutTab";
        public const string InfoGroup = "Info";

        public const string LogTab = "LogTab";

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

        public bool DisabledTrue()
        {
            return true;
        }

        [Exclude]
        private bool _storage;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(DisabledTrue))]
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
        private bool _usswhospital;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool USSWHospital
        {
            get => _usswhospital;
            set
            {
                _usswhospital = value;
                assetFixSystem.FixUSSWHospital(value);
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

        [Exclude]
        private bool _solarParking;

        [SettingsUISection(OptionsTab, FunctionalGroup)]
        public bool SolarParking
        {
            get => _solarParking;
            set
            {
                _solarParking = value;
                assetFixSystem.FixParkingLotSolar(value);
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
                    LogHelper.SendLog(e);
                }
            }
        }

        [SettingsUIButtonGroup("Social")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool Discord
        {
            set
            {
                try
                {
                    Application.OpenURL(
                        $"https://discord.com/channels/1024242828114673724/1390407455522951228"
                    );
                }
                catch (Exception e)
                {
                    LogHelper.SendLog($"{e}");
                }
            }
        }

        [SettingsUIMultilineText]
        [SettingsUIDisplayName(typeof(LogHelper), nameof(LogHelper.LogText))]
        [SettingsUISection(LogTab, "")]
        public string LogText => string.Empty;

        [SettingsUISection(LogTab, "")]
        public bool OpenLog
        {
            set
            {
                Task.Run(() =>
                    Process.Start($"{EnvPath.kUserDataPath}/Logs/{nameof(PrefabAssetFixes)}.log")
                );
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
            USSWHospital = true;
            SolarParking = true;
        }
    }
}
