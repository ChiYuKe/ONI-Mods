using System;
using System.Text;
using ImGuiNET;
using UnityEngine;

namespace DebugUI
{
    public sealed class DebugUIEntityCaptureTool : DevTool
    {
        private Vector2 sampleAtScreenPosition = new Vector2(360f, 360f);
        private GameObject selectedObject;
        private bool drawAllHitBoxes = true;
        private bool drawSelectedBox = true;
        private bool includeSimCell;
        private KAnimControllerBase animViewerController;
        private const float TopPanelHeight = 330f;
        private const float HitsPanelHeight = 150f;
        private const float SelectedSummaryHeight = 210f;

        public DebugUIEntityCaptureTool()
        {
            Name = "Entity Capture / 实体捕获";
            RequiresGameRunning = false;
        }

        protected override void RenderTo(DevPanel panel)
        {
            Name = "Entity Capture / 实体捕获";
            ImGui.BeginChild("DebugUIEntity_TopPanel", new Vector2(0f, TopPanelHeight), true);
            DrawTopPanel();
            ImGui.EndChild();

            ImGui.Separator();
            DrawMainLayout();
        }

        private void DrawTopPanel()
        {
            ImGui.Text("Entity Sampler / 实体取样器");
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
            ImGui.SameLine();
            ImGui.Checkbox("Sim Cell / 格子", ref includeSimCell);

            ImGui.Separator();
            DevToolEntity_EyeDrop.ImGuiInput_SampleScreenPosition(ref sampleAtScreenPosition);

            using (ListPool<DevToolEntityTarget, DebugUIEntityCaptureTool>.PooledList hits = PoolsFor<DebugUIEntityCaptureTool>.AllocateList<DevToolEntityTarget>())
            {
                Option<string> error = DevToolEntity_EyeDrop.CollectWorldGameObjectHitsTo(hits, sampleAtScreenPosition);
                if (includeSimCell)
                {
                    ValueTuple<Option<DevToolEntityTarget.ForSimCell>, Option<string>> simCell = DevToolEntity_EyeDrop.GetSimCellAt(sampleAtScreenPosition);
                    if (simCell.Item1.IsSome())
                    {
                        hits.Add(simCell.Item1.Unwrap());
                    }
                    if (error.IsNone())
                    {
                        error = simCell.Item2;
                    }
                }

                if (error.IsSome())
                {
                    ImGui.Text("[World Raycast Error / 世界射线错误]");
                    ImGui.SameLine();
                    ImGui.Text(error.Unwrap());
                }

                DrawHitsPanel(hits);
            }
        }

        private void DrawHitsPanel(ListPool<DevToolEntityTarget, DebugUIEntityCaptureTool>.PooledList hits)
        {
            ImGui.Text("Hits / 命中列表");
            ImGui.BeginChild("DebugUIEntity_HitsPanel", new Vector2(0f, HitsPanelHeight), true);
            DrawHits(hits);
            ImGui.EndChild();
        }

        private void DrawHits(ListPool<DevToolEntityTarget, DebugUIEntityCaptureTool>.PooledList hits)
        {
            if (hits.Count == 0)
            {
                ImGui.Text("<No Entity Hit / 没有命中实体>");
                return;
            }

            for (int i = 0; i < hits.Count; i++)
            {
                DevToolEntityTarget target = hits[i];
                DevToolEntityTarget.ForWorldGameObject worldTarget = target as DevToolEntityTarget.ForWorldGameObject;
                GameObject go = worldTarget == null ? null : worldTarget.gameObject;
                bool isSelected = !go.IsNullOrDestroyed() && selectedObject == go;
                string label = string.Format("{0}. {1}##entity_hit_{2}", i + 1, GetTargetLabel(target, go), i);
                bool picked = ImGui.Selectable(label, isSelected);
                bool hovered = ImGui.IsItemHovered();
                if (picked && !go.IsNullOrDestroyed())
                {
                    selectedObject = go;
                }

                Option<ValueTuple<Vector2, Vector2>> rect = target.GetScreenRect();
                if (rect.IsSome() && (drawAllHitBoxes || hovered || isSelected))
                {
                    DevToolEntity.DrawBoundingBox(rect.Unwrap(), target.GetDebugName(), hovered || isSelected);
                }
            }
        }

