using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Game;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PrefabAssetFixes.Systems
{
    public partial class AssetFixSystem : GameSystemBase
    {
#nullable disable
        public PrefabSystem prefabSystem;
#nullable enable
        private EntityQuery carDataQuery;
        private int changeCount = 0;
        private bool firstPass = true;
        public bool systemReady = false;
        private readonly List<Entity> addedStorageLimit = new();
        private readonly List<Entity> addedCargoTransport = new();
        private readonly Dictionary<string, float> polePositions = new();
        private readonly Dictionary<Entity, ObjectPrefab[]> extractorsModified = new();

        private bool isPrisonSet = false;
        private bool isPrisonVanSet = false;
        private bool isStorageSet = false;
        private bool isHospitalSet = false;
        private bool isPolesSet = false;
        private bool isUSSWHospitalSet = false;

        //private bool isExtractorsDisabled = false;
        private bool isSolarParkingSet = false;

        //private bool isHarborSet = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
            if (Game.Version.current != new Colossal.Version(1, 301797254804627269L, 76449216))
            {
                Mod.State = "Game version not compatible, only for 1.3.3f1.";
                return;
            }

            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            if (!systemReady)
                return;
            //#if DEBUG
            //#else
            if (mode == GameMode.Editor)
            {
                FixPrisonBus01(false);
                FixPrison01(false);
                FixStorageMissing(false, false);
                FixHostipal01(false);
                FixHoveringPoles(false);
                FixUSSWHospital(false);
                //FixExtractorSpawning(false);
                FixParkingLotSolar(false);
            }
            if (mode != GameMode.Game)
                return;
            //#endif
            StartFixes();
            SetState();
            firstPass = false;
        }

        private void StartFixes()
        {
            if (!systemReady)
                return;
            Setting settings = Mod.m_Setting;
            if (settings.PrisonVan)
                FixPrisonBus01();
            if (settings.Prison)
                FixPrison01();
            if (settings.Storage || settings.Recycling)
                FixStorageMissing(settings.Storage, settings.Recycling);
            if (settings.Hospital)
                FixHostipal01();
            if (settings.HoveringPoles)
                FixHoveringPoles();
            //FixHarbourExtBase();
            if (settings.USSWHospital)
                FixUSSWHospital();
            if (settings.SolarParking)
                FixParkingLotSolar();
            //if (1 == 1)
            //{
            //    //FixExtractorSpawning(false);
            //    FixOfficeLowStorage();
            //}
        }

        protected override void OnUpdate() { }

        public void SetState()
        {
            if (changeCount == 7)
            {
                Mod.State = "All changes set";
                //Mod.log.Info(changeCount);
            }
            else if (changeCount == 0)
            {
                Mod.State = "No changes set";
            }
            else
            {
                Mod.State = "Some changes set";
            }
        }

        public void FixPrisonBus01(bool active = true)
        {
            if (!systemReady)
                return;
            if (!active && !isPrisonVanSet)
                return;
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("CarPrefab", "PrisonVan01"),
                    out PrefabBase prefabBase
                ) && prefabBase.TryGet(out CarPrefab prisonVan)
            )
            {
                bool changed = false;
                if (!active && !firstPass)
                {
                    prisonVan.m_SizeClass = Game.Vehicles.SizeClass.Medium;
                    changeCount--;
                    isPrisonVanSet = false;
                    changed = true;
                }
                else if (active)
                {
                    prisonVan.m_SizeClass = Game.Vehicles.SizeClass.Large;
                    changeCount++;
                    isPrisonVanSet = true;
                    changed = true;
                }
                if (changed)
                {
                    UpdatePrefab(prefabBase);
                    SetState();
                }
            }
        }

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
                ) && prefabBase.TryGet(out Prison prison)
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

        public void FixStorageMissing(bool storageActive = true, bool recyclingActive = true)
        {
            if (!systemReady)
                return;
            if (!storageActive && !recyclingActive && !isStorageSet)
                return;
            if (!(storageActive || recyclingActive) && !firstPass)
            {
                foreach (Entity entity in addedStorageLimit)
                {
                    if (!prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase))
                    {
                        continue;
                    }
                    prefabBase.Remove<StorageLimit>();
                    UpdatePrefab(prefabBase);
                    addedStorageLimit.Remove(entity);
                }
                foreach (Entity entity in addedCargoTransport)
                {
                    if (!prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase))
                    {
                        continue;
                    }
                    prefabBase.Remove<CargoTransportStation>();
                    UpdatePrefab(prefabBase);
                    addedCargoTransport.Remove(entity);
                }
                changeCount--;
                isStorageSet = false;
            }
            else if ((storageActive || recyclingActive) && !firstPass)
            {
                EntityQuery storageBuildingsQuery = SystemAPI
                    .QueryBuilder()
                    .WithAny<ResourceProductionData, StorageLimitData>()
                    .WithNone<CompanyBrandElement, OutsideConnectionData, ServiceUpgradeData>()
                    .Build();
                var storageBuildings = storageBuildingsQuery.ToEntityArray(Allocator.Temp);
                foreach (var entity in storageBuildings)
                {
                    if (!prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase))
                    {
                        continue;
                    }

                    if (prefabBase != null)
                    {
                        //string name = $"{prefabSystem.GetPrefabName(entity)}";
                        if (
                            storageActive
                            && !prefabBase.Has<CargoTransportStation>()
                            && prefabBase.Has<StorageLimit>()
                        )
                        {
                            // add ctl by product
                            CargoTransportStation cts =
                                prefabBase.AddComponent<CargoTransportStation>();
                            cts.transports = 1;
                            cts.m_TransportInterval = new Unity.Mathematics.int2(0, 400);
                            if (!addedCargoTransport.Contains(entity))
                                addedCargoTransport.Add(entity);
                            UpdatePrefab(prefabBase);
                            //Mod.log.Info($"{name} has stl but no ctl, added ctl");
                        }
                        else if (
                            recyclingActive
                            && !prefabBase.Has<CargoTransportStation>()
                            && !prefabBase.Has<StorageLimit>()
                        )
                        {
                            int storageValue = 0;
                            List<ResourceInEditor> res = new();
                            if (prefabBase.TryGet(out ResourceProducer rp))
                            {
                                bool isRecycling = false;
                                if (prefabBase.TryGet(out GarbageFacility grbg))
                                {
                                    isRecycling = true;
                                    storageValue += grbg.m_GarbageCapacity;
                                    if (!prefabBase.Has<TransportStop>())
                                    {
                                        var tpStop = prefabBase.AddComponent<TransportStop>();
                                        tpStop.m_AccessConnectionType = RouteConnectionType.None;
                                        tpStop.m_RouteConnectionType = RouteConnectionType.Cargo;
                                        tpStop.m_AccessRoadType = Game.Net.RoadTypes.Car;
                                        tpStop.m_CargoTransport = true;
                                        tpStop.m_PassengerTransport = false;
                                    }
                                    res.Add(ResourceInEditor.Money);
                                }
                                for (int rrr = 0; rrr < rp.m_Resources.Length; rrr++)
                                {
                                    var rpr = rp.m_Resources[rrr];
                                    storageValue += rpr.m_StorageCapacity;
                                    if (!isRecycling)
                                    {
                                        res.Add(rpr.m_Resource);
                                    }
                                }

                                if (storageValue == 0)
                                {
                                    storageValue = 1;
                                }

                                var stlN = prefabBase.AddComponent<StorageLimit>();
                                stlN.storageLimit = storageValue;
                                if (!addedStorageLimit.Contains(entity))
                                    addedStorageLimit.Add(entity);
                                var ctsN = prefabBase.AddComponent<CargoTransportStation>();
                                ctsN.m_TradedResources = res.ToArray();
                                if (!addedCargoTransport.Contains(entity))
                                    addedCargoTransport.Add(entity);
                                //Mod.log.Info(
                                //    $"{name} has no stl (added {storageValue}) and no ctl (added)"
                                //);
                                UpdatePrefab(prefabBase);
                            }
                        }
                    }
                }
                changeCount++;
                isStorageSet = true;
                SetState();
            }
        }

        public void FixHoveringPoles(bool active = true)
        {
            if (!systemReady)
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
                if (FixPolesInRP(prefabName, active))
                    changed = true;
            }

            if (changed)
            {
                changeCount += active ? 1 : -1;
                isPolesSet = active;
                SetState();
            }
        }

        public bool FixPolesInRP(string prefabName, bool active)
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
                                //Mod.log.Info($"Reverted {prefabName} poles");
                            }
                            else if (active && Math.Round(obj.m_Position.y) == 135f)
                            {
                                obj.m_Position.y = 124f;
                                modified = true;
                                //Mod.log.Info($"Fixed {prefabName} poles");
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
                                //Mod.log.Info($"Reverted {prefabName} {name}");
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
                                //Mod.log.Info($"Fixed {prefabName} {name}");
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
        //            Mod.log.Info("Reverted Harbor Mesh");
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
        //            Mod.log.Info("Fixed Harbor Mesh");
        //        }
        //        prefabSystem.UpdatePrefab(harbor_ext1_mesh);
        //        SetState();
        //    }
        //}

        public void FixUSSWHospital(bool active = true)
        {
            if (!systemReady)
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

        public void UpdatePrefab(PrefabBase prefabBase)
        {
            prefabSystem.UpdatePrefab(prefabBase);
            Mod.log.Info($"{prefabBase.name} updated");
        }
    }
}
