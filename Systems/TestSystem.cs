# if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossal.Entities;
using Game;
using Game.Companies;
using Game.Prefabs;
using StarQ.Shared.Extensions;
using Unity.Entities;

namespace PrefabAssetFixes.Systems
{
    public partial class TestSystem : GameSystemBase
    {
        protected override void OnUpdate() { }

        public void Test()
        {
            TestPrison01();
            TestHospital01();
            TestHoveringPoles();
            TestUSSWHospital();
            TestParkingLotSolar();
            TestRSClinic();
            TestLHTBusStation02();
            TestLHTTaxiDepot01();
            TestLHTTramDepot01();
            TestLHTCargoHarbor01();
            TestRHTBusStation01();
            TestNLLowHousehold();
            TestAdditionalTransformers();
            TestFRCityHall();
        }

        public void TestPrison01()
        {
            LogHelper.SendLog("Testing Prison01");
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "Prison01"),
                    out Entity selectedPrefab
                ) && EntityManager.TryGetComponent(selectedPrefab, out PrisonData pvd)
            )
            {
                if (pvd.m_PrisonVanCapacity == 10)
                {
                    LogHelper.SendLog("Prison01 has 10 vehicles (default)");
                }
                else if (pvd.m_PrisonVanCapacity == 20)
                {
                    LogHelper.SendLog("Prison01 has 20 vehicles (modified)");
                }
                else
                {
                    LogHelper.SendLog(
                        $"Prison01 has {pvd.m_PrisonVanCapacity} vehicles (incorrect)"
                    );
                }
            }
        }

        public void TestHospital01()
        {
            LogHelper.SendLog("Testing Hospital01");
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "Hospital01"),
                    out Entity selectedPrefab
                )
                && EntityManager.TryGetBuffer(
                    selectedPrefab,
                    false,
                    out DynamicBuffer<SubObject> hospitalObj
                )
            )
            {
                bool hasParking = false;
                for (int i = hospitalObj.Length - 1; i >= 00; i--)
                {
                    var obj = hospitalObj[i];
                    if (
                        obj.m_Prefab == AssetFixSystem.parking04Ent
                        && Math.Round(obj.m_Position.x) == -82f
                    )
                    {
                        hasParking = true;
                        LogHelper.SendLog("Hospital01 has parking is for public (default)");
                        break;
                    }
                    else if (
                        obj.m_Prefab == AssetFixSystem.parkingService04Ent
                        && Math.Round(obj.m_Position.x) == -82f
                    )
                    {
                        hasParking = true;
                        LogHelper.SendLog("Hospital01 has parking is for service (modified)");
                        break;
                    }
                }
                if (!hasParking)
                {
                    LogHelper.SendLog(
                        "Hospital01 does not have the right side parking (incorrect)"
                    );
                }
            }
        }

        public void TestHoveringPoles()
        {
            LogHelper.SendLog("Testing CN_OfficeHighA_L1_6x6");
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "CN_OfficeHighA_L1_6x6"),
                    out Entity selectedPrefab1
                )
                && EntityManager.TryGetBuffer(
                    selectedPrefab1,
                    false,
                    out DynamicBuffer<SubObject> obj1
                )
            )
            {
                bool hasFlags = false;
                for (int i = obj1.Length - 1; i >= 00; i--)
                {
                    var obj = obj1[i];
                    if (
                        obj.m_Prefab == AssetFixSystem.FlagPoleCommercial02Entity
                        && Math.Round(obj.m_Position.y) == 135f
                    )
                    {
                        hasFlags = true;
                        LogHelper.SendLog("CN_OfficeHighA_L1_6x6 has flagpole higher (default)");
                    }
                    else if (
                        obj.m_Prefab == AssetFixSystem.FlagPoleCommercial02Entity
                        && Math.Round(obj.m_Position.y) == 124f
                    )
                    {
                        hasFlags = true;
                        LogHelper.SendLog("CN_OfficeHighA_L1_6x6 has flagpole lowered (modified)");
                    }
                }
                if (!hasFlags)
                {
                    LogHelper.SendLog(
                        "CN_OfficeHighA_L1_6x6 does not have the flags in the expected position (incorrect)"
                    );
                }
            }

            LogHelper.SendLog("Testing USSW_CommercialLow100_L1_6x4");
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "USSW_CommercialLow100_L1_6x4"),
                    out Entity selectedPrefab2
                )
                && EntityManager.TryGetBuffer(
                    selectedPrefab2,
                    false,
                    out DynamicBuffer<SubObject> obj2
                )
            )
            {
                bool hasAds = false;
                for (int i = obj2.Length - 1; i >= 00; i--)
                {
                    var obj = obj2[i];
                    if (
                        obj.m_Prefab == AssetFixSystem.BillboardWallMedium01Entity
                        && obj.m_Position.y != 1.803f
                    )
                    {
                        hasAds = true;
                        LogHelper.SendLog(
                            "USSW_CommercialLow100_L1_6x4 has BillboardWallMedium01Entity higher (default)"
                        );
                    }
                    else if (
                        obj.m_Prefab == AssetFixSystem.BillboardWallMedium01Entity
                        && obj.m_Position.y == 1.803f
                    )
                    {
                        hasAds = true;
                        LogHelper.SendLog(
                            "USSW_CommercialLow100_L1_6x4 has BillboardWallMedium01Entity lowered (modified)"
                        );
                    }
                    else if (
                        (
                            obj.m_Prefab == AssetFixSystem.Screen01Entity
                            || obj.m_Prefab == AssetFixSystem.Screen02Entity
                        )
                        && obj.m_Position.y != 0.003f
                    )
                    {
                        hasAds = true;
                        LogHelper.SendLog(
                            "USSW_CommercialLow100_L1_6x4 has Screen0XEntity higher (default)"
                        );
                    }
                    else if (
                        (
                            obj.m_Prefab == AssetFixSystem.Screen01Entity
                            || obj.m_Prefab == AssetFixSystem.Screen02Entity
                        )
                        && obj.m_Position.y == 0.003f
                    )
                    {
                        hasAds = true;
                        LogHelper.SendLog(
                            "USSW_CommercialLow100_L1_6x4 has Screen0XEntity lowered (modified)"
                        );
                    }
                }
                if (!hasAds)
                {
                    LogHelper.SendLog(
                        "USSW_CommercialLow100_L1_6x4 does not have the ads in the expected position (incorrect)"
                    );
                }
            }
        }

        public void TestUSSWHospital()
        {
            LogHelper.SendLog("USSWHospital is not testable");
        }

        public void TestParkingLotSolar()
        {
            LogHelper.SendLog("Testing ParkingLotSolar");
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "ParkingLot12"),
                    out Entity ParkingLot12Entity
                )
            )
            {
                if (!EntityManager.HasComponent<SolarPoweredData>(ParkingLot12Entity))
                {
                    LogHelper.SendLog("ParkingLot12 has solar panels (default)");
                }
                else
                {
                    LogHelper.SendLog("ParkingLot12 has solar panels (modified)");
                }
            }
        }

        public void TestRSClinic()
        {
            LogHelper.SendLog("Testing RS_MedicalClinic01");
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "RS_MedicalClinic01"),
                    out Entity RS_MedicalClinic01Entity
                )
                && EntityManager.TryGetComponent(
                    RS_MedicalClinic01Entity,
                    out StorageLimitData storage1
                )
            )
            {
                if (storage1.m_Limit == 100)
                {
                    LogHelper.SendLog("RS_MedicalClinic01 has 0.1 tonne storage (default)");
                }
                else if (storage1.m_Limit == 1000)
                {
                    LogHelper.SendLog("RS_MedicalClinic01 has 1 tonne storage (modified)");
                }
                else
                {
                    LogHelper.SendLog(
                        $"RS_MedicalClinic01 has {storage1.m_Limit / 1000} tonne storage (incorrect)"
                    );
                }
            }
        }

        public void TestLHTBusStation02()
        {
            LogHelper.SendLog("LHTBusStation02 is not testable");
        }

        public void TestLHTTaxiDepot01()
        {
            LogHelper.SendLog("LHTTaxiDepot01 is not testable");
        }

        public void TestLHTTramDepot01()
        {
            LogHelper.SendLog("LHTTramDepot01 is not testable");
        }

        public void TestLHTCargoHarbor01()
        {
            LogHelper.SendLog("LHTCargoHarbor01 is not testable");
        }

        public void TestRHTBusStation01()
        {
            LogHelper.SendLog("RHTBusStation01 is not testable");
        }

        public void TestNLLowHousehold()
        {
            LogHelper.SendLog("Testing NLLowHousehold");
            if (
                !PrefabHelper.TryGetEntity(
                    new PrefabID("ZonePrefab", "NL Residential Gambrel Low"),
                    out Entity zoneEntity1
                ) || !EntityManager.TryGetComponent(zoneEntity1, out ZonePropertiesData zoneProps1)
            )
                return;

            if (zoneProps1.m_ResidentialProperties == 1)
            {
                LogHelper.SendLog("NL Residential Gambrel Low has 1 household (default)");
            }
            else if (zoneProps1.m_ResidentialProperties == 2)
            {
                LogHelper.SendLog("NL Residential Gambrel Low has 2 households (modified)");
            }
            else
            {
                LogHelper.SendLog(
                    $"NL Residential Gambrel Low has {zoneProps1.m_ResidentialProperties} households (incorrect)"
                );
            }
        }

        public void TestAdditionalTransformers()
        {
            LogHelper.SendLog("Testing AdditionalTransformers");
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(
                        nameof(BuildingPrefab),
                        "SolarPowerStation01 Additional Transformer Station"
                    ),
                    out Entity additionalTransformerEntity
                )
                && EntityManager.TryGetBuffer(
                    additionalTransformerEntity,
                    false,
                    out DynamicBuffer<ServiceUpgradeBuilding> serviceUpgradeBuilding
                )
            )
            {
                if (serviceUpgradeBuilding.Length == 1)
                {
                    LogHelper.SendLog(
                        "SolarPowerStation01 Additional Transformer Station has no additional transformers (default)"
                    );
                }
                else if (serviceUpgradeBuilding.Length == 3)
                {
                    LogHelper.SendLog(
                        "SolarPowerStation01 Additional Transformer Station has 2 additional transformers (modified)"
                    );
                }
                else
                {
                    LogHelper.SendLog(
                        $"SolarPowerStation01 Additional Transformer Station has {serviceUpgradeBuilding.Length} additional transformers (incorrect)"
                    );
                }
            }
        }

        public void TestFRCityHall()
        {
            LogHelper.SendLog("Testing FRCityHall");
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "FR_CityHall01"),
                    out Entity frCityHallEntity
                )
            )
            {
                if (!EntityManager.HasBuffer<UnlockOnBuildData>(frCityHallEntity))
                {
                    LogHelper.SendLog("FR_CityHall01 does not have UnlockOnBuildData (default)");
                }
                else
                {
                    LogHelper.SendLog("FR_CityHall01 have UnlockOnBuildData (modified)");
                }
            }
        }
    }
}

#endif
