using System;
using System.Text;
using ImGuiNET;
using UnityEngine;
using UnityEngine.UI;

namespace DebugUI
{
    public sealed class DebugUIDevTool : DevTool
    {
        private Vector2 sampleAtScreenPosition = new Vector2(320f, 320f);
        private GameObject selectedObject;
        private bool drawAllHitBoxes = true;
        private bool drawSelectedBox = true;
        private const float TopPanelHeight = 320f;
        private const float HitsPanelHeight = 130f;
        private const float SelectedSummaryHeight = 88f;

        public DebugUIDevTool()
        {
            Name = "DebugUI";
            RequiresGameRunning = false;
        }

        protected override void RenderTo(DevPanel panel)
        {
            Name = "DebugUI";
            ImGui.BeginChild("DebugUI_TopPanel", new Vector2(0f, TopPanelHeight), true);
            DrawTopPanel();
            ImGui.EndChild();

            ImGui.Separator();
            DrawMainLayout();
        }

        private void DrawTopPanel()
        {
            ImGui.Text("UI Sampler / UI 取样器");
            ImGui.SameLine();
            if (ImGui.Button("Copy Info / 复制信息"))
            {
                GUIUtility.systemCopyBuffer = BuildInfo(selectedObject);
            }
            ImGui.SameLine();
            if (ImGui.Button("Clear / 清空"))
            {
                selectedObject = null;
            }

            ImGui.Checkbox("All Hit Boxes / 全部命中框", ref drawAllHitBoxes);
            ImGui.SameLine();
            ImGui.Checkbox("Selected Box / 选中框", ref drawSelectedBox);

            ImGui.Separator();
            DevToolEntity_EyeDrop.ImGuiInput_SampleScreenPosition(ref sampleAtScreenPosition);

            using (ListPool<DevToolEntityTarget, DebugUIDevTool>.PooledList hits = PoolsFor<DebugUIDevTool>.AllocateList<DevToolEntityTarget>())
            {
                Option<string> error = DevToolEntity_EyeDrop.CollectUIGameObjectHitsTo(hits, sampleAtScreenPosition);
                if (error.IsSome())
                {
                    ImGui.Text("[UI Raycast Error / UI 射线错误]");
                    ImGui.SameLine();
                    ImGui.Text(error.Unwrap());
                }

                DrawHitsPanel(hits);
            }
        }

        private void DrawHitsPanel(ListPool<DevToolEntityTarget, DebugUIDevTool>.PooledList hits)
        {
            ImGui.Text("Hits / 命中列表");
            ImGui.BeginChild("DebugUI_HitsPanel", new Vector2(0f, HitsPanelHeight), true);
            DrawHits(hits);
            ImGui.EndChild();
        }

        private void DrawHits(ListPool<DevToolEntityTarget, DebugUIDevTool>.PooledList hits)
        {
            if (hits.Count == 0)
            {
                ImGui.Text("<No UI Hit / 没有命中 UI>");
                return;
            }

            for (int i = 0; i < hits.Count; i++)
            {
                DevToolEntityTarget.ForUIGameObject uiTarget = hits[i] as DevToolEntityTarget.ForUIGameObject;
                if (uiTarget == null || uiTarget.gameObject.IsNullOrDestroyed())
                {
                    continue;
                }

                GameObject go = uiTarget.gameObject;
                string label = string.Format("{0}. {1}##hit_{2}", i + 1, GetObjectName(go), go.GetInstanceID());
                bool picked = ImGui.Selectable(label, selectedObject == go);
                bool hovered = ImGui.IsItemHovered();
                if (picked)
                {
                    selectedObject = go;
                }

                Option<ValueTuple<Vector2, Vector2>> rect = uiTarget.GetScreenRect();
                if (rect.IsSome() && (drawAllHitBoxes || hovered || selectedObject == go))
                {
                    DevToolEntity.DrawBoundingBox(rect.Unwrap(), uiTarget.GetDebugName(), hovered || selectedObject == go);
                }
            }
        }

