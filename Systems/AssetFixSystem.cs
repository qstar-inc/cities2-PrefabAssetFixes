using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Colossal;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Reflection;
using Colossal.Serialization.Entities;
using Colossal.UI;
using Game;
using Game.Assets;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Settings;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Game.UI.Localization;
using Game.UI.Menu;
using StarQ.Shared.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PrefabAssetFixes.Systems
{
    public enum ModState
    {
        None,
        Ready,
        Incompatible,
        SetNone,
        SetSome,
        SetAll,
    }

    public partial class AssetFixSystem : GameSystemBase
    {
#nullable disable
        public PrefabSystem prefabSystem;
#nullable enable
        public static int changeCount = 0;
        public static int totalCount = 0;

        public static bool firstPass = true;
        public static bool systemDisposed = false;
        public static bool systemReady = false;

        private readonly Dictionary<string, float> polePositions = new();

        private bool isPrisonSet = false;
        private bool isHospitalSet = false;
        private bool isPolesSet = false;
        private bool isUSSWHospitalSet = false;
        private bool isSolarParkingSet = false;
        private bool isRSClinicSet = false;
        private bool isLHTBusStation02Set = false;
        private bool isLHTTaxiDepot01Set = false;
        private bool isLHTTramDepot01Set = false;
        private bool isLHTCargoHarbor01Set = false;
        private bool isRHTBusStation01Set = false;
        private bool isNLLowHouseholdSet = false;
        private bool isFRCityHallSet = false;
        private bool isAdditionalTransformersSet = false;
        private bool isIndustrialCompanyWorkerUpdated = false;

        private bool hasRP_FR;
        private bool hasRP_CN;
        private bool hasRP_USSW;
        private bool hasDLC_MA;
        private bool hasRP_NL;

        public static Entity FlagPoleCommercial02Entity;
        public static Entity Screen01Entity;
        public static Entity Screen02Entity;
        public static Entity BillboardWallMedium01Entity;
        public static Entity CarSpawnMarker;
        public static Entity parkingService04Ent;
        public static Entity parking04Ent;

        public enum PackType
        {
            RP_FR,
            RP_CN,
            RP_USSW,
            DLC_MA,
            RP_NL,
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            prefabSystem = WorldHelper.PrefabSystem;
            ModHelper.AddAfterActivePlaysetOrModStatusChanged(CheckCount);
            CheckCount();
            Enabled = true;
        }

        public bool IsActive(PackType packType)
        {
            return packType switch
            {
                PackType.RP_FR => hasRP_FR,
                PackType.RP_CN => hasRP_CN,
                PackType.RP_USSW => hasRP_USSW,
                PackType.DLC_MA => hasDLC_MA,
                PackType.RP_NL => hasRP_NL,
                _ => false,
            };
        }

        private void CheckCount()
        {
            totalCount = 12;
            // prison + prison van + recycling + hospital + solar parking +
            // lht bus station 2 + lht taxi depot + lht tram depot +
            // lht cargo harbor + rht bus station 1 + additional transformers +
            // ind company worker

            totalCount -= 2;
            // prison van + recycling

            totalCount -= 1;
            // solar parking

            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID(nameof(AssetPackPrefab), "FR Pack Filter"),
                    out PrefabBase _
                )
            )
            {
                hasRP_FR = true;
                totalCount++;
                LogHelper.SendLog("Found RP_FR Pack", LogLevel.DEVD);
            }
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID(nameof(BuildingPrefab), "CN_OfficeHighA_L1_6x6"),
                    out PrefabBase _
                )
            )
            {
                hasRP_CN = true;
                totalCount++;
                LogHelper.SendLog("Found RP_CN Pack", LogLevel.DEVD);

                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(StaticObjectPrefab), "FlagPoleCommercial02"),
                    out FlagPoleCommercial02Entity
                );
            }
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID(nameof(BuildingPrefab), "USSW_Hospital_16x14"),
                    out PrefabBase _
                )
            )
            {
                hasRP_USSW = true;
                totalCount++;
                LogHelper.SendLog("Found RP_USSW Pack", LogLevel.DEVD);

                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(StaticObjectPrefab), "Screen01"),
                    out Screen01Entity
                );

                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(StaticObjectPrefab), "Screen02"),
                    out Screen02Entity
                );

                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(StaticObjectPrefab), "BillboardWallMedium01"),
                    out BillboardWallMedium01Entity
                );
            }
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID(nameof(BuildingPrefab), "RS_MedicalClinic01"),
                    out PrefabBase _
                )
            )
            {
                hasDLC_MA = true;
                totalCount++;
                LogHelper.SendLog("Found DLC_MA Pack", LogLevel.DEVD);
            }
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID(nameof(AssetPackPrefab), "NL Pack Filter"),
                    out PrefabBase _
                )
            )
            {
                hasRP_NL = true;
                totalCount++;
                LogHelper.SendLog("Found RP_NL Pack", LogLevel.DEVD);
            }

            PrefabHelper.TryGetEntity(
                new PrefabID(nameof(MarkerObjectPrefab), "Car Spawn Location"),
                out CarSpawnMarker
            );
            PrefabHelper.TryGetEntity(
                new PrefabID(nameof(StaticObjectPrefab), "ParkingLotDecal04"),
                out parking04Ent
            );
            PrefabHelper.TryGetEntity(
                new PrefabID(nameof(StaticObjectPrefab), "ParkingLotServiceDecal04"),
                out parkingService04Ent
            );
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            Enabled = true;
            base.OnGameLoadingComplete(purpose, mode);
            if (firstPass)
                CheckCount();
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            CheckCount();
            if (systemDisposed || prefabSystem == null)
                return;

            if (!systemReady)
                return;

            if (mode == GameMode.Editor)
            {
                FixPrison01(false);
                FixHostipal01(false);
                FixHoveringPoles(false);
                FixUSSWHospital(false);
                //FixParkingLotSolar(false);
                FixRSClinic(false);
                FixLHTBusStation02(false);
                FixLHTTaxiDepot01(false);
                FixLHTTramDepot01(false);
                FixLHTCargoHarbor01(false);
                FixRHTBusStation01(false);
                FixNLLowHousehold(false);
                FixAdditionalTransformers(false);
                FixFRCityHall(false);
                UpdateIndustrialWorker(false, 1);
            }
            if (mode != GameMode.Game)
                return;

            firstPass = false;
            StartFixes();
            SetState();
            base.OnGamePreload(purpose, mode);
        }

        private void StartFixes()
        {
            if (!systemReady)
                return;
            LogHelper.SendLog("Starting Fixes");
            Setting settings = Mod.m_Setting;
            if (settings.Prison)
                FixPrison01();
            if (settings.Hospital)
                FixHostipal01();
            if (settings.HoveringPoles && (IsActive(PackType.RP_CN) || IsActive(PackType.RP_USSW)))
                FixHoveringPoles();
            if (settings.USSWHospital && IsActive(PackType.RP_USSW))
                FixUSSWHospital();
            //if (settings.SolarParking)
            //    FixParkingLotSolar();
            if (settings.RSClinic && IsActive(PackType.DLC_MA))
                FixRSClinic();
            if (settings.LHTBusStation02)
                FixLHTBusStation02();
            if (settings.LHTTaxiDepot01)
                FixLHTTaxiDepot01();
            if (settings.LHTTramDepot01)
                FixLHTTramDepot01();
            if (settings.LHTCargoHarbor01)
                FixLHTCargoHarbor01();
            if (settings.RHTBusStation01)
                FixRHTBusStation01();
            if (settings.NLLowHousehold && IsActive(PackType.RP_NL))
                FixNLLowHousehold();
            if (settings.AdditionalTransformers)
                FixAdditionalTransformers();
            if (settings.FRCiltyHall && IsActive(PackType.RP_FR))
                FixFRCityHall();
            if (settings.IndustrialCompanyWorker != 1)
                UpdateIndustrialWorker(true, settings.IndustrialCompanyWorker);
        }

        protected override void OnUpdate()
        {
            FixSaveFromSolar();
            Enabled = false;
        }

        public static void SetState()
        {
            if (
                Mod.modState == ModState.None
                //|| Mod.modState == ModState.Incompatible
                || firstPass
                || systemDisposed
                || !systemReady
            )
                return;
            Mod.UpdateState();
            LogHelper.SendLog($"{changeCount} changes set");
        }

        public void FixPrison01(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isPrisonSet)
                return;
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "Prison01"),
                    out Entity selectedPrefab
                ) && EntityManager.TryGetComponent(selectedPrefab, out PrisonData pvd)
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    pvd.m_PrisonVanCapacity = 10;
                    changeCount--;
                    isPrisonSet = false;
                    changed = true;
                }
                else if (active)
                {
                    pvd.m_PrisonVanCapacity = 20;
                    changeCount++;
                    isPrisonSet = true;
                    changed = true;
                }
                if (changed)
                {
                    EntityManager.SetComponentData(selectedPrefab, pvd);
                    LogHelper.SendLog("Prison01 fix completed", LogLevel.DEVD);
                    SetState();
                }
            }
        }

        public void FixHostipal01(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isHospitalSet)
                return;

            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "Hospital01"),
                    out Entity selectedEntity
                )
                && EntityManager.TryGetBuffer(
                    selectedEntity,
                    false,
                    out DynamicBuffer<SubObject> hospitalObj
                )
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    for (int i = hospitalObj.Length - 1; i >= 00; i--)
                    {
                        var obj = hospitalObj[i];
                        if (
                            obj.m_Prefab == parkingService04Ent
                            && Math.Round(obj.m_Position.x) == -82f
                        )
                        {
                            obj.m_Prefab = parking04Ent;
                            hospitalObj[i] = obj;
                        }
                    }
                    changeCount--;
                    isHospitalSet = false;
                    changed = true;
                }
                else if (active)
                {
                    for (int i = hospitalObj.Length - 1; i >= 00; i--)
                    {
                        var obj = hospitalObj[i];
                        if (obj.m_Prefab == parking04Ent && Math.Round(obj.m_Position.x) == -82f)
                        {
                            obj.m_Prefab = parkingService04Ent;
                            hospitalObj[i] = obj;
                        }
                    }
                    changeCount++;
                    isHospitalSet = true;
                    changed = true;
                }
                if (changed)
                {
                    LogHelper.SendLog("Hostipal01 fix completed", LogLevel.DEVD);
                    SetState();
                }
            }
        }

        public void FixHoveringPoles(bool active = true)
        {
            if (!systemReady)
                return;
            if (!(IsActive(PackType.RP_USSW) || IsActive(PackType.RP_CN)))
                return;
            if (!active && !isPolesSet)
                return;

            List<string> prefabNames = new()
            {
                "CN_OfficeHighA_L1_6x6",
                "CN_OfficeHighA_L2_6x6",
                "USSW_CommercialLow01_L1_6x6",
                "USSW_CommercialLow01_L2_6x6",
                "USSW_CommercialLow01_L3_6x6",
                "USSW_CommercialLow01_L4_6x6",
                "USSW_CommercialLow01_L1_4x4",
                "USSW_CommercialLow01_L2_4x4",
                "USSW_CommercialLow01_L3_4x4",
                "USSW_CommercialLow01_L4_4x4",
                "USSW_CommercialLow01_L5_4x4",
                "USSW_CommercialLow02_L1_3x6",
                "USSW_CommercialLow02_L2_3x6",
                "USSW_CommercialLow02_L3_3x6",
                "USSW_CommercialLow02_L4_3x6",
                "USSW_CommercialLow02_L5_3x6",
                "USSW_CommercialLow03_L1_3x6",
                "USSW_CommercialLow03_L2_3x6",
                "USSW_CommercialLow03_L3_3x6",
                "USSW_CommercialLow03_L4_3x6",
                "USSW_CommercialLow03_L5_3x6",
                "USSW_CommercialLow04_L1_3x4",
                "USSW_CommercialLow04_L2_3x4",
                "USSW_CommercialLow04_L3_3x4",
                "USSW_CommercialLow04_L4_3x4",
                "USSW_CommercialLow100_L1_6x4",
                "USSW_CommercialLow100_L2_6x4",
                "USSW_CommercialLow100_L3_6x4",
                "USSW_CommercialLow100_L4_6x4",
                "USSW_CommercialLow100_L5_6x4",
            };

            bool changed = false;

            foreach (string prefabName in prefabNames)
            {
                if (AddOn_FixPolesInRP(prefabName, active))
                    changed = true;
            }

            if (changed)
            {
                changeCount += active ? 1 : -1;
                isPolesSet = active;
                SetState();
            }
        }

        public bool AddOn_FixPolesInRP(string prefabName, bool active)
        {
            if (
                PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), prefabName),
                    out Entity selectedEntity
                )
                && EntityManager.TryGetBuffer(
                    selectedEntity,
                    false,
                    out DynamicBuffer<SubObject> objBuffer
                )
            )
            {
                bool modified = false;
                int sl = 0;
                for (int i = objBuffer.Length - 1; i >= 0; i--)
                {
                    var subObject = objBuffer[i];
                    if (
                        prefabName.StartsWith("CN")
                        && EntityManager.Exists(FlagPoleCommercial02Entity)
                    )
                    {
                        if (objBuffer[i].m_Prefab == FlagPoleCommercial02Entity)
                        {
                            if (!active && Math.Round(subObject.m_Position.y) == 124f)
                            {
                                subObject.m_Position.y = 135f;
                                modified = true;
                            }
                            else if (active && Math.Round(subObject.m_Position.y) == 135f)
                            {
                                subObject.m_Position.y = 124f;
                                modified = true;
                            }
                            objBuffer[i] = subObject;
                        }
                    }
                    else if (
                        prefabName.StartsWith("USSW")
                        && (
                            EntityManager.Exists(Screen01Entity)
                            || EntityManager.Exists(Screen02Entity)
                            || EntityManager.Exists(BillboardWallMedium01Entity)
                        )
                    )
                    {
                        if (
                            !active
                            && (
                                (
                                    subObject.m_Prefab == BillboardWallMedium01Entity
                                    && subObject.m_Position.y == 1.803f
                                )
                                || (
                                    (
                                        subObject.m_Prefab == Screen01Entity
                                        || subObject.m_Prefab == Screen02Entity
                                    )
                                    && subObject.m_Position.y == 0.003f
                                )
                            )
                        )
                        {
                            if (
                                polePositions.TryGetValue(
                                    prefabName + subObject.m_Prefab.Index + sl,
                                    out float posY
                                )
                            )
                                subObject.m_Position.y = posY;

                            modified = true;
                            //LogHelper.SendLog($"Reverted {prefabName} {name}");
                        }
                        else if (active && Math.Round(subObject.m_Position.y) > 5f)
                        {
                            if (
                                !polePositions.ContainsKey(
                                    prefabName + subObject.m_Prefab.Index + sl
                                )
                            )
                                polePositions.Add(
                                    prefabName + subObject.m_Prefab.Index + sl,
                                    subObject.m_Position.y
                                );
                            if (subObject.m_Prefab == BillboardWallMedium01Entity)
                                subObject.m_Position.y = 1.803f;
                            else
                                subObject.m_Position.y = 0.003f;

                            modified = true;
                            //LogHelper.SendLog($"Fixed {prefabName} {name}");
                        }
                    }
                    objBuffer[i] = subObject;
                    sl++;
                }

                if (modified)
                    LogHelper.SendLog("Hovering Poles fix completed", LogLevel.DEVD);

                return modified;
            }
            return false;
        }

        public void FixUSSWHospital(bool active = true)
        {
            if (!systemReady)
                return;
            if (!IsActive(PackType.RP_USSW))
                return;
            if (!active && !isUSSWHospitalSet)
                return;
            if (
                !PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "USSW_Hospital_16x14"),
                    out Entity USSWHospital
                )
                || EntityManager.TryGetBuffer(
                    USSWHospital,
                    false,
                    out DynamicBuffer<SubObject> hospitalObj
                )
            )
                return;

            bool changed = false;
            if (!active && !firstPass)
            {
                hospitalObj.Add(
                    new SubObject()
                    {
                        m_Prefab = CarSpawnMarker,
                        m_Position = new float3(5.208054f, 0.01312256f, 46.25966f),
                        m_Rotation = new quaternion(0, -.7731476f, 0, 0.634226f),
                        m_Flags = SubObjectFlags.OnGround,
                        m_ParentIndex = -1,
                        m_GroupIndex = 0,
                        m_Probability = 100,
                    }
                );
                changeCount--;
                isUSSWHospitalSet = false;
                changed = true;
            }
            else if (active)
            {
                for (int i = hospitalObj.Length - 1; i >= 0; i--)
                {
                    if (hospitalObj[i].m_Prefab == CarSpawnMarker)
                    {
                        hospitalObj.RemoveAt(i);
                        break;
                    }
                }

                changeCount++;
                isUSSWHospitalSet = true;
                changed = true;
            }

            if (changed)
            {
                LogHelper.SendLog("USSW_Hospital fix completed", LogLevel.DEVD);
                SetState();
            }
        }

        public void FixParkingLotSolar(bool active = true)
        {
            return;
            //if (!systemReady)
            //    return;
            //if (!active && !isSolarParkingSet)
            //    return;
            //if (
            //    PrefabHelper.TryGetEntity(
            //        new PrefabID(nameof(BuildingPrefab), "ParkingLot12"),
            //        out Entity ParkingLot12Entity
            //    )
            //    && PrefabHelper.TryGetEntity(
            //        new PrefabID(nameof(BuildingPrefab), "ParkingLot13"),
            //        out Entity ParkingLot13Entity
            //    )
            //    && PrefabHelper.TryGetEntity(
            //        new PrefabID("PowerLinePrefab", "Low-voltage Marker"),
            //        out Entity powerLineEntity
            //    )
            //)
            //{
            //    bool changed = false;

            //    if (!active && !firstPass)
            //    {
            //        EntityManager.RemoveComponent<SolarPoweredData>(ParkingLot12Entity);
            //        EntityManager.RemoveComponent<SolarPoweredData>(ParkingLot13Entity);
            //        EntityManager.RemoveComponent<PowerPlantData>(ParkingLot12Entity);
            //        EntityManager.RemoveComponent<PowerPlantData>(ParkingLot13Entity);

            //        if (
            //            EntityManager.TryGetBuffer(
            //                ParkingLot12Entity,
            //                false,
            //                out DynamicBuffer<SubNet> osni1
            //            )
            //        )
            //        {
            //            for (int i = osni1.Length - 1; i >= 0; i--)
            //            {
            //                if (osni1[i].m_Prefab == powerLineEntity)
            //                {
            //                    osni1.RemoveAt(i);
            //                    break;
            //                }
            //            }
            //        }

            //        if (
            //            EntityManager.TryGetBuffer(
            //                ParkingLot13Entity,
            //                false,
            //                out DynamicBuffer<SubNet> osni2
            //            )
            //        )
            //        {
            //            for (int i = osni2.Length - 1; i >= 0; i--)
            //            {
            //                if (osni2[i].m_Prefab == powerLineEntity)
            //                {
            //                    osni2.RemoveAt(i);
            //                    break;
            //                }
            //            }
            //        }

            //        changeCount--;
            //        isSolarParkingSet = false;
            //        changed = true;
            //    }
            //    else if (active)
            //    {
            //        float3 f3 = new(0, 0, 12);
            //        SubNet marker = new()
            //        {
            //            m_Prefab = powerLineEntity,
            //            m_Curve = new Colossal.Mathematics.Bezier4x3(f3, f3, f3, f3),
            //            m_NodeIndex = new int2(999, 999),
            //            m_ParentMesh = new int2(-1, -1),
            //            m_Snapping = new bool2(true, false),
            //        };

            //        SolarPoweredData solarPoweredData1 = new() { m_Production = 2500 };
            //        SolarPoweredData solarPoweredData2 = new() { m_Production = 7500 };

            //        EntityManager.AddComponentData(ParkingLot12Entity, solarPoweredData1);
            //        EntityManager.AddComponent<PowerPlantData>(ParkingLot12Entity);
            //        if (
            //            EntityManager.TryGetBuffer(
            //                ParkingLot12Entity,
            //                false,
            //                out DynamicBuffer<SubNet> osni1
            //            )
            //        )
            //            osni1.Add(marker);

            //        EntityManager.AddComponentData(ParkingLot13Entity, solarPoweredData2);
            //        EntityManager.AddComponent<PowerPlantData>(ParkingLot13Entity);
            //        if (
            //            EntityManager.TryGetBuffer(
            //                ParkingLot13Entity,
            //                false,
            //                out DynamicBuffer<SubNet> osni2
            //            )
            //        )
            //            osni2.Add(marker);

            //        changeCount++;
            //        isSolarParkingSet = true;
            //        changed = true;
            //    }

            //    if (changed)
            //    {
            //        LogHelper.SendLog("ParkingLot12 & ParkingLot13 fix completed", LogLevel.DEVD);
            //        SetState();
            //    }
            //}
        }

        public void FixRSClinic(bool active = true)
        {
            if (!systemReady)
                return;
            if (!IsActive(PackType.DLC_MA))
                return;
            if (!active && !isRSClinicSet)
                return;

            if (
                !PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "RS_MedicalClinic01"),
                    out Entity RS_MedicalClinic01Entity
                )
                || !PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(BuildingPrefab), "RS_MedicalClinic01_Sub01"),
                    out Entity RS_MedicalClinic01_Sub01Entity
                )
            )
                return;

            if (
                EntityManager.TryGetComponent(
                    RS_MedicalClinic01Entity,
                    out StorageLimitData storage1
                )
                && EntityManager.TryGetComponent(
                    RS_MedicalClinic01_Sub01Entity,
                    out StorageLimitData storage2
                )
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    storage1.m_Limit = 100;
                    storage2.m_Limit = 100;

                    changeCount--;
                    isRSClinicSet = false;
                    changed = true;
                }
                else if (active)
                {
                    storage1.m_Limit = 1000;
                    storage2.m_Limit = 1000;

                    changeCount++;
                    isRSClinicSet = true;
                    changed = true;
                }
                if (changed)
                {
                    EntityManager.SetComponentData(RS_MedicalClinic01Entity, storage1);
                    EntityManager.SetComponentData(RS_MedicalClinic01_Sub01Entity, storage2);
                    LogHelper.SendLog(
                        "RS_MedicalClinic01 & RS_MedicalClinic01_Sub01 fix completed",
                        LogLevel.DEVD
                    );
                    SetState();
                }
            }
        }

        public bool AddOn_FixSubNetDirection(string prefabName, bool active)
        {
            if (
                !PrefabHelper.TryGetEntity(
                    new PrefabID("BuildingPrefab", prefabName),
                    out Entity prefabEntity
                )
                || !EntityManager.TryGetBuffer(
                    prefabEntity,
                    false,
                    out DynamicBuffer<Game.Prefabs.SubNet> subNets
                )
            )
                return false;
            for (int i = subNets.Length - 1; i >= 0; i--)
            {
                var subNet = subNets[i];

                if (prefabName == "BusStation01")
                {
                    subNet.m_InvertMode = active
                        ? NetInvertMode.RighthandTraffic
                        : NetInvertMode.Never;
                }
                else
                {
                    subNet.m_InvertMode = active
                        ? NetInvertMode.LefthandTraffic
                        : NetInvertMode.Never;
                }
                subNets[i] = subNet;
            }

            if (
                prefabName == "BusStation01"
                && EntityManager.TryGetBuffer(
                    prefabEntity,
                    false,
                    out DynamicBuffer<Game.Prefabs.SubObject> subObjects
                )
                && PrefabHelper.TryGetEntity(
                    new PrefabID(nameof(StaticObjectPrefab), "SlowDownTextDecal01"),
                    out Entity SlowDownDecal01Entity
                )
                && PrefabHelper.TryGetEntity(
                    new PrefabID(
                        nameof(StaticObjectPrefab),
                        "RoadArrow Forward Right90 Placeholder"
                    ),
                    out Entity RoadArrowForwardRight90PlaceholderEntity
                )
            )
            {
                for (int i = subObjects.Length - 1; i >= 0; i--)
                {
                    var subObject = subObjects[i];

                    if (
                        subObject.m_Prefab == SlowDownDecal01Entity
                        || subObject.m_Prefab == RoadArrowForwardRight90PlaceholderEntity
                    )
                    {
                        subObject.m_Rotation = math.mul(
                            subObject.m_Rotation,
                            quaternion.RotateY(math.radians(180))
                        );

                        subObjects[i] = subObject;
                    }
                }
            }

            LogHelper.SendLog($"{prefabName} fix completed", LogLevel.DEVD);
            return true;
        }

        public void FixLHTBusStation02(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isLHTBusStation02Set)
                return;

            bool changed = false;

            if (AddOn_FixSubNetDirection("BusStation02", active))
                changed = true;

            if (changed)
            {
                changeCount += active ? 1 : -1;
                isLHTBusStation02Set = active;
                SetState();
            }
        }

        public void FixLHTTaxiDepot01(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isLHTTaxiDepot01Set)
                return;

            bool changed = false;

            if (AddOn_FixSubNetDirection("TaxiDepot01", active))
                changed = true;

            if (changed)
            {
                changeCount += active ? 1 : -1;
                isLHTTaxiDepot01Set = active;
                SetState();
            }
        }

        public void FixLHTTramDepot01(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isLHTTramDepot01Set)
                return;

            bool changed = false;

            if (AddOn_FixSubNetDirection("TramDepot01", active))
                changed = true;

            if (changed)
            {
                changeCount += active ? 1 : -1;
                isLHTTramDepot01Set = active;
                SetState();
            }
        }

        public void FixLHTCargoHarbor01(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isLHTCargoHarbor01Set)
                return;

            bool changed = false;

            if (AddOn_FixSubNetDirection("CargoHarbor01", active))
                changed = true;

            if (changed)
            {
                changeCount += active ? 1 : -1;
                isLHTCargoHarbor01Set = active;
                SetState();
            }
        }

        public void FixRHTBusStation01(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isRHTBusStation01Set)
                return;

            bool changed = false;

            if (AddOn_FixSubNetDirection("BusStation01", active))
                changed = true;

            if (changed)
            {
                changeCount += active ? 1 : -1;
                isRHTBusStation01Set = active;
                SetState();
            }
        }

        public void FixNLLowHousehold(bool active = true)
        {
            if (!systemReady)
                return;
            if (!IsActive(PackType.RP_NL))
                return;
            if (!active && !isNLLowHouseholdSet)
                return;
            if (
                !PrefabHelper.TryGetEntity(
                    new PrefabID("ZonePrefab", "NL Residential Gambrel Low"),
                    out Entity zoneEntity1
                )
                || !EntityManager.TryGetComponent(zoneEntity1, out ZonePropertiesData zoneProps1)
                || !PrefabHelper.TryGetEntity(
                    new PrefabID("ZonePrefab", "NL Residential Gable Low"),
                    out Entity zoneEntity2
                )
                || !EntityManager.TryGetComponent(zoneEntity2, out ZonePropertiesData zoneProps2)
            )
                return;

            bool changed = false;
            if (!active && !firstPass)
            {
                changeCount--;
                isNLLowHouseholdSet = false;
                changed = true;
            }
            else if (active)
            {
                changeCount++;
                isNLLowHouseholdSet = true;
                changed = true;
            }
            if (changed)
            {
                EntityQuery zoneBuildingsEntity = SystemAPI
                    .QueryBuilder()
                    .WithAll<SpawnableBuildingData>()
                    .Build();
                var zoneBuildings = zoneBuildingsEntity.ToEntityArray(Allocator.Temp);
                zoneProps1.m_ResidentialProperties = active ? 2 : 1;
                zoneProps2.m_ResidentialProperties = active ? 2 : 1;
                EntityManager.SetComponentData(zoneEntity1, zoneProps1);
                EntityManager.SetComponentData(zoneEntity2, zoneProps2);
                foreach (var building in zoneBuildings)
                {
                    if (
                        EntityManager.TryGetComponent(building, out SpawnableBuildingData sbd)
                        && (sbd.m_ZonePrefab == zoneEntity1 || sbd.m_ZonePrefab == zoneEntity2)
                    )
                    {
                        EntityManager.TryGetComponent(building, out BuildingPropertyData bpd);
                        bpd.m_ResidentialProperties = active ? 2 : 1;
                        EntityManager.SetComponentData(building, bpd);
                    }
                }

                LogHelper.SendLog($"NL Zone fix completed", LogLevel.DEVD);
                SetState();
            }
        }

        public void FixAdditionalTransformers(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isAdditionalTransformersSet)
                return;
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
                bool changed = false;

                List<string> buildings = new()
                {
                    "NuclearPowerPlant01",
                    "HydroelectricPowerPlant01",
                };

                if (!active && !firstPass)
                {
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        if (
                            !PrefabHelper.TryGetEntity(
                                new PrefabID(nameof(BuildingPrefab), buildings[i]),
                                out Entity powerBuildingEntity
                            ) || !EntityManager.HasComponent<PowerPlantData>(powerBuildingEntity)
                        )
                            continue;

                        if (!EntityManager.HasBuffer<BuildingUpgradeElement>(powerBuildingEntity))
                            EntityManager.AddBuffer<BuildingUpgradeElement>(powerBuildingEntity);

                        var buildingUpgradeElement =
                            EntityManager.GetBuffer<BuildingUpgradeElement>(
                                powerBuildingEntity,
                                false
                            );

                        for (int j = buildingUpgradeElement.Length - 1; j >= 0; j--)
                        {
                            if (buildingUpgradeElement[j].m_Upgrade == additionalTransformerEntity)
                            {
                                buildingUpgradeElement.RemoveAt(j);
                                break;
                            }
                        }

                        for (int j = serviceUpgradeBuilding.Length - 1; j >= 0; j--)
                        {
                            if (serviceUpgradeBuilding[j].m_Building == powerBuildingEntity)
                            {
                                serviceUpgradeBuilding.RemoveAt(j);
                                break;
                            }
                        }
                    }

                    changeCount--;
                    isAdditionalTransformersSet = false;
                    changed = true;
                }
                else if (active)
                {
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        if (
                            !PrefabHelper.TryGetEntity(
                                new PrefabID(nameof(BuildingPrefab), buildings[i]),
                                out Entity powerBuildingEntity
                            ) || !EntityManager.HasComponent<PowerPlantData>(powerBuildingEntity)
                        )
                            continue;

                        if (!EntityManager.HasBuffer<BuildingUpgradeElement>(powerBuildingEntity))
                            EntityManager.AddBuffer<BuildingUpgradeElement>(powerBuildingEntity);

                        var buildingUpgradeElement =
                            EntityManager.GetBuffer<BuildingUpgradeElement>(
                                powerBuildingEntity,
                                false
                            );

                        buildingUpgradeElement.Add(
                            new BuildingUpgradeElement() { m_Upgrade = additionalTransformerEntity }
                        );
                        serviceUpgradeBuilding.Add(
                            new ServiceUpgradeBuilding() { m_Building = powerBuildingEntity }
                        );
                    }

                    changeCount++;
                    isAdditionalTransformersSet = true;
                    changed = true;
                }
                if (changed)
                {
                    LogHelper.SendLog($"Additional transformer fix completed", LogLevel.DEVD);
                    SetState();
                }
            }
        }

        public void FixFRCityHall(bool active = true)
        {
            if (!systemReady)
                return;
            if (!IsActive(PackType.RP_FR))
                return;
            if (!active && !isFRCityHallSet)
                return;
            bool changed = false;

            if (
                !(
                    PrefabHelper.TryGetEntity(
                        new PrefabID(nameof(BuildingPrefab), "CityHall01"),
                        out Entity cityHallEntity
                    )
                    && EntityManager.TryGetBuffer(
                        cityHallEntity,
                        false,
                        out DynamicBuffer<UnlockOnBuildData> uob0
                    )
                    && PrefabHelper.TryGetEntity(
                        new PrefabID(nameof(BuildingPrefab), "FR_CityHall01"),
                        out Entity frCityHallEntity
                    )
                )
            )
                return;
            if (
                !(
                    PrefabHelper.TryGetEntity(
                        new PrefabID(nameof(BuildingExtensionPrefab), "CityHall01 Court"),
                        out Entity cityHallUp1Entity
                    )
                    && EntityManager.TryGetBuffer(
                        cityHallUp1Entity,
                        false,
                        out DynamicBuffer<UnlockOnBuildData> uob1
                    )
                    && PrefabHelper.TryGetEntity(
                        new PrefabID(nameof(BuildingExtensionPrefab), "FR_CityHall01_Ext01"),
                        out Entity frCityHallUp1Entity
                    )
                )
            )
                return;
            if (
                !(
                    PrefabHelper.TryGetEntity(
                        new PrefabID(
                            nameof(BuildingExtensionPrefab),
                            "CityHall01 Planning Offices"
                        ),
                        out Entity cityHallUp2Entity
                    )
                    && EntityManager.TryGetBuffer(
                        cityHallUp2Entity,
                        false,
                        out DynamicBuffer<UnlockOnBuildData> uob2
                    )
                    && PrefabHelper.TryGetEntity(
                        new PrefabID(nameof(BuildingExtensionPrefab), "FR_CityHall01_Ext02"),
                        out Entity frCityHallUp2Entity
                    )
                )
            )
                return;

            if (!active && !firstPass)
            {
                if (
                    !(
                        EntityManager.HasBuffer<UnlockOnBuildData>(frCityHallEntity)
                        || EntityManager.HasBuffer<UnlockOnBuildData>(frCityHallUp1Entity)
                        || EntityManager.HasBuffer<UnlockOnBuildData>(frCityHallUp2Entity)
                    )
                )
                    return;
                EntityManager.RemoveComponent<UnlockOnBuildData>(frCityHallEntity);
                EntityManager.RemoveComponent<UnlockOnBuildData>(frCityHallUp1Entity);
                EntityManager.RemoveComponent<UnlockOnBuildData>(frCityHallUp2Entity);
                changed = true;
                LogHelper.SendLog($"FR City Hall unlocks removal completed", LogLevel.DEVD);
            }
            else if (active)
            {
                if (!EntityManager.HasBuffer<UnlockOnBuildData>(frCityHallEntity))
                    EntityManager.AddBuffer<UnlockOnBuildData>(frCityHallEntity);

                if (
                    EntityManager.TryGetBuffer(
                        frCityHallEntity,
                        false,
                        out DynamicBuffer<UnlockOnBuildData> uobNew
                    )
                )
                    for (int i = 0; i < uob0.Length; i++)
                    {
                        uobNew.Add(uob0[i]);
                    }

                if (!EntityManager.HasBuffer<UnlockOnBuildData>(frCityHallUp1Entity))
                    EntityManager.AddBuffer<UnlockOnBuildData>(frCityHallUp1Entity);

                if (
                    EntityManager.TryGetBuffer(
                        frCityHallUp1Entity,
                        false,
                        out DynamicBuffer<UnlockOnBuildData> uobNew1
                    )
                )
                    for (int i = 0; i < uob1.Length; i++)
                    {
                        uobNew1.Add(uob1[i]);
                    }

                if (!EntityManager.HasBuffer<UnlockOnBuildData>(frCityHallUp2Entity))
                    EntityManager.AddBuffer<UnlockOnBuildData>(frCityHallUp2Entity);

                if (
                    EntityManager.TryGetBuffer(
                        frCityHallUp2Entity,
                        false,
                        out DynamicBuffer<UnlockOnBuildData> uobNew2
                    )
                )
                    for (int i = 0; i < uob2.Length; i++)
                    {
                        uobNew2.Add(uob2[i]);
                    }
                changed = true;
                LogHelper.SendLog($"FR City Hall unlocks fix completed", LogLevel.DEVD);
            }

            if (changed)
            {
                changeCount += active ? 1 : -1;
                isFRCityHallSet = active;
                SetState();
            }
        }

        public void UpdateIndustrialWorker(bool active = true, int value = 1)
        {
            if (!isIndustrialCompanyWorkerUpdated && !active)
                return;

            if (value == 1)
                active = false;
            if (!active)
                value = 1;

            int multiplier1 = active ? value : 1;

            int multiplier2 = active ? (int)math.ceil(multiplier1 / 5) : 1;

            PrefabSystem _prefabSystem = WorldHelper.PrefabSystem;
            EntityQuery query = SystemAPI.QueryBuilder().WithAll<IndustrialProcessData>().Build();
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            string text = active ? "Boosting" : "Resetting";
            LogHelper.SendLog($"{text} {entities.Length} entities...", LogLevel.DEVD);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                IndustrialProcessData processData = SystemAPI.GetComponent<IndustrialProcessData>(
                    entity
                );

                if (!_prefabSystem.TryGetPrefab(entity, out PrefabBase prefab))
                    continue;

                float existing = processData.m_MaxWorkersPerCell;

                if (existing == 0)
                    continue;

                if (prefab.TryGet(out Game.Prefabs.ProcessingCompany pc))
                {
                    if (
                        processData.m_MaxWorkersPerCell
                        == pc.process.m_MaxWorkersPerCell * multiplier1
                    )
                        continue;

                    processData.m_MaxWorkersPerCell = pc.process.m_MaxWorkersPerCell * multiplier1;
                    processData.m_Output.m_Amount = pc.process.m_Output.m_Amount * multiplier2;
                }

                LogHelper.SendLog(
                    $"Boosted {prefab.name} from {existing} to {processData.m_MaxWorkersPerCell} workers per cell.",
                    LogLevel.DEVD
                );
                SystemAPI.SetComponent(entity, processData);
            }
            LogHelper.SendLog("Done boosting workers!", LogLevel.DEVD);
            isIndustrialCompanyWorkerUpdated = true;
            SetState();
        }

        public void FixSaveFromSolar()
        {
            if (
                !(
                    PrefabHelper.TryGetEntity(
                        new PrefabID(nameof(BuildingPrefab), "ParkingLot12"),
                        out Entity ParkingLot12Entity
                    )
                    && PrefabHelper.TryGetEntity(
                        new PrefabID(nameof(BuildingPrefab), "ParkingLot13"),
                        out Entity ParkingLot13Entity
                    )
                    && PrefabHelper.TryGetEntity(
                        new PrefabID("PowerLinePrefab", "Low-voltage Marker"),
                        out Entity powerLineEntity
                    )
                )
            )
                return;

            var query = SystemAPI
                .QueryBuilder()
                .WithAll<Game.Net.SubNet, Game.Buildings.ParkingFacility>()
                .Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            LogHelper.SendLog($"Found {entities.Length} entities.");
            int processed = 0;
            foreach (var item in entities)
            {
                if (PrefabHelper.TryFindPrefabRef(item, out Entity PrefabRef))
                {
                    if (PrefabRef != ParkingLot12Entity && PrefabRef != ParkingLot13Entity)
                        continue;
                    if (
                        EntityManager.TryGetBuffer(
                            item,
                            false,
                            out DynamicBuffer<Game.Net.SubNet> osni1
                        )
                    )
                    {
                        bool doneEntity = false;
                        for (int i = osni1.Length - 1; i >= 0; i--)
                        {
                            if (
                                PrefabHelper.TryFindPrefabRef(osni1[i].m_SubNet, out Entity NetRef)
                                && (NetRef == powerLineEntity)
                            )
                            {
                                //osni1.RemoveAt(i);
                                LogHelper.SendLog("Removed power line from a parking lot sub-net.");
                                processed++;
                                doneEntity = true;
                            }

                            if (doneEntity)
                            {
                                if (
                                    EntityManager.TryGetComponent(
                                        item,
                                        out Game.Buildings.Building b
                                    )
                                )
                                {
                                    EntityManager.AddComponent<Deleted>(b.m_RoadEdge);
                                    //EntityManager.AddComponent<Updated>(item);
                                    //EntityManager.AddComponent<Highlighted>(item);
                                }
                                //osni1 = new();
                                break;
                            }
                        }
                    }
                }
            }
            if (processed > 0)
            {
                LogHelper.SendLog(LocaleHelper.Translate($"{Mod.Id}.ReloadSave"), LogLevel.Error);

                GameManager.instance.userInterface.appBindings.ShowMessageDialog(
                    new MessageDialog(Mod.Name, $"{Mod.Id}.ReloadSave", $"Common.OK"),
                    x =>
                    {
                        //WorldHelper.GetSystem<GameScreenUISystem>().activeScreen =
                        //    GameScreenUISystem.GameScreen.SaveGame;
                    }
                );
            }
        }
    }
}
