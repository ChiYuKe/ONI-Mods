using System;
using System.Collections.Generic;
using StorageNetwork.Trading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StorageNetworkTrading.UI
{
    // A canvas graphic stays in the modal's UI layer and obeys its clipping and scale.
    internal sealed class StorageNetworkPriceChart : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler
    {
        private IReadOnlyList<TradingPricePoint> points;
        private decimal minimum, maximum;
        private int selected = -1;
        private bool hovering;
        internal Action<int> OnSelection;
        internal Color BuyColor, SellColor;
        internal bool SingleSeries;

        internal void Configure(IReadOnlyList<TradingPricePoint> data, decimal min, decimal max)
        {
            points = data;
            minimum = min;
            maximum = max;
            selected = data.Count - 1;
            SetVerticesDirty();
        }

        public void OnPointerEnter(PointerEventData eventData) { hovering = true; }
        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            Select(points != null ? points.Count - 1 : -1);
        }

        private void Update()
        {
            if (!hovering || points == null || points.Count == 0) return;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, camera, out Vector2 local)) return;
            Rect rect = rectTransform.rect;
            float fraction = Mathf.Clamp01((local.x - rect.xMin) / Mathf.Max(1, rect.width));
            float cycle = points[0].Cycle + fraction * (points[points.Count - 1].Cycle - points[0].Cycle);
            int nearest = 0;
            for (int i = 1; i < points.Count; i++)
                if (Mathf.Abs(points[i].Cycle - cycle) < Mathf.Abs(points[nearest].Cycle - cycle)) nearest = i;
            Select(nearest);
        }

        private void Select(int index)
        {
            if (index == selected) return;
            selected = index;
            SetVerticesDirty();
            OnSelection?.Invoke(index);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Color grid = new Color(0.35f, 0.36f, 0.33f, 0.24f);
            for (int i = 0; i <= 4; i++)
            {
                float y = rect.yMin + rect.height * i / 4;
                Line(mesh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), grid, 1);
            }
            Line(mesh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax), grid, 1);
            if (points == null || points.Count == 0 || maximum <= minimum) return;
            if (selected >= 0 && selected < points.Count)
            {
                float x = Position(rect, selected, points[selected].Buy).x;
                Line(mesh, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), grid, 1);
            }
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 buy = Position(rect, i, points[i].Buy);
                Vector2 sell = Position(rect, i, points[i].Sell);
                if (i > 0)
                {
                    Line(mesh, Position(rect, i - 1, points[i - 1].Buy), buy, BuyColor, 2);
                    if (!SingleSeries) Line(mesh, Position(rect, i - 1, points[i - 1].Sell), sell, SellColor, 2);
                }
                Dot(mesh, buy, BuyColor, 3);
                if (!SingleSeries) Dot(mesh, sell, SellColor, 3);
            }
            if (selected >= 0 && selected < points.Count)
            {
                Vector2 buy = Position(rect, selected, points[selected].Buy);
                Dot(mesh, buy, BuyColor, 6);
                if (!SingleSeries) Dot(mesh, Position(rect, selected, points[selected].Sell), SellColor, 6);
            }
        }

        private Vector2 Position(Rect rect, int index, decimal price)
        {
            float x = points.Count == 1 ? rect.center.x : rect.xMin + rect.width *
                (points[index].Cycle - points[0].Cycle) / (points[points.Count - 1].Cycle - points[0].Cycle);
            return new Vector2(x, rect.yMin + rect.height * (float)((price - minimum) / (maximum - minimum)));
        }

        private float FeatherWidth => 1.25f / Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f);

        private void Line(VertexHelper mesh, Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.000001f) return;
            Vector2 unitNormal = new Vector2(-direction.y, direction.x).normalized;
            Vector2 normal = unitNormal * width * 0.5f;
            Vector2 outer = normal + unitNormal * FeatherWidth;
            Color transparent = new Color(color.r, color.g, color.b, 0);
            // UI canvases do not guarantee MSAA. Coverage strips fade each edge
            // over roughly one screen pixel instead of rasterizing a hard quad.
            Quad(mesh, from - normal, from + normal, to + normal, to - normal, color);
            Quad(mesh, from + normal, from + outer, to + outer, to + normal,
                color, transparent, transparent, color);
            Quad(mesh, from - outer, from - normal, to - normal, to - outer,
                transparent, color, color, transparent);
        }

        private void Dot(VertexHelper mesh, Vector2 center, Color color, float size)
        {
            const int segments = 24;
            float radius = size * 0.5f;
            Color transparent = new Color(color.r, color.g, color.b, 0);
            int start = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(center + direction * radius, color, Vector2.zero);
                mesh.AddVert(center + direction * (radius + FeatherWidth), transparent, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int inner = start + 1 + i * 2;
                mesh.AddTriangle(start, inner, inner + 2);
                mesh.AddTriangle(inner, inner + 1, inner + 3);
                mesh.AddTriangle(inner, inner + 3, inner + 2);
            }
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            Quad(mesh, a, b, c, d, color, color, color, color);
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            Color colorA, Color colorB, Color colorC, Color colorD)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, colorA, Vector2.zero);
            mesh.AddVert(b, colorB, Vector2.zero);
            mesh.AddVert(c, colorC, Vector2.zero);
            mesh.AddVert(d, colorD, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
