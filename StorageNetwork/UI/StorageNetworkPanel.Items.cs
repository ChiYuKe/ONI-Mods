using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {


        private void DropSelectedItem(Storage storage, string itemKey, float requestedMass)
        {
            LogDebug(string.Format(
                "DropSelectedItem begin storage={0} storageGO={1} itemKey={2} requestedMass={3:0.###}",
                storage != null ? storage.GetInstanceID().ToString() : "null",
                storage != null ? storage.gameObject.GetProperName() : "null",
                itemKey,
                requestedMass));

            List<GameObject> items = FindStoredItems(storage, itemKey);
            LogDebug(string.Format("DropSelectedItem matched items={0}", items.Count));
            if (storage == null || items.Count == 0)
            {
                selectedItemStorage = null;
                selectedItemKey = null;
                LogDebug("DropSelectedItem abort: missing storage or items");
                Refresh(true);
                return;
            }

            Tag dropTag = GetStoredItemTag(items[0]);
            float mass = Mathf.Min(requestedMass, GetStoredItemsMass(items));
            LogDebug(string.Format(
                "DropSelectedItem resolved tag={0} tagValid={1} mass={2:0.###} firstItem={3}",
                dropTag,
                dropTag.IsValid,
                mass,
                items[0] != null ? items[0].GetProperName() : "null"));

            bool dropped = dropTag.IsValid && mass > 0f && storage.DropSome(dropTag, mass, false, false, default(Vector3), true, true);
            LogDebug(string.Format("DropSelectedItem DropSome result={0} remainingItems={1}", dropped, storage.items.Count));

            if (!dropped)
            {
                foreach (GameObject item in items.ToList())
                {
                    GameObject droppedItem = storage.Drop(item, true);
                    LogDebug(string.Format(
                        "DropSelectedItem fallback Drop item={0} result={1}",
                        item != null ? item.GetProperName() : "null",
                        droppedItem != null ? droppedItem.GetProperName() : "null"));
                    if (droppedItem != null)
                    {
                        droppedItem.transform.SetPosition(storage.transform.GetPosition());
                    }
                }
            }

            selectedItemStorage = null;
            selectedItemKey = null;
            targetHub?.RefreshNetwork();
            lastListSignature = null;
            Refresh(true);
            LogDebug("DropSelectedItem end");
        }

        private void TransferSelectedItem(Storage source, string itemKey, Storage destination, float requestedMass)
        {
            List<GameObject> items = FindStoredItems(source, itemKey);
            if (source == null || destination == null || items.Count == 0)
            {
                Refresh(true);
                return;
            }

            Tag tag = GetStoredItemTag(items[0]);
            float maxTransfer = Mathf.Min(GetStoredItemsMass(items), Mathf.Max(0f, destination.RemainingCapacity()));
            float remaining = Mathf.Clamp(requestedMass, 0f, maxTransfer);
            float transferred = 0f;

            while (tag.IsValid && remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                float moved = source.Transfer(destination, tag, remaining, block_events: false, hide_popups: true);
                if (moved <= 0f)
                {
                    break;
                }

                transferred += moved;
                remaining -= moved;
            }

            LogDebug(string.Format(
                "TransferSelectedItem source={0} destination={1} tag={2} requested={3:0.###} transferred={4:0.###}",
                source.gameObject.GetProperName(),
                destination.gameObject.GetProperName(),
                tag,
                requestedMass,
                transferred));

            selectedItemStorage = null;
            selectedItemKey = null;
            targetHub?.RefreshNetwork();
            lastListSignature = null;
            Refresh(true);
        }

        private static float GetStoredItemsMass(IEnumerable<GameObject> items)
        {
            return items.Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f);
        }

        private static List<GameObject> FindStoredItems(Storage storage, string itemKey)
        {
            return storage?.items
                .Where(item => item != null && GetStoredItemKey(item) == itemKey)
                .ToList() ?? new List<GameObject>();
        }

        private static Tag GetStoredItemTag(GameObject item)
        {
            if (item == null)
            {
                return Tag.Invalid;
            }

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                return prefabId.PrefabID();
            }

            PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
            return primaryElement != null ? primaryElement.ElementID.CreateTag() : Tag.Invalid;
        }

        private static string GetStoredItemKey(GameObject item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                return prefabId.PrefabID().ToString();
            }

            PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
            return primaryElement != null ? primaryElement.ElementID.ToString() : item.name;
        }

        private static string GetStorageTypeKey(StorageNetworkStorageInfo storageInfo)
        {
            KPrefabID prefabId = storageInfo.Storage?.GetComponent<KPrefabID>();
            return prefabId != null ? prefabId.PrefabID().ToString() : GetStorageTypeName(storageInfo);
        }

        private static string GetStorageTypeName(StorageNetworkStorageInfo storageInfo)
        {
            GameObject gameObject = storageInfo.Storage?.gameObject;
            return gameObject != null ? gameObject.GetProperName() : storageInfo.Name;
        }

        private static string GetStoredItemName(GameObject item)
        {
            return item != null ? item.GetProperName() : string.Empty;
        }

        private static void SetStoredItemIcon(Image icon, GameObject item)
        {
            if (icon == null || item == null)
            {
                return;
            }

            Sprite sprite = null;
            Color tint = Color.white;

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                var uiSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                sprite = uiSprite.first;
                tint = uiSprite.second;
            }

            if (sprite == null)
            {
                PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                if (primaryElement != null)
                {
                    var uiSprite = Def.GetUISprite(primaryElement.ElementID.CreateTag(), "ui", false);
                    sprite = uiSprite.first;
                    tint = uiSprite.second;
                }
            }

            icon.sprite = sprite;
            icon.color = sprite != null ? tint : Color.clear;
        }

        private static void FocusStorage(Storage storage)
        {
            if (storage == null || SelectTool.Instance == null)
            {
                return;
            }

            KSelectable selectable = storage.GetComponent<KSelectable>();
            if (selectable != null)
            {
                SelectTool.Instance.SelectAndFocus(storage.transform.position, selectable, Vector3.zero);
            }
        }
    }
}
