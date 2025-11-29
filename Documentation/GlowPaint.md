For glow paint masks can be applied
eg: glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, glowRadius);

Key Concepts for Shapes
Draw Order Matters: You must draw the blurred version underneath the sharp version to get the standard "glowing object with a solid core" look.
SKMaskFilter.CreateBlur modifies the alpha channel of whatever you draw with that specific paint object, causing the edges to feather out.
SKPaintStyle.Fill vs. SKPaintStyle.Stroke:
Set both paints to Fill to make a glowing, solid sphere.
Set both paints to Stroke to create a glowing ring or outline. Remember to set the StrokeWidth on both paints if you use Stroke style.
This pattern is highly flexible and works for DrawRect, DrawPath, DrawOval, etc.
