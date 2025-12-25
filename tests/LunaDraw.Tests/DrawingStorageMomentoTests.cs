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

using Xunit;
using FluentAssertions;
using Moq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LunaDraw.Logic.Models;
using SkiaSharp;
using LunaDraw.Logic.Utils;
using Microsoft.Maui.ApplicationModel;

namespace LunaDraw.Tests;

public class DrawingStorageMomentoTests : IDisposable
{
  private readonly string testStoragePath;
  private readonly DrawingStorageMomento sut; // System Under Test

  public DrawingStorageMomentoTests()
  {
    // Use a temporary directory for tests
    testStoragePath = Path.Combine(Path.GetTempPath(), "LunaDrawTestGallery", Guid.NewGuid().ToString());
    Directory.CreateDirectory(testStoragePath);

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
    sut = new DrawingStorageMomento(testStoragePath);
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
      Opacity = 255,
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

    var savedDrawing = sut.CreateExternalDrawingFromCurrent(layers, canvasWidth, canvasHeight, drawingName, drawingId);

    // Act
    await sut.ExternalDrawingAsync(savedDrawing);
    var loadedDrawing = await sut.LoadDrawingAsync(drawingId);

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
    var restoredLayers = sut.RestoreLayers(loadedDrawing);
    restoredLayers.Should().NotBeEmpty();
    var restoredLayer = restoredLayers.First();
    var restoredDrawablePath = restoredLayer.Elements.First().Should().BeOfType<DrawablePath>().Which;

    restoredDrawablePath.Path.Should().NotBeNull();
    restoredDrawablePath.Path.ToSvgPathData().Should().Be(drawablePath.Path.ToSvgPathData());
  }

  [Fact]
  public async Task Should_PreserveRectangleType_When_SerializingAndDeserializing()
  {
    // Arrange
    var drawingId = Guid.NewGuid();
    var rectangle = new SKRect(10, 20, 100, 80);

    var drawableRectangle = new DrawableRectangle
    {
      Id = Guid.NewGuid(),
      Rectangle = rectangle,
      IsVisible = true,
      Opacity = 255,
      FillColor = SKColors.Green,
      StrokeColor = SKColors.Black,
      StrokeWidth = 3,
      ZIndex = 0
    };

    var layer = new Layer
    {
      Id = Guid.NewGuid(),
      Name = "Rectangle Layer",
      IsVisible = true,
      IsLocked = false,
      MaskingMode = MaskingMode.None
    };
    layer.Elements.Add(drawableRectangle);

    var savedDrawing = sut.CreateExternalDrawingFromCurrent(new List<Layer> { layer }, 800, 600, "Rectangle Test", drawingId);

    // Act
    await sut.ExternalDrawingAsync(savedDrawing);
    var loadedDrawing = await sut.LoadDrawingAsync(drawingId);

    // Assert
    loadedDrawing.Should().NotBeNull();
    var loadedElement = loadedDrawing.Layers.First().Elements.First();
    loadedElement.Should().BeOfType<External.Rectangle>();

    var externalRectangle = (External.Rectangle)loadedElement;
    externalRectangle.Left.Should().Be(rectangle.Left);
    externalRectangle.Top.Should().Be(rectangle.Top);
    externalRectangle.Right.Should().Be(rectangle.Right);
    externalRectangle.Bottom.Should().Be(rectangle.Bottom);

    // Restore and verify it becomes DrawableRectangle
    var restoredLayers = sut.RestoreLayers(loadedDrawing);
    var restoredElement = restoredLayers.First().Elements.First();
    restoredElement.Should().BeOfType<DrawableRectangle>();

    var restoredRectangle = (DrawableRectangle)restoredElement;
    restoredRectangle.Rectangle.Should().Be(rectangle);
    restoredRectangle.AnimationProgress.Should().Be(1.0f);
  }

