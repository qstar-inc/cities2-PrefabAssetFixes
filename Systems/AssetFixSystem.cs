using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Game;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.UI.InGame;
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
        private readonly List<Entity> addedTransportStop = new();

        private bool isPrisonSet = false;
        private bool isPrisonVanSet = false;
        private bool isStorageSet = false;
        private bool isHospitalSet = false;

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
            if (mode == GameMode.Editor)
            {
                FixPrisonBus01(false);
                FixPrison01(false);
                FixStoarageMissing(false);
            }

            if (mode != GameMode.Game)
                return;
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
            if (settings.Storage)
                FixStoarageMissing();
            if (settings.Hospital)
                FixHostipal01();
        }

        protected override void OnUpdate() { }

        public void SetState()
        {
            if (changeCount == 4)
            {
                Mod.State = "All changes set";
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

        public void FixStoarageMissing(bool active = true)
        {
            if (!active && !isStorageSet)
                return;
            if (!active && !firstPass)
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
            else if (active && !firstPass)
            {
                EntityQuery storageBuildingsQuery = SystemAPI
                    .QueryBuilder()
                    .WithAny<ResourceProductionData, StorageLimitData>()
                    .WithNone<CompanyBrandElement, OutsideConnectionData>()
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
                            //    prefabBase.TryGet(out CargoTransportStation cts)
                            //    && !prefabBase.TryGet(out StorageLimit _)
                            //)
                            //{
                            //    Mod.log.Info($"{name} has cts but no stl");
                            //}
                            //else if (
                            !prefabBase.TryGet(out CargoTransportStation _)
                            && prefabBase.TryGet(out StorageLimit _)
                        )
                        {
                            // add ctl by product
                            prefabBase.AddComponent<CargoTransportStation>();
                            if (!addedCargoTransport.Contains(entity))
                                addedCargoTransport.Add(entity);
                            prefabSystem.UpdatePrefab(prefabBase);
                            //Mod.log.Info($"{name} has stl but no ctl, added ctl");
                        }
                        else if (
                            !prefabBase.TryGet(out CargoTransportStation _)
                            && !prefabBase.TryGet(out StorageLimit _)
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
                                    var tpStop = prefabBase.AddComponent<TransportStop>();
                                    tpStop.m_AccessConnectionType = RouteConnectionType.None;
                                    tpStop.m_RouteConnectionType = RouteConnectionType.Cargo;
                                    tpStop.m_AccessRoadType = Game.Net.RoadTypes.Car;
                                    tpStop.m_CargoTransport = true;
                                    tpStop.m_PassengerTransport = false;
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
    }
}