        private void DrawMainLayout()
        {
            if (selectedObject.IsNullOrDestroyed())
            {
                selectedObject = null;
                ImGui.Text("<Nothing Selected / 未选中>");
                return;
            }

            DrawSelectedBoundingBox();

            ImGui.Text("Selected / 选中对象");
            ImGui.SameLine();
            Transform parent = selectedObject.transform.parent;
            if (parent != null)
            {
                if (ImGui.Button("Up / 上一级"))
                {
                    selectedObject = parent.gameObject;
                }
            }
            else
            {
                ImGui.TextDisabled("<No Parent / 无上级>");
            }
            ImGui.BeginChild("DebugUI_SelectedSummary", new Vector2(0f, SelectedSummaryHeight), true);
            DrawSelectedSummary();
            ImGui.EndChild();

            Vector2 contentSize = ImGui.GetContentRegionAvail();
            float childHeight = Mathf.Max(220f, contentSize.y);
            ImGui.Columns(2, "DebugUI_MainColumns", true);

            ImGui.BeginChild("HierarchyPane / 层级面板", new Vector2(0f, childHeight), true);
            ImGui.Text("Hierarchy / 层级");
            ImGui.Separator();
            DrawHierarchy(selectedObject.transform, selectedObject.transform);
            ImGui.EndChild();

            ImGui.NextColumn();

            ImGui.BeginChild("ComponentsPane / 组件面板", new Vector2(0f, childHeight), true);
            ImGui.Text("Components / 组件");
            ImGui.Separator();
            DrawComponents(selectedObject);
            ImGui.EndChild();

            ImGui.Columns(1);
        }

        private void DrawSelectedBoundingBox()
        {
            DevToolEntityTarget.ForUIGameObject target = new DevToolEntityTarget.ForUIGameObject(selectedObject);
            Option<ValueTuple<Vector2, Vector2>> rect = target.GetScreenRect();
            if (drawSelectedBox && rect.IsSome())
            {
                DevToolEntity.DrawBoundingBox(rect.Unwrap(), target.GetDebugName(), true);
            }
        }

        private void DrawSelectedSummary()
        {
            DrawField("Name / 名称", selectedObject.name);
            DrawField("Path / 路径", GetPath(selectedObject.transform));
            DrawField("Instance ID / 实例 ID", selectedObject.GetInstanceID().ToString());
            DrawRectTransformInfo(selectedObject.GetComponent<RectTransform>());
        }

        private static void DrawComponents(GameObject go)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    ImGui.BulletText("<Missing Component / 缺失组件>");
                    continue;
                }

                Behaviour behaviour = component as Behaviour;
                if (behaviour == null)
                {
                    if (ImGui.Button("Edit / 编辑##component_edit_" + component.GetInstanceID()))
                    {
                        DevToolUtil.Open(new DebugUIComponentEditorTool(component));
                    }
                    ImGui.SameLine();
                    ImGui.BulletText(component.GetType().FullName);
                    continue;
                }