        private void DrawMainLayout()
        {
            if (selectedObject.IsNullOrDestroyed())
            {
                selectedObject = null;
                animViewerController = null;
                ImGui.Text("<Nothing Selected / 未选中>");
                return;
            }

            if (!animViewerController.IsNullOrDestroyed() && animViewerController.gameObject != selectedObject)
            {
                animViewerController = null;
            }

            DrawSelectedBoundingBox();

            ImGui.Text("Selected Entity / 选中实体");
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

            ImGui.BeginChild("DebugUIEntity_SelectedSummary", new Vector2(0f, SelectedSummaryHeight), true);
            DrawSelectedSummary();
            ImGui.EndChild();

            Vector2 contentSize = ImGui.GetContentRegionAvail();
            float childHeight = Mathf.Max(220f, contentSize.y);
            ImGui.Columns(2, "DebugUIEntity_MainColumns", true);

            ImGui.BeginChild("EntityHierarchyPane / 实体层级面板", new Vector2(0f, childHeight), true);
            ImGui.Text("Hierarchy / 层级");
            ImGui.Separator();
            DrawHierarchy(selectedObject.transform, selectedObject.transform);
            ImGui.EndChild();

            ImGui.NextColumn();

            ImGui.BeginChild("EntityComponentsPane / 实体组件面板", new Vector2(0f, childHeight), true);
            ImGui.Text("Components / 组件");
            ImGui.Separator();
            DrawAnimationTools(selectedObject);
            ImGui.Separator();
            DrawComponents(selectedObject);
            ImGui.EndChild();

            ImGui.Columns(1);

            DrawAnimViewerWindow();
        }

        private void DrawSelectedBoundingBox()
        {
            DevToolEntityTarget.ForWorldGameObject target = new DevToolEntityTarget.ForWorldGameObject(selectedObject);
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
            DrawField("Active / 启用", selectedObject.activeSelf + " / InHierarchy: " + selectedObject.activeInHierarchy);
            DrawTransformInfo(selectedObject.transform);
            DrawAnimationInfo(selectedObject);
        }

        private static void DrawTransformInfo(Transform transform)
        {
            DrawField("Position / 位置", FormatVector3(transform.position));
            DrawField("Local Position / 本地位置", FormatVector3(transform.localPosition));
            DrawField("Rotation / 旋转", FormatVector3(transform.eulerAngles));
            DrawField("Scale / 缩放", FormatVector3(transform.localScale));
            int cell = Grid.PosToCell(transform.position);
            DrawField("Cell / 格子", Grid.IsValidCell(cell) ? cell.ToString() : "<Invalid / 无效>");
        }

        private static void DrawAnimationInfo(GameObject go)
        {
            KAnimControllerBase[] controllers = go.GetComponents<KAnimControllerBase>();
            if (controllers == null || controllers.Length == 0)
            {
                DrawField("Anim / 动画", "<No KAnimController / 无动画控制器>");
                return;
            }

            for (int i = 0; i < controllers.Length; i++)
            {
                KAnimControllerBase controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }

                KAnim.Anim currentAnim = controller.GetCurrentAnim();
                string prefix = controllers.Length == 1 ? "Anim / 动画" : "Anim " + (i + 1) + " / 动画 " + (i + 1);
                DrawField(prefix, currentAnim == null ? "<None / 无>" : currentAnim.name);
                DrawField("Anim File / 动画文件", GetCurrentAnimFileName(controller, currentAnim));
                DrawField("Frame / 帧", FormatFrame(controller, currentAnim));
                DrawField("Mode / 模式", KAnimControllerBase.GetModeString(controller.GetMode()));
                DrawField("Time / 时间", FormatAnimTime(controller, currentAnim));
                DrawField("Files / 文件列表", FormatAnimFiles(controller.AnimFiles));
            }
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

                if (ImGui.Button("Edit / 编辑##entity_component_edit_" + component.GetInstanceID()))
                {
                    DevToolUtil.Open(new DebugUIComponentEditorTool(component));
                }
                ImGui.SameLine();

