using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STRINGS;
using UnityEngine;

namespace RunningOutOfTime.Content.Components
{
    public class Ownable : Assignable, ISaveLoadable, IGameObjectEffectDescriptor
    {

        // 用于给这个对象分配一个拥有者
        public override void Assign(IAssignableIdentity new_assignee)
        {
            if (new_assignee == this.assignee)
            {
                return;
            }
            if (base.slot != null && new_assignee is MinionIdentity)
            {
                new_assignee = (new_assignee as MinionIdentity).assignableProxy.Get();
            }
            if (base.slot != null && new_assignee is StoredMinionIdentity)
            {
                new_assignee = (new_assignee as StoredMinionIdentity).assignableProxy.Get();
            }
            if (new_assignee is MinionAssignablesProxy)
            {
                AssignableSlotInstance slot = new_assignee.GetSoleOwner().GetComponent<Ownables>().GetSlot(base.slot);
                if (slot != null)
                {
                    Assignable assignable = slot.assignable;
                    if (assignable != null)
                    {
                        assignable.Unassign();
                    }
                }
            }
            base.Assign(new_assignee);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            this.UpdateTint();
            this.UpdateStatusString();

            base.OnAssign += this.OnNewAssignment;
            if (this.assignee == null)
            {
                MinionStorage minionstorage = base.GetComponent<MinionStorage>();
                if (minionstorage)
                {
                    List<MinionStorage.Info> storedMinionInfo = minionstorage.GetStoredMinionInfo();
                    if (storedMinionInfo.Count > 0)
                    {
                        Ref<KPrefabID> serializedMinion = storedMinionInfo[0].serializedMinion;
                        if (serializedMinion != null && serializedMinion.GetId() != -1)
                        {
                            StoredMinionIdentity storedminionidentity = serializedMinion.Get().GetComponent<StoredMinionIdentity>();
                            storedminionidentity.ValidateProxy();
                            this.Assign(storedminionidentity);
                        }
                    }
                }
            }
        }

        private void OnNewAssignment(IAssignableIdentity assignables)
        {
            // 更新物品颜色
            this.UpdateTint();

            // 更新状态文本
            this.UpdateStatusString();


        }

        // 控制组件颜色（Tint）显示。
        private void UpdateTint()
        {
            if (this.tintWhenUnassigned)
            {
                //物品外观颜色
                KAnimControllerBase component = base.GetComponent<KAnimControllerBase>();
                if (component != null && component.HasBatchInstanceData)
                {

                    component.TintColour = ((this.assignee == null) ? ownedTint : ownedTint);
                    return;
                }
                // 物品动画颜色
                KBatchedAnimController component2 = base.GetComponent<KBatchedAnimController>();
                if (component2 != null && component2.HasBatchInstanceData)
                {
                    component2.TintColour = ((this.assignee == null) ? ownedTint : ownedTint);
                }
            }
        }


        private void UpdateStatusString()
        {
            var selectable = GetComponent<KSelectable>();
            if (selectable == null)
                return;

            // 有 assignee 就显示 AssignedTo，否则显示 Unassigned
            var statusItem = assignee != null
                ? Db.Get().BuildingStatusItems.AssignedTo
                : Db.Get().BuildingStatusItems.Unassigned;

            selectable.SetStatusItem(Db.Get().StatusItemCategories.Ownable, statusItem, this);
        }


        public List<Descriptor> GetDescriptors(GameObject go)
        {
            List<Descriptor> list = new List<Descriptor>();
            Descriptor descriptor = default(Descriptor);
            descriptor.SetupDescriptor(UI.BUILDINGEFFECTS.ASSIGNEDDUPLICANT, UI.BUILDINGEFFECTS.TOOLTIPS.ASSIGNEDDUPLICANT, Descriptor.DescriptorType.Requirement);
            list.Add(descriptor);
            return list;
        }

        private Chore chore;


        // 配置复制人时是否改变物品颜色
        public bool tintWhenUnassigned = true;

        private Color unownedTint = Color.gray;
        private Color ownedTint = Color.white;

    }
}
