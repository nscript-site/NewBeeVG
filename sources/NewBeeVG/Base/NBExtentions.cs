using SkiaSharp;
using System.Runtime.CompilerServices;

namespace NewBeeVG;

public static partial class NBExtentions
{
    extension(SKShader self)
    {
        public static SKShader CreateAlphaLinearGradient(SKPoint start, SKPoint end, float[] positions, float[] alphas,SKShaderTileMode tileMode = SKShaderTileMode.Clamp)
        {
            SKColor[] colors = new SKColor[alphas.Length];
            for (int i = 0; i < alphas.Length; i++)
            {
                colors[i] = new SKColor(255, 255, 255, (byte)(alphas[i] * 255));
            }
            return SKShader.CreateLinearGradient(start, end, colors, positions, tileMode);
        }

        //public static SKShader CreateAlphaLinearGradient(SKRect rect, SKShaderTileMode tileMode = SKShaderTileMode.Clamp)
        //{
        //    float[] positions = [0, 0, 1, 1];
        //    SKColor[] colors = new SKColor[alphas.Length];
        //    for (int i = 0; i < alphas.Length; i++)
        //    {
        //        colors[i] = new SKColor(255, 255, 255, (byte)(alphas[i] * 255));
        //    }
        //    return SKShader.CreateLinearGradient(start, end, colors, positions, tileMode);
        //}

        public static SKShader CreateAlphaRadialGradient(SKPoint center, float radius, float[] positions, float[] alphas, SKShaderTileMode tileMode = SKShaderTileMode.Clamp)
        {
            SKColor[] colors = new SKColor[alphas.Length];
            for (int i = 0; i < alphas.Length; i++)
            {
                colors[i] = new SKColor(255, 255, 255, (byte)(alphas[i] * 255));
            }
            return SKShader.CreateRadialGradient(center, radius, colors, positions, tileMode);
        }

        public static SKShader CreateRadialGradient(SKRect rect, SKColor c1, SKColor c2, float p1, float p2, SKShaderTileMode tileMode = SKShaderTileMode.Clamp)
        {
            // 渐变色标：圆心透明 → 紫色 → 外圈黑色
            SKColor[] colors = new[]
            {
                 c1, c1, c2, c2
            };

            // 对应每个颜色的径向位置 0~1
            float[] colorPositions = new[] { 0f, p1, p2, 1f };

            return SKShader.CreateRadialGradient(rect.Center, rect.MaxRadius, colors, colorPositions, tileMode);
        }
    }

    extension(SKRect self)
    {
        /// <summary>
        /// Centers another rectangle in this rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to center.</param>
        /// <returns>The centered rectangle.</returns>
        public SKRect CenterRect(SKRect rect)
        {
            return CreateFrom(
                self.Left + ((self.Width - rect.Width) / 2),
                self.Top + ((self.Height - rect.Height) / 2),
                rect.Width,
                rect.Height);
        }

        public static SKRect CreateFrom(float left, float top, float width, float height)
        {
            return new SKRect(left, top, left + width, top + height);
        }

        public SKPoint LeftMiddle => new SKPoint(self.Left, self.Top + self.Height / 2);

        public SKPoint RightMiddle => new SKPoint(self.Right, self.Top + self.Height / 2);

        public SKPoint TopMiddle => new SKPoint(self.Left + self.Width / 2, self.Top);

        public SKPoint BottomMiddle => new SKPoint(self.Left + self.Width / 2, self.Bottom);

        public SKPoint Center => new SKPoint(self.Left + self.Width / 2, self.Top + self.Height / 2);

        public SKPoint TopLeft => new SKPoint(self.Left, self.Top);

        public SKPoint TopRight => new SKPoint(self.Right, self.Top);

        public SKPoint BottomLeft => new SKPoint(self.Left, self.Bottom);

        public SKPoint BottomRight => new SKPoint(self.Right, self.Bottom);

        public float Area => self.Width * self.Height;

        public float MinRadius => Math.Min(self.Width, self.Height) / 2;

        public float MaxRadius => (float)Math.Sqrt(self.Width * self.Width + self.Height * self.Height) / 2;

        /// <summary>
        /// Gets the intersection of two rectangles.
        /// </summary>
        /// <param name="rect">The other rectangle.</param>
        /// <returns>The intersection.</returns>
        public SKRect IntersectRect(SKRect rect)
        {
            var newLeft = (rect.Left > self.Left) ? rect.Left : self.Left;
            var newTop = (rect.Top > self.Top) ? rect.Top : self.Top;
            var newRight = (rect.Right < self.Right) ? rect.Right : self.Right;
            var newBottom = (rect.Bottom < self.Bottom) ? rect.Bottom : self.Bottom;

            if ((newRight > newLeft) && (newBottom > newTop))
            {
                return new SKRect(newLeft, newTop, newRight, newBottom);
            }
            else
            {
                return default;
            }
        }
    }

