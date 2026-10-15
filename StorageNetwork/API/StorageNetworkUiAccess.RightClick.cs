using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorageNetwork.API
{
    public static partial class StorageNetworkUiAccess
    {
        private sealed class RightClickHandler
        {
            internal string Id;
            internal Func<bool> IsOpen;
            internal System.Action CloseTop;
            internal int Priority;
        }

        private static readonly List<RightClickHandler> RightClickHandlers = new List<RightClickHandler>();
        private static RightClickHandler pendingRightClick;
        private static Vector3 rightClickStart;
        private static bool rightClickDragged;
        private static int rightClickFrame = -1;
        private static bool rightClickHandledThisFrame, rightClickClosedThisFrame;

        /// <summary>注册附属窗口右键关闭；优先级高的窗口先处理，每次只调用一次关闭回调。</summary>
        public static void RegisterRightClickClose(string id, Func<bool> isOpen, System.Action closeTop, int priority = 100)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A stable provider ID is required.", nameof(id));
            if (isOpen == null) throw new ArgumentNullException(nameof(isOpen));
            if (closeTop == null) throw new ArgumentNullException(nameof(closeTop));
            UnregisterRightClickClose(id);
            RightClickHandlers.Add(new RightClickHandler { Id = id, IsOpen = isOpen, CloseTop = closeTop, Priority = priority });
            RightClickHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public static void UnregisterRightClickClose(string id)
        {
            RightClickHandlers.RemoveAll(handler => handler.Id == id);
            if (pendingRightClick?.Id == id) pendingRightClick = null;
        }

        private static RightClickHandler TopRightClickHandler()
        {
            foreach (var handler in RightClickHandlers)
                if (handler.IsOpen()) return handler;
            return null;
        }

        /// <summary>
        /// 附属窗口 Update 调用。抬起右键且移动不超过8像素时关闭最上层；
        /// 同帧重复调用不会重复关闭。返回该手势是否由附属窗口处理。
        /// </summary>
        public static bool TrackRightClickClose(out bool closed)
        {
            closed = false;
            if (RightClickHandlers.Count == 0) return false;
            return ProcessRightClickClose(Time.frameCount, Input.GetMouseButtonDown(1), Input.GetMouseButtonUp(1),
                Input.GetMouseButton(1), KInputManager.GetMousePos(), out closed);
        }

        internal static bool ProcessRightClickClose(int frame, bool down, bool up, bool held, Vector3 position, out bool closed)
        {
            closed = false;
            if (rightClickFrame == frame)
            {
                closed = rightClickClosedThisFrame;
                return rightClickHandledThisFrame;
            }
            rightClickFrame = frame;
            rightClickHandledThisFrame = rightClickClosedThisFrame = false;
            if (down)
            {
                pendingRightClick = TopRightClickHandler();
                rightClickStart = position;
                rightClickDragged = false;
            }
            if (pendingRightClick == null) return false;
            rightClickHandledThisFrame = true;
            if (!pendingRightClick.IsOpen())
            {
                pendingRightClick = null;
                return true;
            }
            if (!held && !up)
            {
                pendingRightClick = null;
                return true;
            }
            if ((position - rightClickStart).sqrMagnitude > 64f) rightClickDragged = true;
            if (up)
            {
                var handler = pendingRightClick;
                pendingRightClick = null;
                if (!rightClickDragged)
                {
                    handler.CloseTop();
                    rightClickClosedThisFrame = true;
                }
            }
            closed = rightClickClosedThisFrame;
            return true;
        }

        internal static void ResetRightClickGesture()
        {
            pendingRightClick = null;
            rightClickFrame = -1;
            rightClickHandledThisFrame = rightClickClosedThisFrame = false;
        }
    }
}
