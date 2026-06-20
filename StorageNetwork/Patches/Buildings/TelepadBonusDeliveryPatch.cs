using System.Collections;
using HarmonyLib;
using StorageNetwork.Buildings;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class TelepadBonusDeliveryPatch
    {
        private const int StartingEngravingDiskCount = 3;

        [HarmonyPatch(typeof(Telepad.StatesInstance), nameof(Telepad.StatesInstance.SpawnExtraPowerBanks))]
        public static class SpawnExtraPowerBanksPatch
        {
            public static IEnumerator Postfix(IEnumerator __result, Telepad.StatesInstance __instance)
            {
                while (__result != null && __result.MoveNext())
                {
                    yield return __result.Current;
                }

                Telepad telepad = __instance?.GetComponent<Telepad>();
                if (telepad == null)
                {
                    yield break;
                }

                GameObject prefab = Assets.GetPrefab(StorageNetworkEngravingDiskConfig.ID);
                if (prefab == null)
                {
                    Debug.LogWarning("[StorageNetwork] Engraving disk prefab is not registered; cannot spawn starting disks.");
                    yield break;
                }

                int cellTarget = Grid.OffsetCell(Grid.PosToCell(telepad.gameObject), 1, 2);
                Vector3 spawnPosition = Grid.CellToPosCBC(cellTarget, Grid.SceneLayer.Front) - Vector3.right / 2f;
                string diskName = STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.NAME);

                for (int i = 0; i < StartingEngravingDiskCount; i++)
                {
                    PopFXManager.Instance.SpawnFX(
                        PopFXManager.Instance.sprite_Plus,
                        diskName,
                        telepad.gameObject.transform,
                        new Vector3(0f, 0.5f, 0f),
                        1.5f,
                        false,
                        false);
                    KMonoBehaviour.PlaySound(GlobalAssets.GetSound("SandboxTool_Spawner", false));

                    GameObject disk = Util.KInstantiate(prefab, spawnPosition);
                    disk.SetActive(true);

                    Vector2 velocity = new Vector2((-1f + i) * 1.75f, 2.25f);
                    if (GameComps.Fallers.Has(disk))
                    {
                        GameComps.Fallers.Remove(disk);
                    }

                    GameComps.Fallers.Add(disk, velocity);
                    yield return new WaitForSeconds(0.18f);
                }
            }
        }
    }
}