                Behaviour behaviour = component as Behaviour;
                if (behaviour == null)
                {
                    ImGui.BulletText(component.GetType().FullName);
                    continue;
                }

                bool enabled = behaviour.enabled;
                string label = component.GetType().FullName + "##entity_component_enabled_" + component.GetInstanceID();
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

        private void DrawAnimationTools(GameObject go)
        {
            KAnimControllerBase[] controllers = go.GetComponents<KAnimControllerBase>();
            if (controllers == null || controllers.Length == 0)
            {
                ImGui.TextDisabled("Anim Viewer / 动画查看: <No KAnimController / 无动画控制器>");
                return;
            }

            ImGui.Text("Anim Viewer / 动画查看");
            for (int i = 0; i < controllers.Length; i++)
            {
                KAnimControllerBase controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }

                string currentName = controller.GetCurrentAnim() == null ? "<None / 无>" : controller.GetCurrentAnim().name;
                if (ImGui.Button("View Anims / 查看动画##entity_anim_view_" + controller.GetInstanceID()))
                {
                    animViewerController = controller;
                }
                ImGui.SameLine();
                ImGui.Text(controller.GetType().Name + " | " + currentName);
            }
        }

        private void DrawAnimViewerWindow()
        {
            if (animViewerController.IsNullOrDestroyed())
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(520f, 620f), ImGuiCond.FirstUseEver);
            bool open = true;
            if (!ImGui.Begin("Anim Viewer / 动画查看##DebugUIEntityAnimViewer", ref open))
            {
                ImGui.End();
                if (!open)
                {
                    animViewerController = null;
                }
                return;
            }

            if (!open)
            {
                animViewerController = null;
                ImGui.End();
                return;
            }

