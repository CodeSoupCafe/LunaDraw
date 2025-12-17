using System.Text.Json.Serialization;

namespace LunaDraw.Logic.Models;

public class External
{
  public class Drawing
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
  public class Element
  {
    [JsonPropertyName("i")]
    public Guid Id { get; set; }
    [JsonPropertyName("v")]
    public bool IsVisible { get; set; }
    [JsonPropertyName("z")]
    public int ZIndex { get; set; }
    [JsonPropertyName("o")]
    public byte Opacity { get; set; }
    [JsonPropertyName("fc")]
    public string? FillColor { get; set; }
    [JsonPropertyName("sc")]
    public string StrokeColor { get; set; }
    [JsonPropertyName("sw")]
    public float StrokeWidth { get; set; }
    [JsonPropertyName("ge")]
    public bool IsGlowEnabled { get; set; }
    [JsonPropertyName("gc")]
    public string GlowColor { get; set; }
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
}