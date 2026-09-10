using System;
using System.Collections.Generic;
using System.IO;
using OpenTS2.Common;
using OpenTS2.Components;
using OpenTS2.Content;
using OpenTS2.Content.DBPF;
using OpenTS2.Content.DBPF.Scenegraph;
using OpenTS2.Files;
using OpenTS2.Files.Formats.DBPF;
using OpenTS2.Scenes.Lot;
using OpenTS2.Scenes.Lot.State;
using UnityEngine;

namespace OpenTS2.Engine.Tests
{
    public class LotLoadingTest : MonoBehaviour
    {
        public string NeighborhoodPrefix = "N001";
        public int LotID = 82;

        public int Floor = 5;
        public WallsMode Mode = WallsMode.Roof;

        // When enabled, spawns a base sim on the ground floor and walks it to a nearby object
        // along a routed path - a visual check of the routing subsystem.
        public bool RouteDemo = false;

        public int BaseFloor => _architecture?.BaseFloor ?? 0;
        public int MaxFloor => _architecture?.FloorPatterns.Depth ?? 1;

        private WorldState _state = new WorldState(5, WallsMode.Roof);

        private string _nhood;
        private int _lotId;

        private List<GameObject> _lotObject = new List<GameObject>();
        private List<GameObject> _testObjects = new List<GameObject>();
        private LotArchitecture _architecture;

        private void UnloadLot()
        {
            foreach (var obj in _lotObject)
            {
                Destroy(obj);
            }

            _lotObject.Clear();
            _testObjects.Clear();
        }

        private void Start()
        {
            Core.InitializeCore();

            ContentLoading.LoadGameContentSync();

            // Load effects.
            EffectsManager.Instance.Initialize();
            CatalogManager.Instance.Initialize();

            LoadLot(NeighborhoodPrefix, LotID);
        }

        public void Changed()
        {
            if (NeighborhoodPrefix != _nhood)
            {
                StartCoroutine(ReloadLot());
            }
            else if (LotID != _lotId)
            {
                StartCoroutine(ReloadLot());
            }

            WorldState state = new WorldState(Floor, Mode);

            if (!state.Equals(_state))
            {
                _architecture.UpdateState(state);
                UpdateObjectVisibility(state.Level);

                _state = state;
            }
        }

        System.Collections.IEnumerator ReloadLot()
        {
            yield return new WaitForFixedUpdate();

            if (NeighborhoodPrefix != _nhood || LotID != _lotId)
            {
                UnloadLot();
                LoadLot(NeighborhoodPrefix, LotID);
            }
        }

