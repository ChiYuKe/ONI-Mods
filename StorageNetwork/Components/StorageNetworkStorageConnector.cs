using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 储存网络连接器组件。
    /// 挂在拥有 Storage 的建筑上，用于把该建筑注册为储存网络中的可连接节点。
    /// 负责向网络系统提供建筑所在格子、输入/输出端口格子、显示名称以及 Storage 引用。
    /// </summary>
    public class StorageNetworkStorageConnector : KMonoBehaviour, IStorageNetworkConnectable
    {
        private Storage storage;

        public int Cell => Grid.PosToCell(this);

        public int InputCell => GetPortCell(true);

        public int OutputCell => GetPortCell(false);

        public string DisplayName => gameObject.GetProperName();

        public Storage Storage => storage;

        public bool AllowsNetworkPull => true;

        public bool CanShareStorage => storage != null && storage.enabled;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            storage = GetComponent<Storage>();
            StorageNetworkRegistry.Register(this);
        }

        protected override void OnCleanUp()
        {
            StorageNetworkRegistry.Unregister(this);
            base.OnCleanUp();
        }

        private int GetPortCell(bool input)
        {
            int cell = Cell;
            return input ? cell : Grid.CellAbove(cell);
        }

        private CellOffset GetPortOffset(int portCell)
        {
            int cell = Cell;
            if (!Grid.IsValidCell(cell) || !Grid.IsValidCell(portCell))
            {
                return CellOffset.none;
            }

            Grid.CellToXY(cell, out int originX, out int originY);
            Grid.CellToXY(portCell, out int portX, out int portY);
            return new CellOffset(portX - originX, portY - originY);
        }
    }
}
