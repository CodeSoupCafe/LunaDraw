/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

namespace LunaDraw.Logic.Constants;

public static class AppConstants
{
    public static class Directories
    {
        public const string Gallery = "Gallery";
    }

    public static class Files
    {
        public const string JsonExtension = ".json";
        public const string JsonSearchPattern = "*.json";
    }

    public static class Defaults
    {
        public const string UntitledDrawingName = "Untitled";
        public const string LayerName = "Layer";
    }

    public static class Themes
    {
        public const string Automatic = "Automatic";
        public const string Light = "Light";
        public const string Dark = "Dark";
    }

    public static class JsonProperties
    {
        public const string Id = "i";
        public const string Name = "n";
        public const string LastModified = "lm";
        public const string CanvasWidth = "cw";
        public const string CanvasHeight = "ch";
        public const string Layers = "l";
        public const string Visible = "v";
        public const string Locked = "lk";
        public const string MaskingMode = "m";
        public const string Elements = "e";
        public const string ZIndex = "z";
        public const string Opacity = "o";
        public const string FillColor = "fc";
        public const string StrokeColor = "sc";
        public const string StrokeWidth = "sw";
        public const string GlowEnabled = "ge";
        public const string GlowColor = "gc";
        public const string GlowRadius = "gr";
        public const string TransformMatrix = "tm";
        public const string PathData = "pd";
        public const string Filled = "f";
        public const string BlendMode = "b";
        public const string TypeDiscriminator = "P";
        public const string PathType = "Path";
    }

    public static class UI
    {
        public const string Duplicate = "Duplicate";
        public const string Copy = "Copy";
        public const string Paste = "Paste";
        public const string Arrange = "Arrange";
        public const string SendToBack = "Send To Back";
        public const string SendBackward = "Send Backward";
        public const string BringForward = "Bring Forward";
        public const string SendToFront = "Send To Front";
        public const string MoveTo = "Move to";
        public const string NewLayer = "New Layer";
    }
}