  [Fact]
  public async Task Should_PreserveEllipseType_When_SerializingAndDeserializing()
  {
    // Arrange
    var drawingId = Guid.NewGuid();
    var oval = new SKRect(20, 30, 120, 90);

    var drawableEllipse = new DrawableEllipse
    {
      Id = Guid.NewGuid(),
      Oval = oval,
      IsVisible = true,
      Opacity = 200,
      FillColor = SKColors.Yellow,
      StrokeColor = SKColors.Red,
      StrokeWidth = 2,
      ZIndex = 1
    };

    var layer = new Layer
    {
      Id = Guid.NewGuid(),
      Name = "Ellipse Layer",
      IsVisible = true,
      IsLocked = false,
      MaskingMode = MaskingMode.None
    };
    layer.Elements.Add(drawableEllipse);

    var savedDrawing = sut.CreateExternalDrawingFromCurrent(new List<Layer> { layer }, 800, 600, "Ellipse Test", drawingId);

    // Act
    await sut.ExternalDrawingAsync(savedDrawing);
    var loadedDrawing = await sut.LoadDrawingAsync(drawingId);

    // Assert
    loadedDrawing.Should().NotBeNull();
    var loadedElement = loadedDrawing.Layers.First().Elements.First();
    loadedElement.Should().BeOfType<External.Ellipse>();

    var externalEllipse = (External.Ellipse)loadedElement;
    externalEllipse.Left.Should().Be(oval.Left);
    externalEllipse.Top.Should().Be(oval.Top);
    externalEllipse.Right.Should().Be(oval.Right);
    externalEllipse.Bottom.Should().Be(oval.Bottom);

    // Restore and verify it becomes DrawableEllipse
    var restoredLayers = sut.RestoreLayers(loadedDrawing);
    var restoredElement = restoredLayers.First().Elements.First();
    restoredElement.Should().BeOfType<DrawableEllipse>();

    var restoredEllipse = (DrawableEllipse)restoredElement;
    restoredEllipse.Oval.Should().Be(oval);
    restoredEllipse.AnimationProgress.Should().Be(1.0f);
  }

  [Fact]
  public async Task Should_PreserveLineType_When_SerializingAndDeserializing()
  {
    // Arrange
    var drawingId = Guid.NewGuid();
    var startPoint = new SKPoint(10, 20);
    var endPoint = new SKPoint(100, 120);

    var drawableLine = new DrawableLine
    {
      Id = Guid.NewGuid(),
      StartPoint = startPoint,
      EndPoint = endPoint,
      IsVisible = true,
      Opacity = 255,
      StrokeColor = SKColors.Blue,
      StrokeWidth = 5,
      ZIndex = 2
    };

    var layer = new Layer
    {
      Id = Guid.NewGuid(),
      Name = "Line Layer",
      IsVisible = true,
      IsLocked = false,
      MaskingMode = MaskingMode.None
    };
    layer.Elements.Add(drawableLine);

    var savedDrawing = sut.CreateExternalDrawingFromCurrent(new List<Layer> { layer }, 800, 600, "Line Test", drawingId);

    // Act
    await sut.ExternalDrawingAsync(savedDrawing);
    var loadedDrawing = await sut.LoadDrawingAsync(drawingId);

    // Assert
    loadedDrawing.Should().NotBeNull();
    var loadedElement = loadedDrawing.Layers.First().Elements.First();
    loadedElement.Should().BeOfType<External.Line>();

    var externalLine = (External.Line)loadedElement;
    externalLine.StartX.Should().Be(startPoint.X);
    externalLine.StartY.Should().Be(startPoint.Y);
    externalLine.EndX.Should().Be(endPoint.X);
    externalLine.EndY.Should().Be(endPoint.Y);

    // Restore and verify it becomes DrawableLine
    var restoredLayers = sut.RestoreLayers(loadedDrawing);
    var restoredElement = restoredLayers.First().Elements.First();
    restoredElement.Should().BeOfType<DrawableLine>();

    var restoredLine = (DrawableLine)restoredElement;
    restoredLine.StartPoint.Should().Be(startPoint);
    restoredLine.EndPoint.Should().Be(endPoint);
    restoredLine.AnimationProgress.Should().Be(1.0f);
  }

  public void Dispose()
  {
    // Clean up the temporary directory after tests
    if (Directory.Exists(testStoragePath))
    {
      Directory.Delete(testStoragePath, true);
    }
  }
}
