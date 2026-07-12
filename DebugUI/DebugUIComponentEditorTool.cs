using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImGuiNET;
using Klei.AI;
using UnityEngine;
using UnityEngine.UI;

namespace DebugUI
{
    public sealed class DebugUIComponentEditorTool : DevTool
    {
        private static readonly Dictionary<int, IntPtr> TextureBindings = new Dictionary<int, IntPtr>();
        private static readonly Dictionary<int, int> SpritePreviewModes = new Dictionary<int, int>();

        private readonly Component component;
        private readonly int componentInstanceId;
        private string filter = string.Empty;
        private bool showReadonly;
        private bool showFields = true;
        private bool showProperties = true;
        private bool includeNonPublic;
        private bool showNested = true;

        private const int MaxNestedDepth = 5;
        private const int MaxCollectionItems = 256;

        public DebugUIComponentEditorTool(Component component)
        {
            this.component = component;
            componentInstanceId = component == null ? 0 : component.GetInstanceID();
            Name = component == null ? "Component Editor" : "Edit: " + component.GetType().Name;
            RequiresGameRunning = false;
        }

        protected override void RenderTo(DevPanel panel)
        {
            if (component.IsNullOrDestroyed())
            {
                ImGui.Text("<Component destroyed / 组件已销毁>");
                return;
            }

            Type type = component.GetType();
            Name = "Edit: " + type.Name;
            ImGui.Text(type.FullName);
            ImGui.Text("GameObject: " + component.gameObject.name);
            ImGui.Separator();

            Behaviour behaviour = component as Behaviour;
            if (behaviour != null)
            {
                bool enabled = behaviour.enabled;
                if (ImGui.Checkbox("Enabled / 启用", ref enabled))
                {
                    behaviour.enabled = enabled;
                }
            }

            ImGui.InputText("Filter / 过滤", ref filter, 128);
            ImGui.Checkbox("Fields / 字段", ref showFields);
            ImGui.SameLine();
            ImGui.Checkbox("Properties / 属性", ref showProperties);
            ImGui.SameLine();
            ImGui.Checkbox("Readonly / 只读", ref showReadonly);
            ImGui.SameLine();
            ImGui.Checkbox("NonPublic / 非公开", ref includeNonPublic);
            ImGui.SameLine();
            ImGui.Checkbox("Nested / 嵌套", ref showNested);
            ImGui.Separator();

            DrawDuplicantEditors();
            DrawUnityUiResourceInfo();

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            if (showFields && ImGui.CollapsingHeader("Fields / 字段", ImGuiTreeNodeFlags.DefaultOpen))
            {
                FieldInfo[] fields = type.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    DrawField(fields[i]);
                }
            }

            if (showProperties && ImGui.CollapsingHeader("Properties / 属性", ImGuiTreeNodeFlags.DefaultOpen))
            {
                PropertyInfo[] properties = type.GetProperties(flags);
                for (int i = 0; i < properties.Length; i++)
                {
                    DrawProperty(properties[i]);
                }
            }
        }

