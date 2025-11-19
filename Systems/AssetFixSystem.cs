using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using StarQ.Shared.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ServiceUpgrade = Game.Prefabs.ServiceUpgrade;

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

        private readonly List<Entity> addedStorageLimit = new();
        private readonly List<Entity> addedCargoTransport = new();
        private readonly Dictionary<string, float> polePositions = new();
        private readonly Dictionary<Entity, ObjectPrefab[]> extractorsModified = new();

        private bool isPrisonSet = false;

        //private bool isPrisonVanSet = false;
        //private bool isStorageSet = false;
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
        private bool isAdditionalTransformersSet = false;

        //private bool isExtractorsDisabled = false;

        //private bool isHarborSet = false;

        private bool hasRP_CN;
        private bool hasRP_USSW;
        private bool hasDLC_MA;
        private bool hasRP_NL;

        public enum PackType
        {
            RP_CN,
            RP_USSW,
            DLC_MA,
            RP_NL,
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            prefabSystem =
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
        }

        public bool IsActive(PackType packType)
        {
            return packType switch
            {
                PackType.RP_CN => hasRP_CN,
                PackType.RP_USSW => hasRP_USSW,
                PackType.DLC_MA => hasDLC_MA,
                PackType.RP_NL => hasRP_NL,
                _ => false,
            };
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (totalCount == 0)
                CheckCount();
        }

        private void CheckCount()
        {
            totalCount = 11;
            // prison + prison van + recycling + hospital + solar parking +
            // lht bus station 2 + lht taxi depot + lht tram depot +
            // lht cargo harbor + rht bus station 1 + additional transformers

            totalCount -= 2;
            // prison van + recycling
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "CN_OfficeHighA_L1_6x6"),
                    out PrefabBase _
                )
            )
            {
                hasRP_CN = true;
                totalCount++;
            }
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "USSW_Hospital_16x14"),
                    out PrefabBase _
                )
            )
            {
                hasRP_USSW = true;
                totalCount++;
            }
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "RS_MedicalClinic01"),
                    out PrefabBase _
                )
            )
            {
                hasDLC_MA = true;
                totalCount++;
            }
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("AssetPackPrefab", "NL Pack Filter"),
                    out PrefabBase _
                )
            )
            {
                hasRP_NL = true;
                totalCount++;
            }
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            if (systemDisposed || prefabSystem == null)
                return;

            if (!systemReady)
                return;

            if (mode == GameMode.Editor)
            {
                //FixPrisonBus01(false);
                FixPrison01(false);
                //FixStorageMissing(false, false);
                FixHostipal01(false);
                FixHoveringPoles(false);
                FixUSSWHospital(false);
                FixParkingLotSolar(false);
                FixRSClinic(false);
                FixLHTBusStation02(false);
                FixLHTTaxiDepot01(false);
                FixLHTTramDepot01(false);
                FixLHTCargoHarbor01(false);
                FixRHTBusStation01(false);
                FixNLLowHousehold(false);
                //FixExtractorSpawning(false);
            }
            if (mode != GameMode.Game)
                return;
            //#endif

            FixAdditionalTransformers();
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
            //if (settings.PrisonVan)
            //    FixPrisonBus01();
            if (settings.Prison)
                FixPrison01();
            //if (settings.Storage || settings.Recycling)
            //    FixStorageMissing(settings.Storage, settings.Recycling);
            if (settings.Hospital)
                FixHostipal01();
            if (settings.HoveringPoles && (IsActive(PackType.RP_CN) || IsActive(PackType.RP_USSW)))
                FixHoveringPoles();
            //FixHarbourExtBase();
            if (settings.USSWHospital && IsActive(PackType.RP_USSW))
                FixUSSWHospital();
            if (settings.SolarParking)
                FixParkingLotSolar();
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
            //if (settings.AdditionalTransformers)
            //    FixAdditionalTransformers();
            //if (1 == 1)
            //{
            //    //FixExtractorSpawning(false);
            //    FixOfficeLowStorage();
            //}
        }

        protected override void OnUpdate() { }

        public static void SetState()
        {
            if (
                Mod.modState == ModState.None
                || Mod.modState == ModState.Incompatible
                || firstPass
                || systemDisposed
                || !systemReady
            )
                return;
            Mod.UpdateState();
            LogHelper.SendLog($"{changeCount} changes set");
        }

        public void UpdatePrefab(PrefabBase prefabBase)
        {
            prefabSystem.UpdatePrefab(prefabBase);
            LogHelper.SendLog($"{prefabBase.name} updated");
        }

        //public void FixPrisonBus01(bool active = true)
        //{
        //    if (!systemReady)
        //        return;
        //    if (!active && !isPrisonVanSet)
        //        return;
        //    if (
        //        prefabSystem.TryGetPrefab(
        //            new PrefabID("CarPrefab", "PrisonVan01"),
        //            out PrefabBase prefabBase
        //        ) && prefabBase.TryGet(out CarPrefab prisonVan)
        //    )
        //    {
        //        bool changed = false;
        //        if (!active && !firstPass)
        //        {
        //            prisonVan.m_SizeClass = Game.Vehicles.SizeClass.Medium;
        //            changeCount--;
        //            isPrisonVanSet = false;
        //            changed = true;
        //        }
        //        else if (active)
        //        {
        //            prisonVan.m_SizeClass = Game.Vehicles.SizeClass.Large;
        //            changeCount++;
        //            isPrisonVanSet = true;
        //            changed = true;
        //        }
        //        if (changed)
        //        {
        //            UpdatePrefab(prefabBase);
        //            SetState();
        //        }
        //    }
        //}

        public void FixPrison01(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isPrisonSet)
                return;
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "Prison01"),
                    out PrefabBase prefabBase
                ) && prefabBase.TryGet(out Game.Prefabs.Prison prison)
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    prison.m_PrisonVanCapacity = 10;
                    changeCount--;
                    isPrisonSet = false;
                    changed = true;
                }
                else if (active)
                {
                    prison.m_PrisonVanCapacity = 20;
                    changeCount++;
                    isPrisonSet = true;
                    changed = true;
                }
                if (changed)
                {
                    UpdatePrefab(prefabBase);
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
            prefabSystem.TryGetPrefab(
                new PrefabID("StaticObjectPrefab", "ParkingLotDecal04"),
                out PrefabBase parking04
            );
            prefabSystem.TryGetPrefab(
                new PrefabID("StaticObjectPrefab", "ParkingLotServiceDecal04"),
                out PrefabBase parkingService04
            );

            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "Hospital01"),
                    out PrefabBase prefabBase
                ) && prefabBase.TryGet(out ObjectSubObjects hospital)
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    for (int indexH = 0; indexH < hospital.m_SubObjects.Length; indexH++)
                    {
                        var obj = hospital.m_SubObjects[indexH];
                        string name = obj.m_Object.name;
                        if (name == "ParkingLotServiceDecal04")
                        {
                            obj.m_Object = (ObjectPrefab)parking04;
                        }
                    }
                    changeCount--;
                    isHospitalSet = false;
                    changed = true;
                }
                else if (active)
                {
                    for (int indexH = 0; indexH < hospital.m_SubObjects.Length; indexH++)
                    {
                        var obj = hospital.m_SubObjects[indexH];
                        string name = obj.m_Object.name;
                        if (name == "ParkingLotDecal04" && Math.Round(obj.m_Position.x) == -82f)
                        {
                            obj.m_Object = (ObjectPrefab)parkingService04;
                        }
                    }
                    changeCount++;
                    isHospitalSet = true;
                    changed = true;
                }
                if (changed)
                {
                    UpdatePrefab(prefabBase);
                    SetState();
                }
            }
        }

        //public void FixStorageMissing(bool storageActive = true, bool recyclingActive = true)
        //{
        //    storageActive = false;
        //    if (!systemReady)
        //        return;
        //    if (!storageActive && !recyclingActive && !isStorageSet)
        //        return;
        //    if (!(storageActive || recyclingActive) && !firstPass)
        //    {
        //        List<Entity> toRemoveFromAddedStorageLimit = new();
        //        foreach (Entity entity in addedStorageLimit)
        //        {
        //            if (!prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase))
        //            {
        //                continue;
        //            }
        //            prefabBase.Remove<StorageLimit>();
        //            UpdatePrefab(prefabBase);
        //            toRemoveFromAddedStorageLimit.Add(entity);
        //        }
        //        List<Entity> toRemoveFromAddedCargoTransport = new();
        //        foreach (Entity entity in addedCargoTransport)
        //        {
        //            EntityManager.TryGetComponent(entity, out PrefabData prefabData);
        //            prefabSystem.TryGetPrefab(prefabData, out PrefabBase prefabBase);

        //            if (prefabBase == null)
        //            {
        //                continue;
        //            }
        //            prefabBase.Remove<CargoTransportStation>();
        //            UpdatePrefab(prefabBase);
        //            toRemoveFromAddedCargoTransport.Add(entity);
        //        }
        //        changeCount--;
        //        isStorageSet = false;
        //        foreach (Entity entity in toRemoveFromAddedStorageLimit)
        //        {
        //            addedStorageLimit.Remove(entity);
        //        }
        //        foreach (Entity entity in toRemoveFromAddedCargoTransport)
        //        {
        //            addedCargoTransport.Remove(entity);
        //        }
        //    }
        //    else if ((storageActive || recyclingActive))
        //    {
        //        EntityQuery storageBuildingsQuery = SystemAPI
        //            .QueryBuilder()
        //            .WithAny<ResourceProductionData, StorageLimitData>()
        //            .WithNone<CompanyBrandElement, OutsideConnectionData, ServiceUpgradeData>()
        //            .Build();
        //        var storageBuildings = storageBuildingsQuery.ToEntityArray(Allocator.Temp);
        //        foreach (var entity in storageBuildings)
        //        {
        //            EntityManager.TryGetComponent(entity, out PrefabData prefabData);
        //            prefabSystem.TryGetPrefab(prefabData, out PrefabBase prefabBase);

        //            if (prefabBase == null)
        //            {
        //                continue;
        //            }

        //            //string name = $"{prefabSystem.GetPrefabName(entity)}";
        //            if (
        //                storageActive
        //                && !prefabBase.Has<CargoTransportStation>()
        //                && prefabBase.Has<StorageLimit>()
        //            )
        //            {
        //                // add ctl by product
        //                CargoTransportStation cts =
        //                    prefabBase.AddComponent<CargoTransportStation>();
        //                cts.transports = 1;
        //                cts.m_TransportInterval = new int2(0, 0);

        //                if (!addedCargoTransport.Contains(entity))
        //                    addedCargoTransport.Add(entity);
        //                UpdatePrefab(prefabBase);
        //                //LogHelper.SendLog($"{name} has stl but no ctl, added ctl");
        //            }
        //            else if (
        //                recyclingActive
        //                && !prefabBase.Has<CargoTransportStation>()
        //                && !prefabBase.Has<StorageLimit>()
        //            )
        //            {
        //                int storageValue = 0;
        //                List<ResourceInEditor> res = new();
        //                if (prefabBase.TryGet(out ResourceProducer rp))
        //                {
        //                    bool isRecycling = false;
        //                    if (prefabBase.TryGet(out GarbageFacility grbg))
        //                    {
        //                        isRecycling = true;
        //                        storageValue += grbg.m_GarbageCapacity;
        //                        if (!prefabBase.Has<TransportStop>())
        //                        {
        //                            var tpStop = prefabBase.AddComponent<TransportStop>();
        //                            tpStop.m_AccessConnectionType = RouteConnectionType.None;
        //                            tpStop.m_RouteConnectionType = RouteConnectionType.Cargo;
        //                            tpStop.m_AccessRoadType = Game.Net.RoadTypes.Car;
        //                            tpStop.m_CargoTransport = true;
        //                            tpStop.m_PassengerTransport = false;
        //                        }
        //                        res.Add(ResourceInEditor.Money);
        //                    }
        //                    for (int rrr = 0; rrr < rp.m_Resources.Length; rrr++)
        //                    {
        //                        var rpr = rp.m_Resources[rrr];
        //                        storageValue += rpr.m_StorageCapacity;
        //                        if (!isRecycling)
        //                        {
        //                            res.Add(rpr.m_Resource);
        //                        }
        //                    }

        //                    if (storageValue == 0)
        //                    {
        //                        storageValue = 1;
        //                    }

        //                    var stlN = prefabBase.AddComponent<StorageLimit>();
        //                    stlN.storageLimit = storageValue;
        //                    if (!addedStorageLimit.Contains(entity))
        //                        addedStorageLimit.Add(entity);
        //                    var ctsN = prefabBase.AddComponent<CargoTransportStation>();
        //                    ctsN.m_TradedResources = res.ToArray();
        //                    if (!addedCargoTransport.Contains(entity))
        //                        addedCargoTransport.Add(entity);
        //                    //LogHelper.SendLog(
        //                    //    $"{name} has no stl (added {storageValue}) and no ctl (added)"
        //                    //);
        //                    UpdatePrefab(prefabBase);
        //                }
        //            }
        //        }
        //        changeCount++;
        //        isStorageSet = true;
        //        SetState();
        //    }
        //}

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
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", prefabName),
                    out PrefabBase prefabBase
                ) && prefabBase.TryGet(out ObjectSubObjects objects)
            )
            {
                bool modified = false;
                int i = 0;
                foreach (var obj in objects.m_SubObjects)
                {
                    if (prefabName.StartsWith("CN"))
                    {
                        string name = obj.m_Object.name;
                        if (name == "FlagPoleCommercial02")
                        {
                            if (!active && Math.Round(obj.m_Position.y) == 124f)
                            {
                                obj.m_Position.y = 135f;
                                modified = true;
                                //LogHelper.SendLog($"Reverted {prefabName} poles");
                            }
                            else if (active && Math.Round(obj.m_Position.y) == 135f)
                            {
                                obj.m_Position.y = 124f;
                                modified = true;
                                //LogHelper.SendLog($"Fixed {prefabName} poles");
                            }
                        }
                    }
                    else if (prefabName.StartsWith("USSW"))
                    {
                        string name = obj.m_Object.name;
                        if (
                            name == "Screen02"
                            || name == "Screen01"
                            || name == "BillboardWallMedium01"
                        )
                        {
                            if (
                                !active
                                && (
                                    (name == "BillboardWallMedium01" && obj.m_Position.y == 1.803f)
                                    || ((name.StartsWith("Screen0") && obj.m_Position.y == 0.003f))
                                )
                            )
                            {
                                if (
                                    polePositions.TryGetValue(prefabName + name + i, out float posY)
                                )
                                {
                                    obj.m_Position.y = posY;
                                }
                                modified = true;
                                //LogHelper.SendLog($"Reverted {prefabName} {name}");
                            }
                            else if (active && Math.Round(obj.m_Position.y) > 5f)
                            {
                                if (!polePositions.ContainsKey(prefabName + name + i))
                                    polePositions.Add(prefabName + name + i, obj.m_Position.y);
                                if (name == "BillboardWallMedium01")
                                {
                                    obj.m_Position.y = 1.803f;
                                }
                                else
                                {
                                    obj.m_Position.y = 0.003f;
                                }
                                modified = true;
                                //LogHelper.SendLog($"Fixed {prefabName} {name}");
                            }
                        }
                    }
                    i++;
                }

                if (modified)
                    UpdatePrefab(prefabBase);

                return modified;
            }
            return false;
        }

        //public void FixHarbourExtBase(bool active = true)
        //{
        //    if (!active && !isHarborSet)
        //        return;
        //    if (
        //        prefabSystem.TryGetPrefab(
        //            new PrefabID("RenderPrefab", "Harbor01 Mesh"),
        //            out PrefabBase harbor_ext1_mesh
        //        )
        //    )
        //    {
        //        if (!active && !firstPass && harbor_ext1_mesh.Has<BaseProperties>())
        //        {
        //            harbor_ext1_mesh.Remove<BaseProperties>();
        //            changeCount--;
        //            isHarborSet = false;
        //            LogHelper.SendLog("Reverted Harbor Mesh");
        //        }
        //        else if (active && !harbor_ext1_mesh.Has<BaseProperties>())
        //        {
        //            prefabSystem.TryGetPrefab(
        //                new PrefabID("RenderPrefab", "Default_Base Mesh"),
        //                out PrefabBase defaultRP
        //            );

        //            BaseProperties bp = harbor_ext1_mesh.AddComponent<BaseProperties>();
        //            bp.m_UseMinBounds = true;
        //            bp.m_BaseType = (RenderPrefab)defaultRP;
        //            changeCount++;
        //            isHarborSet = true;
        //            LogHelper.SendLog("Fixed Harbor Mesh");
        //        }
        //        prefabSystem.UpdatePrefab(harbor_ext1_mesh);
        //        SetState();
        //    }
        //}

        public void FixUSSWHospital(bool active = true)
        {
            if (!systemReady)
                return;
            if (!IsActive(PackType.RP_USSW))
                return;
            if (!active && !isUSSWHospitalSet)
                return;
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "USSW_Hospital_16x14"),
                    out PrefabBase prefabBase
                )
            )
            {
                bool changed = false;
                if (!active && !firstPass && prefabBase.Has<ObjectSubObjects>())
                {
                    ObjectSubObjects objectSubObjects = prefabBase.GetComponent<ObjectSubObjects>();

                    prefabSystem.TryGetPrefab(
                        new PrefabID("MarkerPrefab", "Car Spawn Location"),
                        out PrefabBase carSpawnMarker
                    );

                    var subObjects = objectSubObjects.m_SubObjects.ToList();
                    subObjects.Add(
                        new ObjectSubObjectInfo() { m_Object = (ObjectPrefab)carSpawnMarker }
                    );

                    objectSubObjects.m_SubObjects = subObjects.ToArray();
                    changeCount--;
                    isUSSWHospitalSet = false;
                    changed = true;
                }
                else if (active && !prefabBase.Has<ObjectSubObjects>())
                {
                    ObjectSubObjects objectSubObjects = prefabBase.GetComponent<ObjectSubObjects>();
                    var subObjects = objectSubObjects.m_SubObjects.ToList();
                    subObjects.RemoveAll(oso => oso.m_Object.name == "Car Spawn Location");

                    objectSubObjects.m_SubObjects = subObjects.ToArray();
                    changeCount++;
                    isUSSWHospitalSet = true;
                    changed = true;
                }

                if (changed)
                {
                    UpdatePrefab(prefabBase);
                    SetState();
                }
            }
        }

        //public void FixExtractorSpawning(bool active = true)
        //{
        //    if (!active && !isExtractorsDisabled)
        //        return;
        //    if (!active && !firstPass)
        //    {
        //        foreach (KeyValuePair<Entity, ObjectPrefab[]> kvp in extractorsModified)
        //        {
        //            Entity entity = kvp.Key;
        //            ObjectPrefab[] objects = kvp.Value;

        //            if (!prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase))
        //            {
        //                continue;
        //            }
        //            if (prefabBase.TryGet(out SpawnableObject sp))
        //            {
        //                sp.m_Placeholders = objects;
        //            }
        //            extractorsModified.Remove(entity);
        //            prefabSystem.UpdatePrefab(prefabBase);
        //        }
        //        changeCount--;
        //        isExtractorsDisabled = false;
        //    }
        //    else if (active && !firstPass)
        //    {
        //        EntityQuery spawnableQuery = SystemAPI
        //            .QueryBuilder()
        //            .WithAll<SpawnableObjectData>()
        //            .WithAll<BuildingData>()
        //            .WithAll<ExtractorFacilityData>()
        //            .Build();
        //        var spawnableBuildings = spawnableQuery.ToEntityArray(Allocator.Temp);
        //        foreach (var entity in spawnableBuildings)
        //        {
        //            if (!prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase))
        //            {
        //                continue;
        //            }

        //            if (prefabBase != null)
        //            {
        //                if (prefabBase.TryGet(out SpawnableObject sp))
        //                {
        //                    if (!extractorsModified.ContainsKey(entity))
        //                        extractorsModified.Add(entity, sp.m_Placeholders);

        //                    Array.Resize(ref sp.m_Placeholders, 0);
        //                    prefabSystem.UpdatePrefab(prefabBase);
        //                }
        //            }
        //        }
        //        changeCount++;
        //        isExtractorsDisabled = true;
        //    }
        //}

        public void FixParkingLotSolar(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isSolarParkingSet)
                return;
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "ParkingLot12"),
                    out PrefabBase prefabBase
                )
                && prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "ParkingLot13"),
                    out PrefabBase prefabBase2
                )
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    if (prefabBase.Has<SolarPowered>() && prefabBase.Has<PowerPlant>())
                    {
                        prefabBase.Remove<SolarPowered>();
                        prefabBase.Remove<PowerPlant>();
                    }
                    if (prefabBase2.Has<SolarPowered>() && prefabBase2.Has<PowerPlant>())
                    {
                        prefabBase2.Remove<SolarPowered>();
                        prefabBase2.Remove<PowerPlant>();
                    }
                    changeCount--;
                    isSolarParkingSet = false;
                    changed = true;
                }
                else if (
                    active
                    && prefabSystem.TryGetPrefab(
                        new PrefabID("PowerLinePrefab", "Low-voltage Marker"),
                        out PrefabBase powerLine
                    )
                )
                {
                    float3 f3 = new(0, 0, 12);
                    ObjectSubNetInfo marker = new()
                    {
                        m_NetPrefab = (NetPrefab)powerLine,
                        m_BezierCurve = new Colossal.Mathematics.Bezier4x3(f3, f3, f3, f3),
                        m_NodeIndex = new int2(999, 999),
                        m_ParentMesh = new int2(-1, -1),
                    };

                    SolarPowered sp = prefabBase.AddOrGetComponent<SolarPowered>();
                    sp.m_Production = 2500;

                    PowerPlant pp = prefabBase.AddOrGetComponent<PowerPlant>();
                    pp.m_ElectricityProduction = 0;

                    ObjectSubNets osn = prefabBase.AddOrGetComponent<ObjectSubNets>();
                    List<ObjectSubNetInfo> osni = osn.m_SubNets.ToList();
                    osni.Add(marker);

                    osn.m_SubNets = osni.ToArray();

                    SolarPowered sp2 = prefabBase2.AddOrGetComponent<SolarPowered>();
                    sp2.m_Production = 7500;

                    PowerPlant pp2 = prefabBase2.AddOrGetComponent<PowerPlant>();
                    pp2.m_ElectricityProduction = 0;

                    ObjectSubNets osn2 = prefabBase2.AddOrGetComponent<ObjectSubNets>();
                    List<ObjectSubNetInfo> osni2 = osn2.m_SubNets.ToList();
                    osni2.Add(marker);

                    osn2.m_SubNets = osni2.ToArray();

                    changeCount++;
                    isSolarParkingSet = true;
                    changed = true;
                }

                if (changed)
                {
                    UpdatePrefab(prefabBase);
                    UpdatePrefab(prefabBase2);
                    SetState();
                }
            }
        }

        public void FixRSClinic(bool active = true)
        {
            if (!systemReady)
                return;
            if (!IsActive(PackType.DLC_MA))
                return;
            if (!active && !isRSClinicSet)
                return;

            prefabSystem.TryGetPrefab(
                new PrefabID("BuildingPrefab", "RS_MedicalClinic01"),
                out PrefabBase prefabBase1
            );
            prefabSystem.TryGetPrefab(
                new PrefabID("BuildingPrefab", "RS_MedicalClinic01_Sub01"),
                out PrefabBase prefabBase2
            );

            if (
                prefabBase1.TryGet(out StorageLimit storage1)
                && prefabBase2.TryGet(out StorageLimit storage2)
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    storage1.storageLimit = 100;
                    storage2.storageLimit = 100;

                    changeCount--;
                    isRSClinicSet = false;
                    changed = true;
                }
                else if (active)
                {
                    storage1.storageLimit = 1000;
                    storage2.storageLimit = 1000;

                    changeCount++;
                    isRSClinicSet = true;
                    changed = true;
                }
                if (changed)
                {
                    UpdatePrefab(prefabBase1);
                    UpdatePrefab(prefabBase2);
                    SetState();
                }
            }
        }

        public bool AddOn_FixSubNetDirection(string prefabName, bool active)
        {
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", prefabName),
                    out PrefabBase prefabBase
                ) && prefabBase.TryGet(out ObjectSubNets subNets)
            )
            {
                if (prefabName == "BusStation01")
                {
                    subNets.m_InvertWhen = active
                        ? NetInvertMode.RighthandTraffic
                        : NetInvertMode.Never;
                }
                else
                {
                    subNets.m_InvertWhen = active
                        ? NetInvertMode.LefthandTraffic
                        : NetInvertMode.Never;
                }
                UpdatePrefab(prefabBase);
                return true;
            }
            return false;
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
                prefabSystem.TryGetPrefab(
                    new PrefabID("ZonePrefab", "NL Residential Gambrel Low"),
                    out PrefabBase prefabBase1
                )
                && prefabBase1.TryGet(out ZoneProperties zoneProps1)
                && prefabSystem.TryGetPrefab(
                    new PrefabID("ZonePrefab", "NL Residential Gable Low"),
                    out PrefabBase prefabBase2
                )
                && prefabBase2.TryGet(out ZoneProperties zoneProps2)
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    zoneProps1.m_ResidentialProperties = 1;
                    zoneProps2.m_ResidentialProperties = 1;
                    changeCount--;
                    isNLLowHouseholdSet = false;
                    changed = true;
                }
                else if (active)
                {
                    zoneProps1.m_ResidentialProperties = 2;
                    zoneProps2.m_ResidentialProperties = 2;
                    changeCount++;
                    isNLLowHouseholdSet = true;
                    changed = true;
                }
                if (changed)
                {
                    UpdatePrefab(prefabBase1);
                    prefabSystem.TryGetEntity(prefabBase1, out Entity zoneEntity1);
                    prefabSystem.TryGetEntity(prefabBase2, out Entity zoneEntity2);
                    EntityQuery zoneBuildingsEntity = SystemAPI
                        .QueryBuilder()
                        .WithAll<SpawnableBuildingData>()
                        .Build();
                    var zoneBuildings = zoneBuildingsEntity.ToEntityArray(Allocator.Temp);
                    foreach (var building in zoneBuildings)
                    {
                        if (
                            EntityManager.TryGetComponent(building, out SpawnableBuildingData sbd)
                            && (sbd.m_ZonePrefab == zoneEntity1 || sbd.m_ZonePrefab == zoneEntity2)
                        )
                        {
                            EntityManager.TryGetComponent(building, out PrefabData prefabData);
                            prefabSystem.TryGetPrefab(prefabData, out PrefabBase pb);

                            UpdatePrefab(pb);
                        }
                    }

                    SetState();
                }
            }
        }

        public void FixAdditionalTransformers(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isAdditionalTransformersSet)
                return;
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID(
                        "BuildingPrefab",
                        "SolarPowerStation01 Additional Transformer Station"
                    ),
                    out PrefabBase additionalTransformer
                )
                && additionalTransformer.Has<ServiceUpgrade>()
                && prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "SolarPowerStation01"),
                    out PrefabBase solarPowerStation
                )
            )
            {
                bool changed = false;

                ServiceUpgrade su = additionalTransformer.GetComponent<ServiceUpgrade>();
                List<BuildingPrefab> bps = new() { (BuildingPrefab)solarPowerStation };

                List<string> buildings = new()
                {
                    "NuclearPowerPlant01",
                    "HydroelectricPowerPlant01",
                };

                if (!active && !firstPass)
                {
                    changeCount--;
                    isAdditionalTransformersSet = false;
                    changed = true;
                }
                else if (active)
                {
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        if (
                            prefabSystem.TryGetPrefab(
                                new PrefabID("BuildingPrefab", buildings[i]),
                                out PrefabBase powerPrefab
                            ) && powerPrefab.Has<PowerPlant>()
                        )
                            bps.Add((BuildingPrefab)powerPrefab);
                    }

                    changeCount++;
                    isAdditionalTransformersSet = true;
                    changed = true;
                }
                if (changed)
                {
                    su.m_Buildings = bps.ToArray();

                    foreach (var building in bps)
                    {
                        building.AddUpgrade(EntityManager, su);
                        //UpdatePrefab(building);
                    }
                    UpdatePrefab(additionalTransformer);

                    SetState();
                }
            }
        }
    }
}
