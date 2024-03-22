using System;
using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
    public enum ResolutionMode
    {
        None,
        PerSegment,
        PerLine
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public class UIPrimitiveBase : MaskableGraphic, ILayoutElement, ICanvasRaycastFilter
    {
        protected static Material s_ETC1DefaultUI;

        [SerializeField] private Sprite m_Sprite;

        [SerializeField] private ResolutionMode m_improveResolution;

        [SerializeField] protected float m_Resolution;

        [SerializeField] private bool m_useNativeSize;

        private readonly List<Vector2> outputList = new();

        // Not serialized until we support read-enabled sprites better.
        internal float m_EventAlphaThreshold = 1;

        [NonSerialized] private Sprite m_OverrideSprite;

        protected UIPrimitiveBase() => useLegacyMeshGeneration = false;

        public Sprite sprite
        {
            get => m_Sprite;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Sprite, value)) GeneratedUVs();
                SetAllDirty();
            }
        }

        public Sprite overrideSprite
        {
            get => activeSprite;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_OverrideSprite, value)) GeneratedUVs();
                SetAllDirty();
            }
        }

        protected Sprite activeSprite => m_OverrideSprite != null ? m_OverrideSprite : sprite;

        public float eventAlphaThreshold
        {
            get => m_EventAlphaThreshold;
            set => m_EventAlphaThreshold = value;
        }

        public ResolutionMode ImproveResolution
        {
            get => m_improveResolution;
            set
            {
                m_improveResolution = value;
                SetAllDirty();
            }
        }

        public float Resolution
        {
            get => m_Resolution;
            set
            {
                m_Resolution = value;
                SetAllDirty();
            }
        }

        public bool UseNativeSize
        {
            get => m_useNativeSize;
            set
            {
                m_useNativeSize = value;
                SetAllDirty();
            }
        }

        /// <summary>
        ///     Default material used to draw everything if no explicit material was specified.
        /// </summary>

        public static Material defaultETC1GraphicMaterial
        {
            get
            {
                if (s_ETC1DefaultUI == null) s_ETC1DefaultUI = Canvas.GetETC1SupportedCanvasMaterial();
                return s_ETC1DefaultUI;
            }
        }

        /// <summary>
        ///     Image's texture comes from the UnityEngine.Image.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (activeSprite == null)
                {
                    if (material != null && material.mainTexture != null) return material.mainTexture;
                    return s_WhiteTexture;
                }

                return activeSprite.texture;
            }
        }

        /// <summary>
        ///     Whether the Image has a border to work with.
        /// </summary>

        public bool hasBorder
        {
            get
            {
                if (activeSprite != null)
                {
                    var v = activeSprite.border;
                    return v.sqrMagnitude > 0f;
                }

                return false;
            }
        }

        public float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                if (activeSprite) spritePixelsPerUnit = activeSprite.pixelsPerUnit;

                float referencePixelsPerUnit = 100;
                if (canvas) referencePixelsPerUnit = canvas.referencePixelsPerUnit;

                return spritePixelsPerUnit / referencePixelsPerUnit;
            }
        }

        public override Material material
        {
            get
            {
                if (m_Material != null) return m_Material;

                if (activeSprite && activeSprite.associatedAlphaSplitTexture != null) return defaultETC1GraphicMaterial;

                return defaultMaterial;
            }

            set => base.material = value;
        }

        #region onEnable

        protected override void OnEnable()
        {
            base.OnEnable();
            SetAllDirty();
        }

        #endregion


        protected UIVertex[] SetVbo(Vector2[] vertices, Vector2[] uvs)
        {
            var vbo = new UIVertex[4];
            for (var i = 0; i < vertices.Length; i++)
            {
                var vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = vertices[i];
                vert.uv0 = uvs[i];
                vbo[i] = vert;
            }

            return vbo;
        }

        protected Vector2[] IncreaseResolution(Vector2[] input) =>
            IncreaseResolution(new List<Vector2>(input)).ToArray();

        protected List<Vector2> IncreaseResolution(List<Vector2> input)
        {
            outputList.Clear();

            switch (ImproveResolution)
            {
                case ResolutionMode.PerLine:
                    float totalDistance = 0, increments = 0;
                    for (var i = 0; i < input.Count - 1; i++) totalDistance += Vector2.Distance(input[i], input[i + 1]);
                    ResolutionToNativeSize(totalDistance);
                    increments = totalDistance / m_Resolution;
                    var incrementCount = 0;
                    for (var i = 0; i < input.Count - 1; i++)
                    {
                        var p1 = input[i];
                        outputList.Add(p1);
                        var p2 = input[i + 1];
                        var segmentDistance = Vector2.Distance(p1, p2) / increments;
                        var incrementTime = 1f / segmentDistance;
                        for (var j = 0; j < segmentDistance; j++)
                        {
                            outputList.Add(Vector2.Lerp(p1, p2, j * incrementTime));
                            incrementCount++;
                        }

                        outputList.Add(p2);
                    }

                    break;
                case ResolutionMode.PerSegment:
                    for (var i = 0; i < input.Count - 1; i++)
                    {
                        var p1 = input[i];
                        outputList.Add(p1);
                        var p2 = input[i + 1];
                        ResolutionToNativeSize(Vector2.Distance(p1, p2));
                        increments = 1f / m_Resolution;
                        for (float j = 1; j < m_Resolution; j++) outputList.Add(Vector2.Lerp(p1, p2, increments * j));
                        outputList.Add(p2);
                    }

                    break;
            }

            return outputList;
        }

        protected virtual void GeneratedUVs()
        {
        }

        protected virtual void ResolutionToNativeSize(float distance)
        {
        }


        #region ILayoutElement Interface

        public virtual void CalculateLayoutInputHorizontal()
        {
        }

        public virtual void CalculateLayoutInputVertical()
        {
        }

        public virtual float minWidth => 0;

        public virtual float preferredWidth
        {
            get
            {
                if (overrideSprite == null) return 0;
                return overrideSprite.rect.size.x / pixelsPerUnit;
            }
        }

        public virtual float flexibleWidth => -1;

        public virtual float minHeight => 0;

        public virtual float preferredHeight
        {
            get
            {
                if (overrideSprite == null) return 0;
                return overrideSprite.rect.size.y / pixelsPerUnit;
            }
        }

        public virtual float flexibleHeight => -1;

        public virtual int layoutPriority => 0;

        #endregion

        #region ICanvasRaycastFilter Interface

        public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // add test for line check
            if (m_EventAlphaThreshold >= 1) return true;

            var sprite = overrideSprite;
            if (sprite == null) return true;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local);

            var rect = GetPixelAdjustedRect();

            // Convert to have lower left corner as reference point.
            local.x += rectTransform.pivot.x * rect.width;
            local.y += rectTransform.pivot.y * rect.height;

            local = MapCoordinate(local, rect);

            //test local coord with Mesh

            // Normalize local coordinates.
            var spriteRect = sprite.textureRect;
            var normalized = new Vector2(local.x / spriteRect.width, local.y / spriteRect.height);

            // Convert to texture space.
            var x = Mathf.Lerp(spriteRect.x, spriteRect.xMax, normalized.x) / sprite.texture.width;
            var y = Mathf.Lerp(spriteRect.y, spriteRect.yMax, normalized.y) / sprite.texture.height;

            try
            {
                return sprite.texture.GetPixelBilinear(x, y).a >= m_EventAlphaThreshold;
            }
            catch (UnityException e)
            {
                Debug.LogError(
                    "Using clickAlphaThreshold lower than 1 on Image whose sprite texture cannot be read. " + e.Message
                    + " Also make sure to disable sprite packing for this sprite.",
                    this
                );
                return true;
            }
        }

        /// <summary>
        ///     Return image adjusted position
        ///     **Copied from Unity's Image component for now and simplified for UI Extensions primitives
        /// </summary>
        /// <param name="local"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        private Vector2 MapCoordinate(Vector2 local, Rect rect)
        {
            var spriteRect = sprite.rect;
            //if (type == Type.Simple || type == Type.Filled)
            return new Vector2(local.x * rect.width, local.y * rect.height);

            //Vector4 border = sprite.border;
            //Vector4 adjustedBorder = GetAdjustedBorders(border / pixelsPerUnit, rect);

            //for (int i = 0; i < 2; i++)
            //{
            //    if (local[i] <= adjustedBorder[i])
            //        continue;

            //    if (rect.size[i] - local[i] <= adjustedBorder[i + 2])
            //    {
            //        local[i] -= (rect.size[i] - spriteRect.size[i]);
            //        continue;
            //    }

            //    if (type == Type.Sliced)
            //    {
            //        float lerp = Mathf.InverseLerp(adjustedBorder[i], rect.size[i] - adjustedBorder[i + 2], local[i]);
            //        local[i] = Mathf.Lerp(border[i], spriteRect.size[i] - border[i + 2], lerp);
            //        continue;
            //    }
            //    else
            //    {
            //        local[i] -= adjustedBorder[i];
            //        local[i] = Mathf.Repeat(local[i], spriteRect.size[i] - border[i] - border[i + 2]);
            //        local[i] += border[i];
            //        continue;
            //    }
            //}

            //return local;
        }

        private Vector4 GetAdjustedBorders(Vector4 border, Rect rect)
        {
            for (var axis = 0; axis <= 1; axis++)
            {
                // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
                // In order to avoid artefact's with overlapping borders, we scale the borders down to fit.
                var combinedBorders = border[axis] + border[axis + 2];
                if (rect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    var borderScaleRatio = rect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }

            return border;
        }

        #endregion
    }
}