        private void DrawDuplicantEditors()
        {
            AttributeLevels attributeLevels = component as AttributeLevels;
            if (attributeLevels == null)
            {
                return;
            }

            if (!ImGui.CollapsingHeader("Duplicant Attributes / 复制人属性", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            ImGui.Text("Changes are applied through AttributeLevels, so derived bonuses refresh immediately. / 修改会立即刷新派生加成。");
            foreach (AttributeLevel attributeLevel in attributeLevels)
            {
                if (attributeLevel == null || attributeLevel.attribute == null || attributeLevel.attribute.Attribute == null)
                {
                    continue;
                }

                string attributeId = attributeLevel.attribute.Attribute.Id;
                if (!ShouldShow(attributeId))
                {
                    continue;
                }

                ImGui.PushID("duplicant_attribute_" + attributeId);
                int level = attributeLevel.GetLevel();
                ImGui.PushItemWidth(150f);
                if (ImGui.InputInt(attributeId + " Level / 等级", ref level))
                {
                    attributeLevels.SetLevel(attributeId, Mathf.Max(0, level));
                }
                ImGui.PopItemWidth();

                float experience = attributeLevel.experience;
                ImGui.PushItemWidth(150f);
                if (ImGui.InputFloat("Experience / 经验", ref experience))
                {
                    attributeLevels.SetExperience(attributeId, Mathf.Max(0f, experience));
                }
                ImGui.PopItemWidth();

                ImGui.SameLine();
                ImGui.Text(string.Format("Next: {0:0.##}  Progress: {1:P1}", attributeLevel.GetExperienceForNextLevel(), attributeLevel.GetPercentComplete()));
                ImGui.Separator();
                ImGui.PopID();
            }
        }

        private void DrawUnityUiResourceInfo()
        {
            Graphic graphic = component as Graphic;
            if (graphic == null)
            {
                return;
            }

            if (!ImGui.CollapsingHeader("UI Resources / UI 资源", ImGuiTreeNodeFlags.DefaultOpen))
            {
                return;
            }

            Image image = component as Image;
            if (image != null)
            {
                DrawSpriteInfo("Sprite / 精灵", image.sprite);
                return;
            }

            DrawUnityObject("Material / 材质", graphic.material);
            DrawTextureInfo("Graphic Main Texture / Graphic 主纹理", graphic.mainTexture);
        }

        private static void DrawSpriteInfo(string label, Sprite sprite)
        {
            if (sprite == null)
            {
                DrawReadOnlyStatic(label, "Sprite", "<null>");
                return;
            }

            if (ImGui.TreeNode(label + "##sprite_" + sprite.GetInstanceID()))
            {
                DrawSpritePreview(sprite);
                int previewMode = GetSpritePreviewMode(sprite);
                if (ImGui.Button(GetSpritePreviewModeLabel(previewMode) + "##rotate_sprite_" + sprite.GetInstanceID()))
                {
                    SetSpritePreviewMode(sprite, (previewMode + 1) % 3);
                }
                if (ImGui.Button("Open Viewer / 打开查看器##view_sprite_" + sprite.GetInstanceID()))
                {
                    DevToolUtil.Open(DebugUITextureViewerTool.ForSprite(sprite));
                }
                ImGui.SameLine();
                if (ImGui.Button("Export Sprite PNG / 导出精灵 PNG##export_sprite_" + sprite.GetInstanceID()))
                {
                    ExportSprite(sprite);
                }
                DrawReadOnlyStatic("Name / 名称", "string", sprite.name);
                DrawReadOnlyStatic("Instance ID / 实例 ID", "int", sprite.GetInstanceID().ToString());
                DrawReadOnlyStatic("Texture / 纹理", "Texture2D", sprite.texture == null ? "<null>" : sprite.texture.name);
                DrawReadOnlyStatic("Rect / 矩形", "Rect", FormatRect(sprite.rect));
                DrawReadOnlyStatic("Texture Rect / 纹理矩形", "Rect", FormatRect(sprite.textureRect));
                DrawReadOnlyStatic("Pivot / 轴心", "Vector2", FormatVector2(sprite.pivot));
                DrawReadOnlyStatic("Border / 九宫格边框", "Vector4", FormatVector4(sprite.border));
                DrawReadOnlyStatic("Pixels Per Unit / 每单位像素", "float", sprite.pixelsPerUnit.ToString("0.###"));
                ImGui.TreePop();
            }
        }

        private static void DrawTextureInfo(string label, Texture texture)
        {
            if (texture == null)
            {
                DrawReadOnlyStatic(label, "Texture", "<null>");
                return;
            }

            if (ImGui.TreeNode(label + "##texture_" + texture.GetInstanceID()))
            {
                DrawTexturePreview(texture);
                Texture2D texture2D = texture as Texture2D;
                if (texture2D != null && ImGui.Button("Open Viewer / 打开查看器##view_texture_" + texture.GetInstanceID()))
                {
                    DevToolUtil.Open(DebugUITextureViewerTool.ForTexture(texture2D));
                }
                if (texture2D != null)
                {
                    ImGui.SameLine();
                }
                if (texture2D != null && ImGui.Button("Export Texture PNG / 导出纹理 PNG##export_texture_" + texture.GetInstanceID()))
                {
                    ExportTexture(texture2D);
                }
                DrawReadOnlyStatic("Name / 名称", texture.GetType().Name, texture.name);
                DrawReadOnlyStatic("Instance ID / 实例 ID", "int", texture.GetInstanceID().ToString());
                DrawReadOnlyStatic("Size / 尺寸", "int", texture.width + " x " + texture.height);
                DrawReadOnlyStatic("Filter Mode / 过滤模式", "FilterMode", texture.filterMode.ToString());
                DrawReadOnlyStatic("Wrap Mode / 包裹模式", "TextureWrapMode", texture.wrapMode.ToString());
                ImGui.TreePop();
            }
        }

        private static void DrawUnityObject(string label, UnityEngine.Object obj)
        {
            if (obj == null)
            {
                DrawReadOnlyStatic(label, "UnityObject", "<null>");
                return;
            }

            DrawReadOnlyStatic(label, obj.GetType().Name, obj.name + " [0x" + obj.GetInstanceID().ToString("X") + "]");
        }

        private static void DrawSpritePreview(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Texture2D texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            Vector2 size = FitPreviewSize(textureRect.width, textureRect.height);
            Vector2 uv0;
            Vector2 uv1;
            GetSpriteUv(texture, textureRect, GetSpritePreviewMode(sprite), out uv0, out uv1);
            DrawTextureImage(texture, size, uv0, uv1);
        }

        private static void DrawTexturePreview(Texture texture)
        {
            Texture2D texture2D = texture as Texture2D;
            if (texture2D == null)
            {
                DrawReadOnlyStatic("Preview / 预览", texture.GetType().Name, "Only Texture2D preview is supported / 只支持 Texture2D 预览");
                return;
            }

            Vector2 size = FitPreviewSize(texture2D.width, texture2D.height);
            DrawTextureImage(texture2D, size, Vector2.zero, Vector2.one);
        }

        private static void DrawTextureImage(Texture2D texture, Vector2 size, Vector2 uv0, Vector2 uv1)
        {
            IntPtr textureId = GetTextureId(texture);
            if (textureId == IntPtr.Zero)
            {
                DrawReadOnlyStatic("Preview / 预览", "Texture2D", "<bind failed>");
                return;
            }

            ImGui.Image(textureId, size, uv0, uv1);
        }

        private static void ExportSprite(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            string path = ExportSpriteTexture(sprite);
            Debug.Log("[DebugUI] Exported sprite texture: " + path);
        }

        private static void ExportTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            string path = ExportTextureRegion(texture, new Rect(0f, 0f, texture.width, texture.height), "texture_" + texture.name);
            Debug.Log("[DebugUI] Exported texture: " + path);
        }

        internal static string ExportTextureRegion(Texture2D source, Rect sourceRect, string namePrefix)
        {
            return ExportTextureRegion(source, sourceRect, namePrefix, false);
        }

        internal static string ExportTextureRegion(Texture2D source, Rect sourceRect, string namePrefix, bool rotate180)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(sourceRect.x), 0, source.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(sourceRect.y), 0, source.height - 1);
            int width = Mathf.Clamp(Mathf.RoundToInt(sourceRect.width), 1, source.width - x);
            int height = Mathf.Clamp(Mathf.RoundToInt(sourceRect.height), 1, source.height - y);

            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;
                output.ReadPixels(new Rect(x, y, width, height), 0, 0);
                output.Apply();
                if (rotate180)
                {
                    RotateTexture180(output);
                }