    public static SKPoint Interpolate(this ValueTuple<SKPoint, SKPoint> self, double t)
    {
        return self.Interpolate((float)t);
    }

    public static SKPoint Interpolate(this ValueTuple<SKPoint, SKPoint> self, float t)
    {
        return new SKPoint(
            self.Item1.X + (self.Item2.X - self.Item1.X) * t,
            self.Item1.Y + (self.Item2.Y - self.Item1.Y) * t);
    }

    public static void Render(this Func<NBDrawContext, NBClip, NBVisual?> builder, NBDrawContext ctx, NBClip clip, SKCanvas canvas)
    {
        builder.RenderCore(ctx, clip, canvas);
    }

    internal static NBLayoutable? RenderCore(this Func<NBDrawContext, NBClip, NBVisual?> builder, NBDrawContext ctx, NBClip clip, SKCanvas canvas, NBLayoutable? refLayout = null)
    {
        var content = builder(ctx, clip);
        if (content == null) return null;

        NBFrameUpdateEvent e = new NBFrameUpdateEvent(ctx);

        var panel = Methods.Panel([content]);
        panel.TryInvalidateMeasure();
        panel.FireOnFrameUpdated(e);
        panel.Measure(ctx.width, ctx.height);
        panel.Arrange(0, 0, ctx.width, ctx.height);
        if (refLayout != null)
        {
            var boundedItems = content.FindBounded();
            if(boundedItems != null)
            {
                foreach (var item in boundedItems)
                {
                    var refItem = refLayout.Find(item.BoundedId);
                    if (refItem == null) continue;
                    item.Bounds = refItem.Bounds;
                }
            }
        }

        panel.Render(canvas);
        return panel;
    }

    public static void Render(this NBVisual? content, SKSize size, SKCanvas canvas, NBLayoutable? refLayout = null)
    {
        content.RenderCore(size, canvas, refLayout);
    }

    internal static NBLayoutable? RenderCore(this NBVisual? content, SKSize size, SKCanvas canvas, NBLayoutable? refLayout = null)
    {
        if (content == null) return null;

        NBFrameUpdateEvent e = new NBFrameUpdateEvent(NBDrawContext.Current ?? new NBDrawContext());

        var panel = Methods.Panel([content]);
        panel.TryInvalidateMeasure();
        panel.FireOnFrameUpdated(e);
        panel.Measure(size.Width, size.Height);
        panel.Arrange(0, 0, size.Width, size.Height);
        if (refLayout != null)
        {
            var boundedItems = content.FindBounded();
            if (boundedItems != null)
            {
                foreach (var item in boundedItems)
                {
                    var refItem = refLayout.Find(item.BoundedId);
                    if (refItem == null) continue;
                    item.Bounds = refItem.Bounds;
                }
            }
        }

        panel.Render(canvas);
        return panel;
    }

    public static T Styles<T>(this T t, Action<T>[]? styles) where T : NBVisual
    {
        if (styles != null)
        {
            foreach (var style in styles)
            {
                style(t);
            }
        }
        return t;
    }

    public static Vector CalculateScaling(
        this Stretch stretch,
        SKSize destinationSize,
        SKSize sourceSize,
        StretchDirection stretchDirection = NewBeeVG.StretchDirection.Both)
    {
        return stretch.CalculateScaling(new Size(destinationSize.Width, destinationSize.Height), new Size(sourceSize.Width, sourceSize.Height), stretchDirection);
    }

