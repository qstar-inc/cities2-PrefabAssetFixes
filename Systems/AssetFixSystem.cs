using System;
using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

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
        private readonly List<Entity> addedStorageLimit = new();
        private readonly List<Entity> addedCargoTransport = new();
        private readonly Dictionary<string, float> polePositions = new();

        private bool isPrisonSet = false;
        private bool isPrisonVanSet = false;
        private bool isStorageSet = false;
        private bool isHospitalSet = false;
        private bool isPolesSet = false;

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

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            firstPass = false;
            //#if DEBUG
            //#else
            if (mode == GameMode.Editor)
            {
                FixPrisonBus01(false);
                FixPrison01(false);
                FixStorageMissing(false, false);
                FixHostipal01(false);
                FixHoveringPoles(false);
            }
            if (mode != GameMode.Game)
                return;
            //#endif
            StartFixes();
            SetState();
        }

        private void StartFixes()
        {
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
        }

        protected override void OnUpdate() { }

        public void SetState()
        {
            if (changeCount == 6)
            {
                Mod.State = "All changes set";
                Mod.log.Info(changeCount);
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
            if (!active && !isPrisonVanSet)
                return;
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("CarPrefab", "PrisonVan01"),
                    out PrefabBase prisonVan01
                ) && prisonVan01.TryGet(out CarPrefab prisonVan)
            )
            {
                if (!active && !firstPass)
                {
                    prisonVan.m_SizeClass = Game.Vehicles.SizeClass.Medium;
                    changeCount--;
                    isPrisonVanSet = false;
                }
                else if (active)
                {
                    prisonVan.m_SizeClass = Game.Vehicles.SizeClass.Large;
                    changeCount++;
                    isPrisonVanSet = true;
                }
                prefabSystem.UpdatePrefab(prisonVan01);
                SetState();
            }
        }

        public void FixPrison01(bool active = true)
        {
            if (!active && !isPrisonSet)
                return;
            if (
                prefabSystem.TryGetPrefab(
                    new PrefabID("BuildingPrefab", "Prison01"),
                    out PrefabBase prison01
                ) && prison01.TryGet(out Prison prison)
            )
            {
                if (!active && !firstPass)
                {
                    prison.m_PrisonVanCapacity = 10;
                    changeCount--;
                    isPrisonSet = false;
                }
                else if (active)
                {
                    prison.m_PrisonVanCapacity = 20;
                    changeCount++;
                    isPrisonSet = true;
                }
                prefabSystem.UpdatePrefab(prison01);
                SetState();
            }
        }

        public void FixHostipal01(bool active = true)
        {
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
                    out PrefabBase hospital01
                ) && hospital01.TryGet(out ObjectSubObjects hospital)
            )
            {
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
                }
                prefabSystem.UpdatePrefab(hospital01);
                SetState();
            }
        }

        public void FixStorageMissing(bool storageActive = true, bool recyclingActive = true)
        {
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
                    addedStorageLimit.Remove(entity);
                }
                foreach (Entity entity in addedCargoTransport)
                {
                    if (!prefabSystem.TryGetPrefab(entity, out PrefabBase prefabBase))
                    {
                        continue;
                    }
                    prefabBase.Remove<CargoTransportStation>();
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
                        string name = $"{prefabSystem.GetPrefabName(entity)}";
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
                            if (!addedCargoTransport.Contains(entity))
                                addedCargoTransport.Add(entity);
                            prefabSystem.UpdatePrefab(prefabBase);
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
                                prefabSystem.UpdatePrefab(prefabBase);
                            }
                        }
                    }
                }
                changeCount++;
                isStorageSet = true;
            }
        }

        public void FixHoveringPoles(bool active = true)
        {
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

            bool anyChanged = false;

            foreach (string prefabName in prefabNames)
            {
                if (FixPolesInRP(prefabName, active))
                    anyChanged = true;
            }

            if (anyChanged)
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
                    prefabSystem.UpdatePrefab(prefabBase);

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
    }
}
