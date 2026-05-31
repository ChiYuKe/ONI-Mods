using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DebugUI
{
    internal static class DebugUIOverlay
    {
        private static bool enabled;
        private static GameObject root;
        private static RectTransform lockedTarget;
        private static Rect? targetScreenRect;
        private static string targetOutlineLabel;
        private static float panelStep = 1f;
        private static float missingCanvasLogTimer;
        private static Rect panelGuiRect = new Rect(24f, 110f, 600f, 520f);
        private static Rect probeGuiRect = new Rect(180f, 180f, 52f, 52f);
        private static Vector2 sampleScreenPosition = new Vector2(206f, Screen.height - 206f);
        private static Vector2 infoScrollPosition;
        private static string currentInfo = "DebugUI\nCtrl+F8 toggle\nDrag the pink probe to inspect UI.";
        private static Texture2D probeTexture;
        private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        private static readonly List<Transform> hierarchyAncestors = new List<Transform>();
        private static readonly StringBuilder builder = new StringBuilder(1024);
        private static readonly StringBuilder pathBuilder = new StringBuilder(512);

        public static void Tick()
        {
            if (IsCtrlDown() && Input.GetKeyDown(KeyCode.F8))
            {
                enabled = !enabled;
                EnsureRoot();
                if (root != null)
                {
                    root.SetActive(enabled);
                    Debug.Log("[DebugUI] Overlay " + (enabled ? "enabled." : "disabled."));
                }
                else
                {
                    Debug.LogWarning("[DebugUI] Toggle requested, but no UI canvas is available yet.");
                }
            }

            if (!enabled)
            {
                return;
            }

            EnsureRoot();
            if (root != null)
            {
                root.transform.SetAsLastSibling();
            }

            if (IsCtrlDown() && Input.GetKeyDown(KeyCode.F9))
            {
                ToggleLock();
            }

            ApplyLockedAdjustments();
            UpdateHover();
            UpdatePanelControls();

            if (IsCtrlDown() && Input.GetKeyDown(KeyCode.C))
            {
                CopyCurrentInfo();
            }
        }

        private static void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            Transform parent = GameScreenManager.Instance?.ssOverlayCanvas?.transform;
            if (parent == null)
            {
                parent = Global.Instance?.globalCanvas?.transform;
            }

            if (parent == null)
            {
                if (Time.unscaledTime >= missingCanvasLogTimer)
                {
                    missingCanvasLogTimer = Time.unscaledTime + 5f;
                    Debug.Log("[DebugUI] Waiting for canvas...");
                }

                return;
            }

            root = new GameObject("DebugUIOverlay", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            root.SetActive(enabled);
            Debug.Log("[DebugUI] Overlay root created under " + parent.name + ".");
        }

        private static bool IsCtrlDown()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private static bool IsShiftDown()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private static bool IsAltDown()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        public static void DrawProbeGUI()
        {
            if (!enabled)
            {
                return;
            }

            panelGuiRect = GUI.Window(927380, panelGuiRect, DrawPanelWindow, GUIContent.none, GUIStyle.none);
            panelGuiRect.x = Mathf.Clamp(panelGuiRect.x, 0f, Screen.width - panelGuiRect.width);
            panelGuiRect.y = Mathf.Clamp(panelGuiRect.y, 0f, Screen.height - panelGuiRect.height);

            probeGuiRect = GUI.Window(927381, probeGuiRect, DrawProbeWindow, GUIContent.none, GUIStyle.none);
            probeGuiRect.x = Mathf.Clamp(probeGuiRect.x, 0f, Screen.width - probeGuiRect.width);
            probeGuiRect.y = Mathf.Clamp(probeGuiRect.y, 0f, Screen.height - probeGuiRect.height);
            sampleScreenPosition = new Vector2(probeGuiRect.center.x, Screen.height - probeGuiRect.center.y);

            DrawTargetOutlineGUI();
        }

        private static void DrawPanelWindow(int id)
        {
            EnsureProbeTexture();
            DrawFilledRect(new Rect(0f, 0f, panelGuiRect.width, panelGuiRect.height), new Color(0.05f, 0.06f, 0.07f, 0.84f), 0);

            GUILayout.BeginArea(new Rect(10f, 8f, panelGuiRect.width - 20f, panelGuiRect.height - 16f));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(lockedTarget == null ? "Lock" : "Unlock", GUILayout.Width(58f), GUILayout.Height(24f)))
            {
                ToggleLock();
            }
            if (GUILayout.Button("Copy", GUILayout.Width(58f), GUILayout.Height(24f)))
            {
                CopyCurrentInfo();
            }
            if (GUILayout.Button("Step " + panelStep.ToString("0"), GUILayout.Width(72f), GUILayout.Height(24f)))
            {
                TogglePanelStep();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            DrawAdjustButton("X-", new Vector2(-1f, 0f), false);
            DrawAdjustButton("X+", new Vector2(1f, 0f), false);
            DrawAdjustButton("Y-", new Vector2(0f, -1f), false);
            DrawAdjustButton("Y+", new Vector2(0f, 1f), false);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            DrawAdjustButton("W-", new Vector2(-1f, 0f), true);
            DrawAdjustButton("W+", new Vector2(1f, 0f), true);
            DrawAdjustButton("H-", new Vector2(0f, -1f), true);
            DrawAdjustButton("H+", new Vector2(0f, 1f), true);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            infoScrollPosition = GUILayout.BeginScrollView(infoScrollPosition, GUILayout.Width(panelGuiRect.width - 20f), GUILayout.Height(panelGuiRect.height - 120f));
            GUILayout.Label(currentInfo);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.DragWindow(new Rect(0f, 0f, panelGuiRect.width, 28f));
        }

        private static void DrawAdjustButton(string label, Vector2 direction, bool resize)
        {
            GUI.enabled = lockedTarget != null;
            if (GUILayout.Button(label, GUILayout.Width(42f), GUILayout.Height(24f)))
            {
                AdjustLockedTarget(direction * panelStep, resize);
            }
            GUI.enabled = true;
        }

        private static void DrawProbeWindow(int id)
        {
            Event current = Event.current;
            bool hovered = probeGuiRect.Contains(current.mousePosition + probeGuiRect.position);
            bool activeDrag = GUIUtility.hotControl == id;
            Color accent = activeDrag
                ? ColorFromHex("C5153B")
                : hovered
                    ? ColorFromHex("F498AC")
                    : ColorFromHex("EC4F71");

            Rect full = new Rect(2f, 2f, 48f, 48f);
            Rect border = new Rect(8f, 8f, 36f, 36f);
            Rect dot = new Rect(22f, 22f, 8f, 8f);

            DrawFilledRect(full, new Color(0f, 0f, 0f, 0.70f), 8);
            DrawFilledRect(dot, accent, 4);
            DrawRectOutline(border, accent, 4f, 8);
            GUI.DragWindow(new Rect(0f, 0f, probeGuiRect.width, probeGuiRect.height));
        }

        private static void DrawTargetOutlineGUI()
        {
            if (!targetScreenRect.HasValue)
            {
                return;
            }

            Rect rect = targetScreenRect.Value;
            Color border = ColorFromHex("EC4F71");
            Color fill = new Color(border.r, border.g, border.b, 0.12f);
            DrawFilledRect(rect, fill, 0);
            DrawRectOutline(rect, border, 3f, 0);

            if (!string.IsNullOrEmpty(targetOutlineLabel))
            {
                Rect labelRect = new Rect(rect.x, Mathf.Max(0f, rect.y - 22f), Mathf.Max(220f, rect.width), 20f);
                DrawFilledRect(labelRect, new Color(0f, 0f, 0f, 0.70f), 0);
                GUI.Label(new Rect(labelRect.x + 4f, labelRect.y + 2f, labelRect.width - 8f, labelRect.height - 4f), targetOutlineLabel);
            }
        }

        private static Color ColorFromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            {
                return color;
            }

            return Color.magenta;
        }

        private static void DrawFilledRect(Rect rect, Color color, int radius)
        {
            EnsureProbeTexture();
            Color previous = GUI.color;
            GUI.color = color;

            if (radius <= 0)
            {
                GUI.DrawTexture(rect, probeTexture);
            }
            else
            {
                GUI.DrawTexture(new Rect(rect.x + radius, rect.y, rect.width - radius * 2f, rect.height), probeTexture);
                GUI.DrawTexture(new Rect(rect.x, rect.y + radius, rect.width, rect.height - radius * 2f), probeTexture);
                GUI.DrawTexture(new Rect(rect.x + radius * 0.35f, rect.y + radius * 0.35f, radius, radius), probeTexture);
                GUI.DrawTexture(new Rect(rect.xMax - radius * 1.35f, rect.y + radius * 0.35f, radius, radius), probeTexture);
                GUI.DrawTexture(new Rect(rect.x + radius * 0.35f, rect.yMax - radius * 1.35f, radius, radius), probeTexture);
                GUI.DrawTexture(new Rect(rect.xMax - radius * 1.35f, rect.yMax - radius * 1.35f, radius, radius), probeTexture);
            }

            GUI.color = previous;
        }

        private static void DrawRectOutline(Rect rect, Color color, float thickness, int radius)
        {
            DrawFilledRect(new Rect(rect.x + radius, rect.y, rect.width - radius * 2f, thickness), color, 0);
            DrawFilledRect(new Rect(rect.x + radius, rect.yMax - thickness, rect.width - radius * 2f, thickness), color, 0);
            DrawFilledRect(new Rect(rect.x, rect.y + radius, thickness, rect.height - radius * 2f), color, 0);
            DrawFilledRect(new Rect(rect.xMax - thickness, rect.y + radius, thickness, rect.height - radius * 2f), color, 0);

            DrawFilledRect(new Rect(rect.x + radius * 0.35f, rect.y + radius * 0.35f, radius, radius), color, radius);
            DrawFilledRect(new Rect(rect.xMax - radius * 1.35f, rect.y + radius * 0.35f, radius, radius), color, radius);
            DrawFilledRect(new Rect(rect.x + radius * 0.35f, rect.yMax - radius * 1.35f, radius, radius), color, radius);
            DrawFilledRect(new Rect(rect.xMax - radius * 1.35f, rect.yMax - radius * 1.35f, radius, radius), color, radius);
        }

        private static void EnsureProbeTexture()
        {
            if (probeTexture != null)
            {
                return;
            }

            probeTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            probeTexture.SetPixel(0, 0, Color.white);
            probeTexture.Apply();
        }

        private static void UpdateHover()
        {
            RectTransform target = lockedTarget != null ? lockedTarget : FindHoveredRect();
            if (target == null)
            {
                lockedTarget = null;
                targetScreenRect = null;
                targetOutlineLabel = null;
                currentInfo = "DebugUI\nCtrl+F8 toggle\nCtrl+F9 lock current\nCtrl+C copy info\nDrag the pink probe to inspect UI.\nNo UI hit under probe.";
                return;
            }

            targetScreenRect = GetScreenRect(target);
            targetOutlineLabel = GetPath(target.transform);
            currentInfo = BuildInfo(target);
        }

        private static void ToggleLock()
        {
            if (lockedTarget != null)
            {
                Debug.Log("[DebugUI] Target unlocked.");
                lockedTarget = null;
                return;
            }

            lockedTarget = FindHoveredRect();
            if (lockedTarget != null)
            {
                Debug.Log("[DebugUI] Target locked: " + GetPath(lockedTarget.transform));
            }
            else
            {
                Debug.Log("[DebugUI] No target to lock.");
            }
        }

        private static void TogglePanelStep()
        {
            panelStep = panelStep >= 10f ? 1f : 10f;
            UpdatePanelControls();
        }

        private static void UpdatePanelControls()
        {
        }

        private static void ApplyLockedAdjustments()
        {
            if (lockedTarget == null || !IsCtrlDown())
            {
                return;
            }

            float step = IsShiftDown() ? 10f : 1f;
            Vector2 delta = Vector2.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                delta.x -= step;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                delta.x += step;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                delta.y -= step;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                delta.y += step;
            }

            if (delta == Vector2.zero)
            {
                return;
            }

            AdjustLockedTarget(delta, IsAltDown());
        }

        private static void AdjustLockedTarget(Vector2 delta, bool resize)
        {
            if (lockedTarget == null || delta == Vector2.zero)
            {
                return;
            }

            if (resize)
            {
                lockedTarget.sizeDelta += delta;
                LayoutElement layout = lockedTarget.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    if (delta.x != 0f && layout.preferredWidth >= 0f)
                    {
                        layout.preferredWidth += delta.x;
                    }
                    if (delta.y != 0f && layout.preferredHeight >= 0f)
                    {
                        layout.preferredHeight += delta.y;
                    }
                }
            }
            else
            {
                lockedTarget.anchoredPosition += delta;
            }
        }

        private static void CopyCurrentInfo()
        {
            if (string.IsNullOrEmpty(currentInfo))
            {
                Debug.Log("[DebugUI] No info to copy.");
                return;
            }

            GUIUtility.systemCopyBuffer = currentInfo;
            Debug.Log("[DebugUI] Current UI info copied to clipboard.");
        }

        private static RectTransform FindHoveredRect()
        {
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                return null;
            }

            Vector2 inspectPosition = GetProbeScreenPosition();
            raycastResults.Clear();
            UnityEngine.EventSystems.PointerEventData pointer = new UnityEngine.EventSystems.PointerEventData(eventSystem)
            {
                position = inspectPosition
            };
            eventSystem.RaycastAll(pointer, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject hit = raycastResults[i].gameObject;
                if (hit == null || root != null && hit.transform.IsChildOf(root.transform))
                {
                    continue;
                }

                RectTransform rect = hit.GetComponentInParent<RectTransform>();
                if (rect != null)
                {
                    return rect;
                }
            }

            return null;
        }

        private static Vector2 GetProbeScreenPosition()
        {
            return sampleScreenPosition;
        }

        private static string BuildInfo(RectTransform rect)
        {
            builder.Length = 0;
            builder.Append("DebugUI  Ctrl+F8 toggle  Ctrl+F9 ");
            builder.AppendLine(lockedTarget == rect ? "unlock" : "lock");
            builder.AppendLine("Ctrl+C copy info");
            builder.AppendLine("Drag the pink probe to inspect UI");
            if (lockedTarget == rect)
            {
                builder.AppendLine("Panel buttons move/resize selected UI");
                builder.AppendLine("Ctrl+Arrows move  Ctrl+Alt+Arrows resize");
                builder.AppendLine("Hold Shift for x10");
            }
            builder.Append("name: ").AppendLine(GetPath(rect.transform));
            builder.Append("rect: ").Append(rect.rect.width.ToString("0.0")).Append(" x ").AppendLine(rect.rect.height.ToString("0.0"));
            builder.Append("pos: ").Append(rect.anchoredPosition.x.ToString("0.0")).Append(", ").AppendLine(rect.anchoredPosition.y.ToString("0.0"));
            builder.Append("pivot: ").Append(rect.pivot.x.ToString("0.00")).Append(", ").AppendLine(rect.pivot.y.ToString("0.00"));
            builder.Append("anchorMin: ").Append(rect.anchorMin.x.ToString("0.00")).Append(", ").AppendLine(rect.anchorMin.y.ToString("0.00"));
            builder.Append("anchorMax: ").Append(rect.anchorMax.x.ToString("0.00")).Append(", ").AppendLine(rect.anchorMax.y.ToString("0.00"));
            builder.Append("offsetMin: ").Append(rect.offsetMin.x.ToString("0.0")).Append(", ").AppendLine(rect.offsetMin.y.ToString("0.0"));
            builder.Append("offsetMax: ").Append(rect.offsetMax.x.ToString("0.0")).Append(", ").AppendLine(rect.offsetMax.y.ToString("0.0"));
            builder.AppendLine("hierarchy:");
            AppendHierarchy(rect);

            LayoutElement layout = rect.GetComponent<LayoutElement>();
            if (layout != null)
            {
                builder.Append("layout preferred: ").Append(layout.preferredWidth.ToString("0.0")).Append(" x ").AppendLine(layout.preferredHeight.ToString("0.0"));
                builder.Append("layout min: ").Append(layout.minWidth.ToString("0.0")).Append(" x ").AppendLine(layout.minHeight.ToString("0.0"));
                builder.Append("layout flexible: ").Append(layout.flexibleWidth.ToString("0.0")).Append(" x ").AppendLine(layout.flexibleHeight.ToString("0.0"));
            }

            HorizontalOrVerticalLayoutGroup group = rect.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (group != null)
            {
                builder.Append("group spacing: ").AppendLine(group.spacing.ToString("0.0"));
                builder.Append("group childControl: ").Append(group.childControlWidth).Append(" / ").AppendLine(group.childControlHeight.ToString());
                builder.Append("group childExpand: ").Append(group.childForceExpandWidth).Append(" / ").AppendLine(group.childForceExpandHeight.ToString());
            }

            return builder.ToString();
        }

        private static void AppendHierarchy(RectTransform rect)
        {
            const int maxAncestors = 8;
            const int maxChildren = 8;

            hierarchyAncestors.Clear();
            Transform current = rect.transform;
            while (current != null && hierarchyAncestors.Count < maxAncestors)
            {
                hierarchyAncestors.Add(current);
                current = current.parent;
                if (current != null && current.name == "DebugUIOverlay")
                {
                    break;
                }
            }

            for (int i = hierarchyAncestors.Count - 1; i >= 0; i--)
            {
                Transform node = hierarchyAncestors[i];
                int depth = hierarchyAncestors.Count - 1 - i;
                AppendIndent(depth);
                builder.Append(node == rect.transform ? "> " : "- ");
                builder.Append(node.name);
                builder.Append(" [").Append(node.GetSiblingIndex()).Append("/");
                builder.Append(node.parent == null ? 0 : node.parent.childCount).Append("]");
                builder.Append(" children=").AppendLine(node.childCount.ToString());
            }

            hierarchyAncestors.Clear();

            int shown = 0;
            for (int i = 0; i < rect.childCount && shown < maxChildren; i++)
            {
                Transform child = rect.GetChild(i);
                if (child == null || child.name == "DebugUIOverlay")
                {
                    continue;
                }

                AppendIndent(Mathf.Min(maxAncestors, 9));
                builder.Append("- ");
                builder.Append(child.name);
                builder.Append(" [").Append(child.GetSiblingIndex()).Append("/");
                builder.Append(rect.childCount).Append("]");
                builder.Append(" children=").AppendLine(child.childCount.ToString());
                shown++;
            }

            if (rect.childCount > shown)
            {
                AppendIndent(Mathf.Min(maxAncestors, 9));
                builder.Append("... +").Append(rect.childCount - shown).AppendLine(" children");
            }
        }

        private static void AppendIndent(int depth)
        {
            for (int i = 0; i < depth; i++)
            {
                builder.Append("  ");
            }
        }

        private static string GetPath(Transform transform)
        {
            pathBuilder.Length = 0;
            Transform current = transform;
            while (current != null)
            {
                if (pathBuilder.Length == 0)
                {
                    pathBuilder.Insert(0, current.name);
                }
                else
                {
                    pathBuilder.Insert(0, current.name + "/");
                }

                current = current.parent;
                if (current != null && current.name == "DebugUIOverlay")
                {
                    break;
                }
            }

            return pathBuilder.ToString();
        }

        private static Rect? GetScreenRect(RectTransform target)
        {
            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);
            Camera eventCamera = GetEventCamera(target);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCorners[i]);
                Vector2 guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
                min = Vector2.Min(min, guiPoint);
                max = Vector2.Max(max, guiPoint);
            }

            if (min.x == float.MaxValue || max.x == float.MinValue)
            {
                return null;
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Camera GetEventCamera(RectTransform target)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera ?? Camera.main;
        }

    }
}