            DrawAnimViewerContent(animViewerController);
            ImGui.End();
        }

        private void DrawAnimViewerContent(KAnimControllerBase controller)
        {
            GameObject go = controller.gameObject;
            KAnim.Anim currentAnim = controller.GetCurrentAnim();
            DrawField("Object / 对象", go.IsNullOrDestroyed() ? "<Destroyed / 已销毁>" : GetPath(go.transform));
            DrawField("Controller / 控制器", controller.GetType().FullName);
            DrawField("Current / 当前动画", currentAnim == null ? "<None / 无>" : currentAnim.name);
            DrawField("Frame / 帧", FormatFrame(controller, currentAnim));
            DrawField("Mode / 模式", KAnimControllerBase.GetModeString(controller.GetMode()));
            DrawField("Time / 时间", FormatAnimTime(controller, currentAnim));

            ImGui.Separator();
            if (ImGui.Button("Stop / 停止"))
            {
                controller.Stop();
            }
            ImGui.SameLine();
            if (ImGui.Button("Restart / 重播") && currentAnim != null)
            {
                controller.Play(currentAnim.name, controller.GetMode(), controller.GetPlaySpeed(), 0f);
            }

            ImGui.Separator();
            DrawAnimFileList(controller);
        }

        private void DrawAnimFileList(KAnimControllerBase controller)
        {
            KAnimFile[] files = controller.AnimFiles;
            if (files == null || files.Length == 0)
            {
                ImGui.TextDisabled("<No AnimFiles / 无动画文件>");
                return;
            }

            ImGui.BeginChild("DebugUIEntity_AnimList", new Vector2(0f, 0f), true);
            for (int i = 0; i < files.Length; i++)
            {
                KAnimFile file = files[i];
                if (file == null)
                {
                    ImGui.TextDisabled("<Null AnimFile / 空动画文件>");
                    continue;
                }

                KAnimFileData data = file.GetData();
                string fileName = data == null ? file.name : data.name;
                int animCount = data == null ? 0 : data.animCount;
                if (ImGui.TreeNodeEx(fileName + " (" + animCount + ")##anim_file_" + i, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawAnimFileTextures(file, i);

                    if (data == null)
                    {
                        ImGui.TextDisabled("<No Anim Data / 无动画数据>");
                    }
                    else
                    {
                        for (int j = 0; j < data.animCount; j++)
                        {
                            KAnim.Anim anim = data.GetAnim(j);
                            if (anim == null)
                            {
                                continue;
                            }

                            bool isCurrent = controller.GetCurrentAnim() == anim || (controller.GetCurrentAnim() != null && controller.GetCurrentAnim().name == anim.name);
                            if (ImGui.Selectable(anim.name + FormatAnimMeta(anim) + "##anim_item_" + i + "_" + j, isCurrent))
                            {
                                controller.Play(anim.name, KAnim.PlayMode.Loop, 1f, 0f);
                            }
                            if (ImGui.BeginPopupContextItem("AnimActions / 动画操作##anim_popup_" + i + "_" + j))
                            {
                                if (ImGui.MenuItem("Play Loop / 循环播放"))
                                {
                                    controller.Play(anim.name, KAnim.PlayMode.Loop, 1f, 0f);
                                }
                                if (ImGui.MenuItem("Play Once / 播放一次"))
                                {
                                    controller.Play(anim.name, KAnim.PlayMode.Once, 1f, 0f);
                                }
                                if (ImGui.MenuItem("Copy Name / 复制名称"))
                                {
                                    GUIUtility.systemCopyBuffer = anim.name;
                                }
                                ImGui.EndPopup();
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("Click: Play Loop / 左键: 循环播放");
                                ImGui.Text("Right Click: More / 右键: 更多操作");
                                ImGui.Text("Frames / 帧数: " + anim.numFrames);
                                ImGui.Text("FPS: " + anim.frameRate.ToString("0.##"));
                                ImGui.Text("Time / 时长: " + anim.totalTime.ToString("0.###") + "s");
                                ImGui.EndTooltip();
                            }
                        }
                    }
                    ImGui.TreePop();
                }
            }
            ImGui.EndChild();
        }

        private static void DrawAnimFileTextures(KAnimFile file, int fileIndex)
        {
            if (file == null)
            {
                return;
            }

            if (file.textureList == null || file.textureList.Count == 0)
            {
                ImGui.TextDisabled("Textures / 纹理: <None / 无>");
                return;
            }

            if (!ImGui.TreeNodeEx("Textures / 纹理 (" + file.textureList.Count + ")##anim_textures_" + fileIndex, ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            for (int i = 0; i < file.textureList.Count; i++)
            {
                Texture2D texture = file.textureList[i];
                if (texture == null)
                {
                    ImGui.BulletText("<Null Texture / 空纹理>");
                    continue;
                }

                if (ImGui.Button("View / 查看##anim_texture_view_" + fileIndex + "_" + i))
                {
                    DevToolUtil.Open(DebugUITextureViewerTool.ForTexture(texture));
                }
                ImGui.SameLine();
                if (ImGui.Button("Export / 导出##anim_texture_export_" + fileIndex + "_" + i))
                {
                    string path = DebugUIComponentEditorTool.ExportTextureRegion(texture, new Rect(0f, 0f, texture.width, texture.height), "kanim_texture_" + texture.name);
                    Debug.Log("[DebugUI] Exported KAnim texture: " + path);
                }
                ImGui.SameLine();
                ImGui.Text(texture.name + "  " + texture.width + "x" + texture.height);
            }

            ImGui.TreePop();
        }

        private void DrawHierarchy(Transform current, Transform selected)
        {
            if (current == null)
            {
                return;
            }

            GameObject go = current.gameObject;
            bool activeSelf = go.activeSelf;
            if (ImGui.Checkbox("##entity_hierarchy_active_" + go.GetInstanceID(), ref activeSelf))
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

            bool open = ImGui.TreeNodeEx("##entity_hierarchy_tree_" + go.GetInstanceID(), flags, string.Empty);
            ImGui.SameLine();
            if (ImGui.Selectable(current.name + "##entity_hierarchy_select_" + go.GetInstanceID(), current == selected))
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
            builder.AppendLine("Active / 启用: " + go.activeSelf + " / InHierarchy: " + go.activeInHierarchy);
            builder.AppendLine("Position / 位置: " + FormatVector3(go.transform.position));
            builder.AppendLine("Local Position / 本地位置: " + FormatVector3(go.transform.localPosition));
            builder.AppendLine("Rotation / 旋转: " + FormatVector3(go.transform.eulerAngles));
            builder.AppendLine("Scale / 缩放: " + FormatVector3(go.transform.localScale));

            int cell = Grid.PosToCell(go.transform.position);
            builder.AppendLine("Cell / 格子: " + (Grid.IsValidCell(cell) ? cell.ToString() : "<Invalid / 无效>"));
            AppendAnimationInfo(builder, go);

            builder.AppendLine("Components / 组件:");
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                builder.AppendLine("- " + (components[i] == null ? "<Missing Component / 缺失组件>" : components[i].GetType().FullName));
            }

            return builder.ToString();
        }

        private static void AppendAnimationInfo(StringBuilder builder, GameObject go)
        {
            KAnimControllerBase[] controllers = go.GetComponents<KAnimControllerBase>();
            builder.AppendLine("Animation / 动画:");
            if (controllers == null || controllers.Length == 0)
            {
                builder.AppendLine("- <No KAnimController / 无动画控制器>");
                return;
            }

            for (int i = 0; i < controllers.Length; i++)
            {
                KAnimControllerBase controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }

                KAnim.Anim currentAnim = controller.GetCurrentAnim();
                builder.AppendLine("- Controller / 控制器: " + controller.GetType().FullName);
                builder.AppendLine("  Current / 当前: " + (currentAnim == null ? "<None / 无>" : currentAnim.name));
                builder.AppendLine("  Anim File / 动画文件: " + GetCurrentAnimFileName(controller, currentAnim));
                builder.AppendLine("  Frame / 帧: " + FormatFrame(controller, currentAnim));
                builder.AppendLine("  Mode / 模式: " + KAnimControllerBase.GetModeString(controller.GetMode()));
                builder.AppendLine("  Time / 时间: " + FormatAnimTime(controller, currentAnim));
                builder.AppendLine("  Files / 文件列表: " + FormatAnimFiles(controller.AnimFiles));
            }
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

        private static string GetTargetLabel(DevToolEntityTarget target, GameObject go)
        {
            if (!go.IsNullOrDestroyed())
            {
                return go.name + " [0x" + go.GetInstanceID().ToString("X") + "]";
            }

            return target == null ? "<Null / 空>" : target.GetDebugName();
        }

        private static string FormatVector3(Vector3 value)
        {
            return string.Format("({0:0.##}, {1:0.##}, {2:0.##})", value.x, value.y, value.z);
        }

        private static string GetCurrentAnimFileName(KAnimControllerBase controller, KAnim.Anim currentAnim)
        {
            if (currentAnim != null && currentAnim.animFile != null)
            {
                return currentAnim.animFile.name;
            }

            KAnimFile[] files = controller.AnimFiles;
            if (files != null && files.Length > 0 && files[0] != null)
            {
                return files[0].name;
            }

            return "<None / 无>";
        }

        private static string FormatFrame(KAnimControllerBase controller, KAnim.Anim currentAnim)
        {
            if (currentAnim == null)
            {
                return controller.currentFrame.ToString();
            }

            return string.Format("{0}/{1}", controller.currentFrame, currentAnim.numFrames);
        }

        private static string FormatAnimTime(KAnimControllerBase controller, KAnim.Anim currentAnim)
        {
            if (currentAnim == null)
            {
                return controller.GetElapsedTime().ToString("0.###") + "s";
            }

            return string.Format("{0:0.###}s / {1:0.###}s", controller.GetElapsedTime(), currentAnim.totalTime);
        }

        private static string FormatAnimFiles(KAnimFile[] files)
        {
            if (files == null || files.Length == 0)
            {
                return "<None / 无>";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < files.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(files[i] == null ? "<null>" : files[i].name);
            }

            return builder.ToString();
        }

        private static string FormatAnimMeta(KAnim.Anim anim)
        {
            return string.Format("  [{0}f, {1:0.##}fps, {2:0.###}s]", anim.numFrames, anim.frameRate, anim.totalTime);
        }
    }
}