        private void SetObjectVisiblity(GameObject obj, bool visible)
        {
            foreach (MeshRenderer renderer in obj.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode = visible ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            foreach (SkinnedMeshRenderer renderer in obj.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.shadowCastingMode = visible ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        private void UpdateObjectVisibility(int viewLevel)
        {
            foreach (GameObject obj in _testObjects)
            {
                int level = _architecture.GetLevelAt(obj.transform.GetChild(0).localPosition);

                SetObjectVisiblity(obj, level < viewLevel);
            }
        }

        private void LoadLot(string neighborhoodPrefix, int id)
        {
            _nhood = neighborhoodPrefix;
            _lotId = id;

            var contentManager = ContentManager.Instance;

            var lotsFolderPath = Path.Combine(Filesystem.UserDataDirectory, $"Neighborhoods/{NeighborhoodPrefix}/Lots");
            var lotFilename = $"{NeighborhoodPrefix}_Lot{LotID}.package";
            var lotFullPath = Path.Combine(lotsFolderPath, lotFilename);

            if (!File.Exists(lotFullPath))
            {
                return;
            }

            var lotPackage = contentManager.AddPackage(lotFullPath);

            // Go through each lot object.
            foreach (var entry in lotPackage.Entries)
            {
                if (entry.GlobalTGI.TypeID != TypeIDs.LOT_OBJECT)
                {
                    continue;
                }

                try
                {
                    var lotObject = entry.GetAsset<LotObjectAsset>();
                    GameObject model;

                    if (lotObject.Object is LotObjectAsset.PersonObject)
                    {
                        // Sims have no named base resource - their body/outfit is assembled from CAS
                        // outfit data in a dedicated per-sim package instead.
                        model = SimCharacterComponent.CreatePersonOutfit(neighborhoodPrefix, entry.TGI.InstanceID, entry.GlobalTGI);
                        if (model == null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // Empty resource name implies no scenegraph resource.
                        if (lotObject.Object.ResourceName == "")
                        {
                            continue;
                        }
                        var resource = contentManager.GetAsset<ScenegraphResourceAsset>(
                            new ResourceKey(lotObject.Object.ResourceName + "_cres", GroupIDs.Scenegraph,
                                TypeIDs.SCENEGRAPH_CRES));
                        if (resource == null)
                        {
                            Debug.Log($"Could not find lot object: {lotObject.Object.ResourceName} / TGI: {entry.GlobalTGI}");
                            continue;
                        }

                        model = resource.CreateRootGameObject();
                    }

                    model.transform.GetChild(0).localPosition = lotObject.Object.Position;
                    model.transform.GetChild(0).localRotation = lotObject.Object.Rotation;

                    _lotObject.Add(model);
                    _testObjects.Add(model);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _architecture = new LotArchitecture();

            _architecture.LoadFromPackage(lotPackage);
            _architecture.CreateGameObjects(_lotObject);

            Floor = MaxFloor + BaseFloor;
            Mode = WallsMode.Roof;
            _state = new WorldState(Floor, Mode);
            _architecture.UpdateState(_state);

            if (RouteDemo)
                RunRouteDemo();
        }

        // Walks a base sim along a routed path on the ground floor as a visual routing check.
        private void RunRouteDemo()
        {
            var elevation = _architecture.Elevation;
            var tilesW = elevation.Width - 1;
            var tilesH = elevation.Height - 1;

            var grid = new OpenTS2.SimAntics.Routing.PathfindingGrid(tilesW, tilesH);
            var objectTiles = new List<OpenTS2.SimAntics.Routing.GridPosition>();
            foreach (var obj in _testObjects)
            {
                var lp = obj.transform.GetChild(0).localPosition;
                if (lp == Vector3.zero || _architecture.GetLevelAt(lp) != 0)
                    continue;
                objectTiles.Add(OpenTS2.SimAntics.Routing.LotTileCoordinates.WorldToTile(lp.x, lp.y));
            }
            OpenTS2.SimAntics.Routing.PathfindingGridBuilder.BlockTiles(grid, objectTiles);

            OpenTS2.SimAntics.Routing.GridPosition? start = null;
            for (var y = 0; y < tilesH && start == null; y++)
                for (var x = 0; x < tilesW; x++)
                    if (!grid.IsBlocked(x, y))
                    {
                        start = new OpenTS2.SimAntics.Routing.GridPosition(x, y);
                        break;
                    }

            if (start == null)
            {
                Debug.Log("RouteDemo: no walkable start tile");
                return;
            }

            List<OpenTS2.SimAntics.Routing.GridPosition> path = null;
            var chosen = default(OpenTS2.SimAntics.Routing.GridPosition);
            foreach (var tile in objectTiles)
            {
                var candidate = OpenTS2.SimAntics.Routing.AStarPathfinder.FindPathAdjacentTo(grid, start.Value, tile);
                if (candidate != null && candidate.Count > 5)
                {
                    path = candidate;
                    chosen = tile;
                    break;
                }
            }

            if (path == null)
            {
                Debug.Log("RouteDemo: no route to any object found");
                return;
            }

            // The lot converts data space (Z-up) to world (Y-up) via a rotation carried on each
            // object's root, with the data position on its child. Reuse that same rotation so the
            // follower can work in data space.
            var sample = _testObjects[0];
            var conversionRoot = new GameObject("RouteDemo_root");
            conversionRoot.transform.SetPositionAndRotation(sample.transform.position, sample.transform.rotation);

            var sim = SimCharacterComponent.CreateNakedBaseSim();
            var simObject = sim.gameObject;
            simObject.name = "RouteDemo_sim";
            simObject.transform.SetParent(conversionRoot.transform, false);
            // The VM owns the logical movement (one tile per tick); the renderer reflects it.
            var vm = new OpenTS2.SimAntics.VM { RoutingGrid = grid };
            var simDefinition = new ObjectDefinitionAsset { TGI = new ResourceKey(1, 1, TypeIDs.OBJD) };
            var entity = new OpenTS2.SimAntics.VMEntity(simDefinition);
            vm.AddEntity(entity);
            entity.TileX = start.Value.X;
            entity.TileY = start.Value.Y;
            var routeHandler = new OpenTS2.SimAntics.VMRouteHandler(entity, chosen);

            var entityRenderer = simObject.AddComponent<OpenTS2.SimAntics.Routing.VMEntityRenderer>();
            entityRenderer.Entity = entity;
            entityRenderer.Speed = 3f;
            entityRenderer.HeightAt = (x, y) => _architecture.GetFloorHeightAt(x, y, 0);
            _lotObject.Add(conversionRoot);

            PlayWalkAnimation(sim);
            StartCoroutine(DriveRoute(routeHandler, entityRenderer));

            Debug.Log($"RouteDemo: VM walking sim from {start} to {chosen} along {path.Count} tiles");
        }

        // Advances the route's logical tile once the sim has visibly reached the current tile,
        // keeping movement smooth and on-path.
        private System.Collections.IEnumerator DriveRoute(OpenTS2.SimAntics.VMRouteHandler handler,
            OpenTS2.SimAntics.Routing.VMEntityRenderer entityRenderer)
        {
            var code = handler.Tick();
            while (code == OpenTS2.SimAntics.VMExitCode.Continue)
            {
                yield return null;
                if (entityRenderer.AtTargetTile())
                    code = handler.Tick();
            }
        }

        private void PlayWalkAnimation(SimCharacterComponent sim)
        {
            var content = ContentManager.Instance;
            var key = new ResourceKey("a-male-walk-alt0-normal_anim", GroupIDs.Scenegraph, TypeIDs.SCENEGRAPH_ANIM);

            var anim = content.GetAsset<ScenegraphAnimationAsset>(key);
            if (anim == null)
            {
                // Sim animations live in the Sims3D packages, which the lot test may not have loaded.
                content.AddPackages(Filesystem.GetPackagesInDirectory(
                    Path.Combine(Filesystem.GetPathForProduct(ProductFlags.BaseGame), "TSData/Res/Sims3D")));
                anim = content.GetAsset<ScenegraphAnimationAsset>(key);
            }
            if (anim == null)
            {
                Debug.Log("RouteDemo: walk animation not found");
                return;
            }

            var animationObj = sim.GetComponentInChildren<Animation>();
            if (animationObj == null)
            {
                Debug.Log("RouteDemo: sim has no Animation component");
                return;
            }

            var clip = anim.CreateClipFromResource(sim.Scenegraph.BoneNamesToRelativePaths, sim.Scenegraph.BlendNamesToRelativePaths);
            clip.legacy = true;
            clip.wrapMode = WrapMode.Loop;
            animationObj.AddClip(clip, "walk");
            sim.AdjustInverseKinematicWeightsForAnimation(anim.AnimResource);
            animationObj.Play("walk");
        }
    }
}