                string folder = GetExportFolder();
                Directory.CreateDirectory(folder);
                string fileName = SanitizeFileName(namePrefix) + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png";
                string path = Path.Combine(folder, fileName);
                File.WriteAllBytes(path, output.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.Destroy(output);
            }
        }

        private static void RotateTexture180(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            Color32[] rotated = new Color32[pixels.Length];
            int last = pixels.Length - 1;
            for (int i = 0; i < pixels.Length; i++)
            {
                rotated[last - i] = pixels[i];
            }

            texture.SetPixels32(rotated);
            texture.Apply();
        }

        internal static string ExportSpriteTexture(Sprite sprite)
        {
            return ExportTextureRegion(sprite.texture, sprite.textureRect, "sprite_" + sprite.name);
        }

        internal static int GetSpritePreviewMode(Sprite sprite)
        {
            if (sprite == null)
            {
                return 2;
            }

            int mode;
            return SpritePreviewModes.TryGetValue(sprite.GetInstanceID(), out mode) ? mode : 2;
        }

        internal static void SetSpritePreviewMode(Sprite sprite, int mode)
        {
            if (sprite != null)
            {
                SpritePreviewModes[sprite.GetInstanceID()] = mode;
            }
        }

        internal static string GetSpritePreviewModeLabel(int mode)
        {
            if (mode == 1)
            {
                return "Preview: 180 / 预览: 180";
            }
            if (mode == 2)
            {
                return "Preview: 180 + Mirror / 预览: 180 + 镜像";
            }

            return "Preview: Normal / 预览: 原始";
        }

        internal static void GetSpriteUv(Texture2D texture, Rect textureRect, int mode, out Vector2 uv0, out Vector2 uv1)
        {
            if (mode == 1)
            {
                // Rotate 180.
                uv0 = new Vector2(textureRect.xMax / texture.width, textureRect.yMax / texture.height);
                uv1 = new Vector2(textureRect.xMin / texture.width, textureRect.yMin / texture.height);
                return;
            }
            if (mode == 2)
            {
                // Rotate 180, then mirror horizontally for preview only.
                uv0 = new Vector2(textureRect.xMin / texture.width, textureRect.yMax / texture.height);
                uv1 = new Vector2(textureRect.xMax / texture.width, textureRect.yMin / texture.height);
                return;
            }

            uv0 = new Vector2(textureRect.xMin / texture.width, textureRect.yMin / texture.height);
            uv1 = new Vector2(textureRect.xMax / texture.width, textureRect.yMax / texture.height);
        }

        private static string GetExportFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Klei",
                "OxygenNotIncluded",
                "mods",
                "Dev",
                "DebugUI",
                "ExportedTextures");
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "texture";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                value = value.Replace(invalid[i], '_');
            }

            return value;
        }

        internal static IntPtr GetTextureId(Texture2D texture)
        {
            if (texture == null)
            {
                return IntPtr.Zero;
            }

            int id = texture.GetInstanceID();
            if (TextureBindings.TryGetValue(id, out IntPtr textureId))
            {
                return textureId;
            }

            ImGuiRenderer renderer = ImGuiRenderer.GetInstance();
            if (renderer == null)
            {
                return IntPtr.Zero;
            }

            textureId = renderer.BindTexture(texture, false);
            TextureBindings[id] = textureId;
            return textureId;
        }

        private static Vector2 FitPreviewSize(float width, float height)
        {
            if (width <= 0f || height <= 0f)
            {
                return new Vector2(64f, 64f);
            }

            const float maxSide = 192f;
            float scale = Mathf.Min(maxSide / width, maxSide / height);
            scale = Mathf.Min(scale, 1f);
            return new Vector2(Mathf.Max(8f, width * scale), Mathf.Max(8f, height * scale));
        }

        private void DrawField(FieldInfo field)
        {
            if (!ShouldShow(field.Name))
            {
                return;
            }

            object value;
            try
            {
                value = field.GetValue(component);
            }
            catch (Exception e)
            {
                DrawReadOnly(field.Name, field.FieldType, "<get failed: " + e.GetType().Name + ">");
                return;
            }

            bool canWrite = !field.IsInitOnly && !field.IsLiteral;
            DrawMember(field.Name, field.FieldType, value, canWrite, newValue => field.SetValue(component, newValue));
        }

        private void DrawProperty(PropertyInfo property)
        {
            if (!ShouldShow(property.Name) || property.GetIndexParameters().Length != 0)
            {
                return;
            }

            MethodInfo getMethod = property.GetGetMethod(includeNonPublic);
            if (getMethod == null)
            {
                return;
            }

            object value;
            try
            {
                value = property.GetValue(component, null);
            }
            catch (Exception e)
            {
                DrawReadOnly(property.Name, property.PropertyType, "<get failed: " + e.GetType().Name + ">");
                return;
            }

            bool canWrite = property.GetSetMethod(includeNonPublic) != null;
            DrawMember(property.Name, property.PropertyType, value, canWrite, newValue => property.SetValue(component, newValue, null));
        }

        private void DrawMember(string name, Type type, object value, bool canWrite, Action<object> setValue)
        {
            string label = name + "##" + componentInstanceId + "_" + name;
            if (canWrite && TryDrawEditable(label, type, value, setValue))
            {
                return;
            }

            if (showNested && value != null && CanExpand(type))
            {
                DrawNested(name, type, value, 0, componentInstanceId + "_" + name);
                return;
            }

            if (showReadonly)
            {
                DrawReadOnly(name, type, FormatValue(value));
            }
        }

        private void DrawNested(string name, Type declaredType, object value, int depth, string path)
        {
            Type runtimeType = value == null ? declaredType : value.GetType();
            string summary = name + " (" + GetFriendlyTypeName(runtimeType) + ")";
            if (!ImGui.TreeNode(summary + "##nested_" + path))
            {
                return;
            }

            try
            {
                if (depth >= MaxNestedDepth)
                {
                    ImGui.Text("<maximum nested depth / 已达到最大嵌套深度>");
                    return;
                }

                IDictionary dictionary = value as IDictionary;
                if (dictionary != null)
                {
                    DrawDictionary(dictionary, depth + 1, path);
                    return;
                }

                IList list = value as IList;
                if (list != null)
                {
                    DrawList(list, runtimeType, depth + 1, path);
                    return;
                }

                DrawObjectMembers(value, runtimeType, depth + 1, path);
            }
            catch (Exception e)
            {
                ImGui.Text("<inspect failed: " + e.GetType().Name + ": " + e.Message + ">");
            }
            finally
            {
                ImGui.TreePop();
            }
        }

        private void DrawList(IList list, Type listType, int depth, string path)
        {
            ImGui.Text("Count / 数量: " + list.Count);
            int count = Math.Min(list.Count, MaxCollectionItems);
            Type elementType = listType.IsArray ? listType.GetElementType() : GetCollectionElementType(listType);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                object item;
                try { item = list[index]; }
                catch (Exception e) { ImGui.Text("[" + index + "]: <get failed: " + e.GetType().Name + ">"); continue; }
                Type itemType = elementType ?? (item == null ? typeof(object) : item.GetType());
                DrawNestedValue("[" + index + "]", itemType, item, !list.IsReadOnly,
                    newValue => list[index] = newValue, depth, path + "_" + index);
            }
            if (list.Count > count)
            {
                ImGui.Text("... " + (list.Count - count) + " more items / 其余项目未显示");
            }
        }

        private void DrawDictionary(IDictionary dictionary, int depth, string path)
        {
            ImGui.Text("Count / 数量: " + dictionary.Count);
            int index = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (index >= MaxCollectionItems) break;
                object key = entry.Key;
                object item = entry.Value;
                Type itemType = item == null ? typeof(object) : item.GetType();
                string keyText = FormatValue(key);
                DrawNestedValue("[" + keyText + "]", itemType, item, !dictionary.IsReadOnly,
                    newValue => dictionary[key] = newValue, depth, path + "_" + index);
                index++;
            }
            if (dictionary.Count > index)
            {
                ImGui.Text("... " + (dictionary.Count - index) + " more items / 其余项目未显示");
            }
        }

        private void DrawObjectMembers(object owner, Type ownerType, int depth, string path)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic) flags |= BindingFlags.NonPublic;

            if (showFields)
            {
                FieldInfo[] fields = ownerType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (!ShouldShow(field.Name) && !CanExpand(field.FieldType)) continue;
                    object fieldValue;
                    try { fieldValue = field.GetValue(owner); }
                    catch (Exception e) { if (showReadonly) DrawReadOnly(field.Name, field.FieldType, "<get failed: " + e.GetType().Name + ">"); continue; }
                    DrawNestedValue(field.Name, field.FieldType, fieldValue, !field.IsInitOnly && !field.IsLiteral,
                        newValue => field.SetValue(owner, newValue), depth, path + "_f_" + field.Name);
                }
            }

            if (showProperties)
            {
                PropertyInfo[] properties = ownerType.GetProperties(flags);
                for (int i = 0; i < properties.Length; i++)
                {
                    PropertyInfo property = properties[i];
                    if (property.GetIndexParameters().Length != 0 || (!ShouldShow(property.Name) && !CanExpand(property.PropertyType))) continue;
                    MethodInfo getter = property.GetGetMethod(includeNonPublic);
                    if (getter == null) continue;
                    object propertyValue;
                    try { propertyValue = property.GetValue(owner, null); }
                    catch (Exception e) { if (showReadonly) DrawReadOnly(property.Name, property.PropertyType, "<get failed: " + e.GetType().Name + ">"); continue; }
                    bool writable = property.GetSetMethod(includeNonPublic) != null;
                    DrawNestedValue(property.Name, property.PropertyType, propertyValue, writable,
                        newValue => property.SetValue(owner, newValue, null), depth, path + "_p_" + property.Name);
                }
            }
        }

        private void DrawNestedValue(string name, Type type, object value, bool canWrite, Action<object> setValue, int depth, string path)
        {
            string label = name + "##" + componentInstanceId + "_" + path;
            if (canWrite && TryDrawEditable(label, type, value, setValue)) return;
            if (value != null && CanExpand(type))
            {
                DrawNested(name, type, value, depth, path);
            }
            else if (showReadonly)
            {
                DrawReadOnly(name, type, FormatValue(value));
            }
        }

        private static bool CanExpand(Type type)
        {
            if (type == null || type == typeof(string) || type.IsPrimitive || type.IsEnum || type.IsPointer || typeof(Delegate).IsAssignableFrom(type)) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;
            return type != typeof(decimal) && type != typeof(DateTime) && type != typeof(TimeSpan);
        }

        private static Type GetCollectionElementType(Type type)
        {
            if (type.IsGenericType && type.GetGenericArguments().Length == 1) return type.GetGenericArguments()[0];
            Type[] interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == typeof(IList<>)) return interfaces[i].GetGenericArguments()[0];
            }
            return null;
        }

        private bool TryDrawEditable(string label, Type type, object value, Action<object> setValue)
        {
            try
            {
                if (type == typeof(bool))
                {
                    bool current = value is bool b && b;
                    if (ImGui.Checkbox(label, ref current))
                    {
                        setValue(current);
                    }
                    return true;
                }

                if (type == typeof(int))
                {
                    int current = value is int i ? i : 0;
                    ImGui.PushItemWidth(180f);
                    bool changed = ImGui.DragInt(label, ref current, 1f);
                    ImGui.PopItemWidth();
                    ImGui.SameLine();
                    ImGui.PushItemWidth(88f);
                    changed |= ImGui.InputInt("##input_" + label, ref current);
                    ImGui.PopItemWidth();
                    if (changed)
                    {
                        setValue(current);
                    }
                    return true;
                }

                if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort))
                {
                    int current = value == null ? 0 : Convert.ToInt32(value);
                    ImGui.PushItemWidth(180f);
                    bool changed = ImGui.DragInt(label, ref current, 1f);
                    ImGui.PopItemWidth();
                    if (changed)
                    {
                        if (type == typeof(byte)) setValue((byte)Mathf.Clamp(current, byte.MinValue, byte.MaxValue));
                        else if (type == typeof(sbyte)) setValue((sbyte)Mathf.Clamp(current, sbyte.MinValue, sbyte.MaxValue));
                        else if (type == typeof(short)) setValue((short)Mathf.Clamp(current, short.MinValue, short.MaxValue));
                        else setValue((ushort)Mathf.Clamp(current, ushort.MinValue, ushort.MaxValue));
                    }
                    return true;
                }

                if (type == typeof(float))
                {
                    float current = value is float f ? f : 0f;
                    ImGui.PushItemWidth(180f);
                    bool changed = current >= 0f && current <= 1f
                        ? ImGui.SliderFloat(label, ref current, 0f, 1f)
                        : ImGui.DragFloat(label, ref current, 0.01f);
                    ImGui.PopItemWidth();
                    ImGui.SameLine();
                    ImGui.PushItemWidth(88f);
                    changed |= ImGui.InputFloat("##input_" + label, ref current);
                    ImGui.PopItemWidth();
                    if (changed)
                    {
                        setValue(current);
                    }
                    return true;
                }

                if (type == typeof(string))
                {
                    string current = value as string ?? string.Empty;
                    if (ImGui.InputText(label, ref current, 1024))
                    {
                        setValue(current);
                    }
                    return true;
                }

                if (type == typeof(Vector2))
                {
                    Vector2 current = value is Vector2 v ? v : Vector2.zero;
                    ImGui.PushItemWidth(260f);
                    bool changed = ImGui.DragFloat2(label, ref current, 0.01f);
                    ImGui.PopItemWidth();
                    ImGui.SameLine();
                    ImGui.PushItemWidth(160f);
                    changed |= ImGui.InputFloat2("##input_" + label, ref current);
                    ImGui.PopItemWidth();
                    if (changed)
                    {
                        setValue(current);
                    }
                    return true;
                }

                if (type == typeof(Vector3))
                {
                    Vector3 current = value is Vector3 v ? v : Vector3.zero;
                    ImGui.PushItemWidth(300f);
                    bool changed = ImGui.DragFloat3(label, ref current, 0.01f);
                    ImGui.PopItemWidth();
                    ImGui.SameLine();
                    ImGui.PushItemWidth(210f);
                    changed |= ImGui.InputFloat3("##input_" + label, ref current);
                    ImGui.PopItemWidth();
                    if (changed)
                    {
                        setValue(current);
                    }
                    return true;
                }

                if (type == typeof(Vector4))
                {
                    Vector4 current = value is Vector4 v ? v : Vector4.zero;
                    ImGui.PushItemWidth(340f);
                    bool changed = ImGui.DragFloat4(label, ref current, 0.01f);
                    ImGui.PopItemWidth();
                    ImGui.SameLine();
                    ImGui.PushItemWidth(260f);
                    changed |= ImGui.InputFloat4("##input_" + label, ref current);
                    ImGui.PopItemWidth();
                    if (changed)
                    {
                        setValue(current);
                    }
                    return true;
                }

                if (type == typeof(Color))
                {
                    Color color = value is Color c ? c : Color.white;
                    Vector4 current = new Vector4(color.r, color.g, color.b, color.a);
                    ImGui.PushItemWidth(320f);
                    bool changed = ImGui.ColorEdit4(label, ref current);
                    ImGui.PopItemWidth();
                    if (changed)
                    {
                        setValue(new Color(current.x, current.y, current.z, current.w));
                    }
                    return true;
                }

                if (type.IsEnum)
                {
                    string currentName = value == null ? "<null>" : value.ToString();
                    if (ImGui.BeginCombo(label, currentName))
                    {
                        Array values = Enum.GetValues(type);
                        for (int i = 0; i < values.Length; i++)
                        {
                            object enumValue = values.GetValue(i);
                            string enumName = enumValue.ToString();
                            bool selected = enumName == currentName;
                            if (ImGui.Selectable(enumName, selected))
                            {
                                setValue(enumValue);
                            }
                        }
                        ImGui.EndCombo();
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                ImGui.Text(label + ": <set failed: " + e.GetType().Name + ">");
                return true;
            }

            return false;
        }

        private void DrawReadOnly(string name, Type type, string value)
        {
            ImGui.Text(name + " (" + GetFriendlyTypeName(type) + "): " + value);
        }

        private static void DrawReadOnlyStatic(string name, string typeName, string value)
        {
            ImGui.Text(name + " (" + typeName + "): " + value);
        }

        private bool ShouldShow(string name)
        {
            return string.IsNullOrEmpty(filter) || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            UnityEngine.Object unityObject = value as UnityEngine.Object;
            if (unityObject != null)
            {
                return unityObject.name + " (" + value.GetType().Name + ")";
            }

            return value.ToString();
        }

        private static string GetFriendlyTypeName(Type type)
        {
            return type == null ? "<null>" : type.Name;
        }

        private static string FormatRect(Rect value)
        {
            return string.Format("x:{0:0.##}, y:{1:0.##}, w:{2:0.##}, h:{3:0.##}", value.x, value.y, value.width, value.height);
        }

        private static string FormatVector2(Vector2 value)
        {
            return string.Format("({0:0.##}, {1:0.##})", value.x, value.y);
        }

        private static string FormatVector4(Vector4 value)
        {
            return string.Format("({0:0.##}, {1:0.##}, {2:0.##}, {3:0.##})", value.x, value.y, value.z, value.w);
        }
    }
}
