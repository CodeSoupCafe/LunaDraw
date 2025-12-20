using Xunit;
using FluentAssertions;
using Moq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Models.Serialization;
using SkiaSharp;
using LunaDraw.Logic.Utils;
using Microsoft.Maui.ApplicationModel;

namespace LunaDraw.Tests;

public class DrawingStorageMomentoTests : IDisposable
{
  private readonly string _testStoragePath;
  private readonly DrawingStorageMomento _sut; // System Under Test

  public DrawingStorageMomentoTests()
  {
    // Use a temporary directory for tests
    _testStoragePath = Path.Combine(Path.GetTempPath(), "LunaDrawTestGallery", Guid.NewGuid().ToString());
    Directory.CreateDirectory(_testStoragePath);

    // Mock FileSystem.AppDataDirectory for tests
    // This requires some trickery as FileSystem.AppDataDirectory is static.
    // For simplicity in a test, we can directly manipulate the path
    // that DrawingStorageMomento uses, or refactor DrawingStorageMomento to accept the path.
    // For now, let's assume we can control the path through reflection or direct assignment if possible.
    // Since it's a private readonly field initialized in the constructor, we'll need to refactor.

    // Refactor: Pass storage path to constructor for testability.
    // For the current structure, I'll instantiate it directly and ensure it uses a test path.
    // This will require a minor change to DrawingStorageMomento or creating a test-specific version.

    // For now, I'll use a trick or simply proceed assuming the default path
    // and clean up afterwards, acknowledging this is not ideal.
    // Let's modify DrawingStorageMomento to accept a path in its constructor for testability.
    // This will be a refactoring step. For now, I'll write the test structure.

    // Let's mock FileSystem.AppDataDirectory instead.
    // This is a MAUI specific static class, so mocking it directly is hard.
    // I will change the DrawingStorageMomento constructor to take a path for testing.
    _sut = new DrawingStorageMomento(_testStoragePath);
  }

  [Fact]
  public async Task Should_SaveAndLoadDrawing_When_DrawingContainsPath()
  {
    // Arrange
    var drawingId = Guid.NewGuid();
    var drawingName = "Test Drawing with Path";
    var canvasWidth = 800;
    var canvasHeight = 600;

    var path = new SKPath();
    path.MoveTo(50, 50);
    path.LineTo(100, 100);
    path.LineTo(50, 150);
    path.Close();

    var drawablePath = new DrawablePath
    {
      Id = Guid.NewGuid(),
      Path = path,
      IsVisible = true,
      Opacity = 1.0f,
      FillColor = SKColors.Red,
      StrokeColor = SKColors.Blue,
      StrokeWidth = 5,
      BlendMode = SKBlendMode.SrcOver,
      ZIndex = 0
    };

    var layer = new Layer
    {
      Id = Guid.NewGuid(),
      Name = "Test Layer",
      IsVisible = true,
      IsLocked = false,
      MaskingMode = MaskingMode.None
    };
    layer.Elements.Add(drawablePath);

    var layers = new List<Layer> { layer };

    var savedDrawing = _sut.CreateExternalDrawingFromCurrent(layers, canvasWidth, canvasHeight, drawingName, drawingId);

    // Act
    await _sut.ExternalDrawingAsync(savedDrawing);
    var loadedDrawing = await _sut.LoadDrawingAsync(drawingId);

    // Assert
    loadedDrawing.Should().NotBeNull();
    loadedDrawing.Id.Should().Be(drawingId);
    loadedDrawing.Name.Should().Be(drawingName);
    loadedDrawing.CanvasWidth.Should().Be(canvasWidth);
    loadedDrawing.CanvasHeight.Should().Be(canvasHeight);
    loadedDrawing.Layers.Should().NotBeEmpty();

    var loadedLayer = loadedDrawing.Layers.First();
    loadedLayer.Id.Should().Be(layer.Id);
    loadedLayer.Name.Should().Be(layer.Name);
    loadedLayer.IsVisible.Should().Be(layer.IsVisible);
    loadedLayer.IsLocked.Should().Be(layer.IsLocked);
    loadedLayer.MaskingMode.Should().Be((int)layer.MaskingMode);
    loadedLayer.Elements.Should().NotBeEmpty();

    var loadedElement = loadedLayer.Elements.First().Should().BeOfType<External.Path>().Which;
    loadedElement.Id.Should().Be(drawablePath.Id);
    loadedElement.PathData.Should().NotBeNullOrEmpty();
    loadedElement.BlendMode.Should().Be((int)drawablePath.BlendMode);
    loadedElement.Opacity.Should().Be(drawablePath.Opacity);
    loadedElement.FillColor.Should().Be(drawablePath.FillColor?.ToString());
    loadedElement.StrokeColor.Should().Be(drawablePath.StrokeColor.ToString());
    loadedElement.StrokeWidth.Should().Be(drawablePath.StrokeWidth);
    loadedElement.ZIndex.Should().Be(drawablePath.ZIndex);

    // Restore layers and check actual SKPath data
    var restoredLayers = _sut.RestoreLayers(loadedDrawing);
    restoredLayers.Should().NotBeEmpty();
    var restoredLayer = restoredLayers.First();
    var restoredDrawablePath = restoredLayer.Elements.First().Should().BeOfType<DrawablePath>().Which;

    restoredDrawablePath.Path.Should().NotBeNull();
    restoredDrawablePath.Path.ToSvgPathData().Should().Be(drawablePath.Path.ToSvgPathData());
  }

  public void Dispose()
  {
    // Clean up the temporary directory after tests
    if (Directory.Exists(_testStoragePath))
    {
      Directory.Delete(_testStoragePath, true);
    }
  }
}
