using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
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
            if (mode == GameMode.Editor)
            {
                FixPrisonBus01(false);
                FixPrison01(false);
            }

            if (mode != GameMode.Game)
                return;
            Setting settings = Mod.m_Setting;
            if (settings.PrisonVan)
                FixPrisonBus01();
            if (settings.Prison)
                FixPrison01();
            firstPass = false;
            SetState();
        }

        protected override void OnUpdate() { }

        public void SetState()
        {
            if (changeCount == 2)
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
                }
                else if (active)
                {
                    prisonVan.m_SizeClass = Game.Vehicles.SizeClass.Large;
                    changeCount++;
                }
                prefabSystem.UpdatePrefab(prisonVan01);
                SetState();
            }
        }

        public void FixPrison01(bool active = true)
        {
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
                }
                else if (active)
                {
                    prison.m_PrisonVanCapacity = 20;
                    changeCount++;
                }
                prefabSystem.UpdatePrefab(prison01);
                SetState();
            }
        }
    }
}