                if (ImGui.Button("Edit / 编辑##component_edit_" + component.GetInstanceID()))
                {
                    DevToolUtil.Open(new DebugUIComponentEditorTool(component));
                }
                ImGui.SameLine();
                bool enabled = behaviour.enabled;
                string label = component.GetType().FullName + "##component_enabled_" + component.GetInstanceID();
                if (ImGui.Checkbox(label, ref enabled) && behaviour.enabled != enabled)
                {
                    behaviour.enabled = enabled;
                    Debug.Log(string.Format("[DebugUI] {0} {1} on {2}.", enabled ? "Enabled" : "Disabled", component.GetType().FullName, GetPath(go.transform)));
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(enabled ? "Enabled / 已启用" : "Disabled / 已禁用");
                    ImGui.EndTooltip();
                }
            }
        }

        private static void DrawRectTransformInfo(RectTransform rectTransform)
        {
            if (rectTransform.IsNullOrDestroyed())
            {
                DrawField("RectTransform", "<Missing / 缺失>");
                return;
            }

            DrawField("Size / 尺寸", FormatVector2(rectTransform.rect.size));
            DrawField("Anchored Position / 锚定位置", FormatVector2(rectTransform.anchoredPosition));
            DrawField("Pivot / 轴心", FormatVector2(rectTransform.pivot));
            DrawField("Anchor Min / 锚点最小值", FormatVector2(rectTransform.anchorMin));
            DrawField("Anchor Max / 锚点最大值", FormatVector2(rectTransform.anchorMax));
            DrawField("Offset Min / 偏移最小值", FormatVector2(rectTransform.offsetMin));
            DrawField("Offset Max / 偏移最大值", FormatVector2(rectTransform.offsetMax));

            LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
            if (!layoutElement.IsNullOrDestroyed())
            {
                DrawField("Layout Preferred / 布局首选尺寸", string.Format("{0:0.##}, {1:0.##}", layoutElement.preferredWidth, layoutElement.preferredHeight));
                DrawField("Layout Flexible / 布局弹性尺寸", string.Format("{0:0.##}, {1:0.##}", layoutElement.flexibleWidth, layoutElement.flexibleHeight));
            }

            HorizontalOrVerticalLayoutGroup layoutGroup = rectTransform.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (!layoutGroup.IsNullOrDestroyed())
            {
                DrawField("Layout Spacing / 布局间距", layoutGroup.spacing.ToString("0.##"));
                DrawField("Child Control / 控制子对象尺寸", string.Format("W:{0} H:{1} / 宽:{0} 高:{1}", layoutGroup.childControlWidth, layoutGroup.childControlHeight));
                DrawField("Child Force Expand / 强制展开子对象", string.Format("W:{0} H:{1} / 宽:{0} 高:{1}", layoutGroup.childForceExpandWidth, layoutGroup.childForceExpandHeight));
            }
        }

        private void DrawHierarchy(Transform current, Transform selected)
        {
            if (current == null)
            {
                return;
            }

            GameObject go = current.gameObject;
            bool activeSelf = go.activeSelf;
            if (ImGui.Checkbox("##hierarchy_active_" + go.GetInstanceID(), ref activeSelf))
            {
                go.SetActive(activeSelf);
                Debug.Log(string.Format("[DebugUI] SetActive({0}) on {1}.", activeSelf, GetPath(current)));
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(activeSelf ? "Active / 可见" : "Inactive / 隐藏");
                ImGui.EndTooltip();
            }
            ImGui.SameLine();

            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;
            if (current == selected)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }
            if (current.childCount == 0)
            {
                flags |= ImGuiTreeNodeFlags.Leaf;
            }

            bool open = ImGui.TreeNodeEx("##hierarchy_tree_" + go.GetInstanceID(), flags, string.Empty);
            ImGui.SameLine();
            if (ImGui.Selectable(current.name + "##hierarchy_select_" + go.GetInstanceID(), current == selected))
            {
                selectedObject = go;
            }
            if (!go.activeInHierarchy && go.activeSelf)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("<Parent Hidden / 父级隐藏>");
            }
            else if (!go.activeSelf)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("<Hidden / 已隐藏>");
            }
            if (open)
            {
                for (int i = 0; i < current.childCount; i++)
                {
                    DrawHierarchy(current.GetChild(i), selected);
                }
                ImGui.TreePop();
            }
        }

        private static void DrawField(string label, string value)
        {
            ImGui.Text(label + ":");
            ImGui.SameLine();
            ImGui.Text(value ?? "<null>");
        }

        private static string BuildInfo(GameObject go)
        {
            if (go.IsNullOrDestroyed())
            {
                return "<Nothing Selected / 未选中对象>";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Name / 名称: " + go.name);
            builder.AppendLine("Path / 路径: " + GetPath(go.transform));
            builder.AppendLine("Instance ID / 实例 ID: " + go.GetInstanceID());

            RectTransform rectTransform = go.GetComponent<RectTransform>();
            if (!rectTransform.IsNullOrDestroyed())
            {
                builder.AppendLine("Size / 尺寸: " + FormatVector2(rectTransform.rect.size));
                builder.AppendLine("Anchored Position / 锚定位置: " + FormatVector2(rectTransform.anchoredPosition));
                builder.AppendLine("Pivot / 轴心: " + FormatVector2(rectTransform.pivot));
                builder.AppendLine("Anchor Min / 锚点最小值: " + FormatVector2(rectTransform.anchorMin));
                builder.AppendLine("Anchor Max / 锚点最大值: " + FormatVector2(rectTransform.anchorMax));
                builder.AppendLine("Offset Min / 偏移最小值: " + FormatVector2(rectTransform.offsetMin));
                builder.AppendLine("Offset Max / 偏移最大值: " + FormatVector2(rectTransform.offsetMax));
            }

            builder.AppendLine("Components / 组件:");
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                builder.AppendLine("- " + (components[i] == null ? "<Missing Component / 缺失组件>" : components[i].GetType().FullName));
            }

            return builder.ToString();
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            StringBuilder builder = new StringBuilder(transform.name);
            Transform parent = transform.parent;
            while (parent != null)
            {
                builder.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return builder.ToString();
        }

        private static string GetObjectName(GameObject go)
        {
            return go.IsNullOrDestroyed() ? "<Null / 空>" : go.name + " [0x" + go.GetInstanceID().ToString("X") + "]";
        }

        private static string FormatVector2(Vector2 value)
        {
            return string.Format("({0:0.##}, {1:0.##})", value.x, value.y);
        }
    }
}
