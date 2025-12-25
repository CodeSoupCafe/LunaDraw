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

using System.Text.Json.Serialization;

namespace LunaDraw.Logic.Models;

public class External
{
  public class Drawing : CodeSoupCafe.Maui.Models.ISortable
  {
    [JsonPropertyName("i")]
    public Guid Id { get; set; }
    [JsonPropertyName("n")]
    public string Name { get; set; } = "Untitled";
    [JsonPropertyName("lm")]
    public DateTime LastModified { get; set; }
    [JsonPropertyName("cw")]
    public int CanvasWidth { get; set; }
    [JsonPropertyName("ch")]
    public int CanvasHeight { get; set; }
    [JsonPropertyName("l")]
    public List<Layer> Layers { get; set; } = [];

    // ISortable implementation (not serialized)
    [JsonIgnore]
    public string Title => Name;

    [JsonIgnore]
    public DateTimeOffset DateCreated => new DateTimeOffset(LastModified);

    [JsonIgnore]
    public DateTimeOffset DateUpdated => new DateTimeOffset(LastModified);
  }

  public class Layer
  {
    [JsonPropertyName("i")]
    public Guid Id { get; set; }
    [JsonPropertyName("n")]
    public string Name { get; set; } = "Layer";
    [JsonPropertyName("v")]
    public bool IsVisible { get; set; }
    [JsonPropertyName("lk")]
    public bool IsLocked { get; set; }
    [JsonPropertyName("m")]
    public int MaskingMode { get; set; }
    [JsonPropertyName("e")]
    public List<Element> Elements { get; set; } = [];
  }

  [JsonDerivedType(typeof(Path), typeDiscriminator: "P")]
  [JsonDerivedType(typeof(Stamps), typeDiscriminator: "S")]
  [JsonDerivedType(typeof(Rectangle), typeDiscriminator: "R")]
  [JsonDerivedType(typeof(Ellipse), typeDiscriminator: "E")]
  [JsonDerivedType(typeof(Line), typeDiscriminator: "L")]
  public class Element
  {
    [JsonPropertyName("i")]
    public Guid Id { get; set; }
    [JsonPropertyName("ca")]
    public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("v")]
    public bool IsVisible { get; set; }
    [JsonPropertyName("z")]
    public int ZIndex { get; set; }
    [JsonPropertyName("o")]
    public byte Opacity { get; set; }
    [JsonPropertyName("fc")]
    public string? FillColor { get; set; }
    [JsonPropertyName("sc")]
    public string StrokeColor { get; set; } = "Black";
    [JsonPropertyName("sw")]
    public float StrokeWidth { get; set; }
    [JsonPropertyName("ge")]
    public bool IsGlowEnabled { get; set; }
    [JsonPropertyName("gc")]
    public string GlowColor { get; set; } = "Transparent";
    [JsonPropertyName("gr")]
    public float GlowRadius { get; set; }
    [JsonPropertyName("tm")]
    public float[] TransformMatrix { get; set; } = new float[9];
  }

  public class Path : Element
  {
    [JsonPropertyName("pd")]
    public string PathData { get; set; } = string.Empty;
    [JsonPropertyName("f")]
    public bool IsFilled { get; set; }
    [JsonPropertyName("b")]
    public int BlendMode { get; set; }
  }

  public class Stamps : Element
  {
    [JsonPropertyName("pts")]
    public List<float[]> Points { get; set; } = [];
    [JsonPropertyName("st")]
    public int ShapeType { get; set; }
    [JsonPropertyName("s")]
    public float Size { get; set; }
    [JsonPropertyName("fl")]
    public byte Flow { get; set; }
    [JsonPropertyName("if")]
    public bool IsFilled { get; set; }
    [JsonPropertyName("bm")]
    public int BlendMode { get; set; }
    [JsonPropertyName("ire")]
    public bool IsRainbowEnabled { get; set; }
    [JsonPropertyName("r")]
    public List<float> Rotations { get; set; } = [];
    [JsonPropertyName("sj")]
    public float SizeJitter { get; set; }
    [JsonPropertyName("aj")]
    public float AngleJitter { get; set; }
    [JsonPropertyName("hj")]
    public float HueJitter { get; set; }
  }

  public class Rectangle : Element
  {
    [JsonPropertyName("l")]
    public float Left { get; set; }
    [JsonPropertyName("t")]
    public float Top { get; set; }
    [JsonPropertyName("r")]
    public float Right { get; set; }
    [JsonPropertyName("b")]
    public float Bottom { get; set; }
  }

  public class Ellipse : Element
  {
    [JsonPropertyName("l")]
    public float Left { get; set; }
    [JsonPropertyName("t")]
    public float Top { get; set; }
    [JsonPropertyName("r")]
    public float Right { get; set; }
    [JsonPropertyName("b")]
    public float Bottom { get; set; }
  }

  public class Line : Element
  {
    [JsonPropertyName("sx")]
    public float StartX { get; set; }
    [JsonPropertyName("sy")]
    public float StartY { get; set; }
    [JsonPropertyName("ex")]
    public float EndX { get; set; }
    [JsonPropertyName("ey")]
    public float EndY { get; set; }
  }
}