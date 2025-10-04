using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.PSI.Environment;
using Game.Modding;
using Game.Settings;
using PrefabAssetFixes.Systems;
using StarQ.Shared.Extensions;
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

        [SettingsUIDisableByCondition(typeof(Setting), nameof(Has_RP_USSW))]
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

        [SettingsUIDisableByCondition(typeof(Setting), nameof(Has_RP_CN_or_USSW))]
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

        [Exclude]
        private bool _rs_clinic;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(Has_DLC_MA))]
        [SettingsUISection(OptionsTab, FunctionalGroup)]
        public bool RSClinic
        {
            get => _rs_clinic;
            set
            {
                _rs_clinic = value;
                assetFixSystem.FixRSClinic(value);
            }
        }

        [Exclude]
        private bool _lhtBusStation02;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool LHTBusStation02
        {
            get => _lhtBusStation02;
            set
            {
                _lhtBusStation02 = value;
                assetFixSystem.FixLHTBusStation02(value);
            }
        }

        [Exclude]
        private bool _lhtTaxiDepot01;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool LHTTaxiDepot01
        {
            get => _lhtTaxiDepot01;
            set
            {
                _lhtTaxiDepot01 = value;
                assetFixSystem.FixLHTTaxiDepot01(value);
            }
        }

        [Exclude]
        private bool _lhtTramDepot01;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool LHTTramDepot01
        {
            get => _lhtTramDepot01;
            set
            {
                _lhtTramDepot01 = value;
                assetFixSystem.FixLHTTramDepot01(value);
            }
        }

        [Exclude]
        private bool _lhtCargoHarbor01;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool LHTCargoHarbor01
        {
            get => _lhtCargoHarbor01;
            set
            {
                _lhtCargoHarbor01 = value;
                assetFixSystem.FixLHTCargoHarbor01(value);
            }
        }

        [Exclude]
        private bool _rhtBusStation01;

        [SettingsUISection(OptionsTab, VisualGroup)]
        public bool RHTBusStation01
        {
            get => _rhtBusStation01;
            set
            {
                _rhtBusStation01 = value;
                assetFixSystem.FixRHTBusStation01(value);
            }
        }

        [Exclude]
        private bool _nl_LowHousehold;

        [SettingsUIDisableByCondition(typeof(Setting), nameof(Has_DLC_MA))]
        [SettingsUISection(OptionsTab, FunctionalGroup)]
        public bool NLLowHousehold
        {
            get => _nl_LowHousehold;
            set
            {
                _nl_LowHousehold = value;
                assetFixSystem.FixNLLowHousehold(value);
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
                string logPath = $"{EnvPath.kUserDataPath}/Logs/{nameof(PrefabAssetFixes)}.log";
                string? logsDir = System.IO.Path.GetDirectoryName(logPath);

                try
                {
                    if (System.IO.File.Exists(logPath))
                    {
                        var psi = new ProcessStartInfo(logPath)
                        {
                            UseShellExecute = true,  // open with default editor
                            ErrorDialog = false,
                            Verb = "open"
                        };
                        Process.Start(psi);
                        return;
                    }

                    // No file yet — open the Logs folder instead
                    if (!string.IsNullOrEmpty(logsDir) && System.IO.Directory.Exists(logsDir))
                    {
                        var psi2 = new ProcessStartInfo(logsDir)
                        {
                            UseShellExecute = true,
                            ErrorDialog = false,
                            Verb = "open"
                        };
                        Process.Start(psi2);
                    }
                    else
                    {
                        LogHelper.SendLog("[OpenLog] No log file yet, and Logs folder not found.", LogLevel.Info);
                    }
                }
                catch (Exception ex)
                {
                    // Single catch handles Win32Exception and anything else safely
                    LogHelper.SendLog($"[OpenLog] Failed to open path: {ex.GetType().Name}: {ex.Message}", LogLevel.Warn);
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
            USSWHospital = true;
            SolarParking = true;
            RSClinic = true;
            LHTBusStation02 = true;
            LHTTaxiDepot01 = true;
            LHTTramDepot01 = true;
            LHTCargoHarbor01 = true;
            RHTBusStation01 = true;
            NLLowHousehold = true;
        }

        private bool Has_RP_CN()
        {
            return !assetFixSystem.IsActive(AssetFixSystem.PackType.RP_CN);
        }

        private bool Has_RP_USSW()
        {
            return !assetFixSystem.IsActive(AssetFixSystem.PackType.RP_USSW);
        }

        private bool Has_RP_CN_or_USSW()
        {
            return !(
                assetFixSystem.IsActive(AssetFixSystem.PackType.RP_CN)
                || assetFixSystem.IsActive(AssetFixSystem.PackType.RP_USSW)
            );
        }

        private bool Has_DLC_MA()
        {
            return !assetFixSystem.IsActive(AssetFixSystem.PackType.DLC_MA);
        }
    }
}