    /// <summary>
    /// Calculates scaling based on a <see cref="Stretch"/> value.
    /// </summary>
    /// <param name="stretch">The stretch mode.</param>
    /// <param name="destinationSize">The size of the destination viewport.</param>
    /// <param name="sourceSize">The size of the source.</param>
    /// <param name="stretchDirection">The stretch direction.</param>
    /// <returns>A vector with the X and Y scaling factors.</returns>
    public static Vector CalculateScaling(
        this Stretch stretch,
        Size destinationSize,
        Size sourceSize,
        StretchDirection stretchDirection = NewBeeVG.StretchDirection.Both)
    {
        var scaleX = 1.0;
        var scaleY = 1.0;

        bool isConstrainedWidth = !double.IsPositiveInfinity(destinationSize.Width);
        bool isConstrainedHeight = !double.IsPositiveInfinity(destinationSize.Height);

        if ((stretch == NewBeeVG.Stretch.Uniform || stretch == NewBeeVG.Stretch.UniformToFill || stretch == NewBeeVG.Stretch.Fill)
             && (isConstrainedWidth || isConstrainedHeight))
        {
            // Compute scaling factors for both axes
            scaleX = MathUtilities.IsZero(sourceSize.Width) ? 0.0 : destinationSize.Width / sourceSize.Width;
            scaleY = MathUtilities.IsZero(sourceSize.Height) ? 0.0 : destinationSize.Height / sourceSize.Height;

            if (!isConstrainedWidth)
            {
                scaleX = scaleY;
            }
            else if (!isConstrainedHeight)
            {
                scaleY = scaleX;
            }
            else
            {
                // If not preserving aspect ratio, then just apply transform to fit
                switch (stretch)
                {
                    case NewBeeVG.Stretch.Uniform:
                        // Find minimum scale that we use for both axes
                        double minscale = scaleX < scaleY ? scaleX : scaleY;
                        scaleX = scaleY = minscale;
                        break;

                    case NewBeeVG.Stretch.UniformToFill:
                        // Find maximum scale that we use for both axes
                        double maxscale = scaleX > scaleY ? scaleX : scaleY;
                        scaleX = scaleY = maxscale;
                        break;

                    case NewBeeVG.Stretch.Fill:
                        // We already computed the fill scale factors above, so just use them
                        break;
                }
            }

            // Apply stretch direction by bounding scales.
            // In the uniform case, scaleX=scaleY, so this sort of clamping will maintain aspect ratio
            // In the uniform fill case, we have the same result too.
            // In the fill case, note that we change aspect ratio, but that is okay
            switch (stretchDirection)
            {
                case NewBeeVG.StretchDirection.UpOnly:
                    if (scaleX < 1.0)
                        scaleX = 1.0;
                    if (scaleY < 1.0)
                        scaleY = 1.0;
                    break;

                case NewBeeVG.StretchDirection.DownOnly:
                    if (scaleX > 1.0)
                        scaleX = 1.0;
                    if (scaleY > 1.0)
                        scaleY = 1.0;
                    break;

                case NewBeeVG.StretchDirection.Both:
                    break;

                default:
                    break;
            }
        }

        return new Vector(scaleX, scaleY);
    }

    /// <summary>
    /// Calculates a scaled size based on a <see cref="Stretch"/> value.
    /// </summary>
    /// <param name="stretch">The stretch mode.</param>
    /// <param name="destinationSize">The size of the destination viewport.</param>
    /// <param name="sourceSize">The size of the source.</param>
    /// <param name="stretchDirection">The stretch direction.</param>
    /// <returns>The size of the stretched source.</returns>
    public static Size CalculateSize(
        this Stretch stretch,
        Size destinationSize,
        Size sourceSize,
        StretchDirection stretchDirection = NewBeeVG.StretchDirection.Both)
    {
        return sourceSize * stretch.CalculateScaling(destinationSize, sourceSize, stretchDirection);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool HasAllFlags<T>(this T value, T flags) where T : unmanaged, Enum
    {
        if (sizeof(T) == 1)
        {
            var byteValue = Unsafe.As<T, byte>(ref value);
            var byteFlags = Unsafe.As<T, byte>(ref flags);
            return (byteValue & byteFlags) == byteFlags;
        }
        else if (sizeof(T) == 2)
        {
            var shortValue = Unsafe.As<T, short>(ref value);
            var shortFlags = Unsafe.As<T, short>(ref flags);
            return (shortValue & shortFlags) == shortFlags;
        }
        else if (sizeof(T) == 4)
        {
            var intValue = Unsafe.As<T, int>(ref value);
            var intFlags = Unsafe.As<T, int>(ref flags);
            return (intValue & intFlags) == intFlags;
        }
        else if (sizeof(T) == 8)
        {
            var longValue = Unsafe.As<T, long>(ref value);
            var longFlags = Unsafe.As<T, long>(ref flags);
            return (longValue & longFlags) == longFlags;
        }
        else
            throw new NotSupportedException("Enum with size of " + Unsafe.SizeOf<T>() + " are not supported");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool HasAnyFlag<T>(this T value, T flags) where T : unmanaged, Enum
    {
        if (sizeof(T) == 1)
        {
            var byteValue = Unsafe.As<T, byte>(ref value);
            var byteFlags = Unsafe.As<T, byte>(ref flags);
            return (byteValue & byteFlags) != 0;
        }
        else if (sizeof(T) == 2)
        {
            var shortValue = Unsafe.As<T, short>(ref value);
            var shortFlags = Unsafe.As<T, short>(ref flags);
            return (shortValue & shortFlags) != 0;
        }
        else if (sizeof(T) == 4)
        {
            var intValue = Unsafe.As<T, int>(ref value);
            var intFlags = Unsafe.As<T, int>(ref flags);
            return (intValue & intFlags) != 0;
        }
        else if (sizeof(T) == 8)
        {
            var longValue = Unsafe.As<T, long>(ref value);
            var longFlags = Unsafe.As<T, long>(ref flags);
            return (longValue & longFlags) != 0;
        }
        else
            throw new NotSupportedException("Enum with size of " + Unsafe.SizeOf<T>() + " are not supported");
    }
}
