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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Drawing;
using Moq;
using SkiaSharp;
using Xunit;

namespace LunaDraw.Tests.Features.MovieMode;

public class SerializationTests : IDisposable
{
  private readonly DrawingStorageMomento storage;
  private readonly string tempPath;

  public SerializationTests()
  {
    tempPath = Path.Combine(Path.GetTempPath(), "LunaDrawTests_" + Guid.NewGuid());
    Directory.CreateDirectory(tempPath);
    storage = new DrawingStorageMomento(tempPath);
  }

  [Fact]
  public void CreateExternalDrawing_Should_Persist_CreatedAt_Timestamp()
  {
    // Arrange
    var createdAt = DateTimeOffset.Now.AddMinutes(-5);
    var layer = new Layer { Id = Guid.NewGuid(), Name = "TestLayer" };
    var element = new DrawablePath
    {
      Id = Guid.NewGuid(),
      CreatedAt = createdAt,
      Path = new SKPath()
    };
    layer.Elements.Add(element);
    var layers = new List<Layer> { layer };

    // Act
    var externalDrawing = storage.CreateExternalDrawingFromCurrent(layers, 100, 100, "TestDrawing", Guid.NewGuid());
    var restoredLayers = storage.RestoreLayers(externalDrawing);

    // Assert
    restoredLayers.Should().ContainSingle();
    restoredLayers[0].Elements.Should().ContainSingle();
    restoredLayers[0].Elements[0].CreatedAt.Should().Be(createdAt);
  }

  public void Dispose()
  {
    if (Directory.Exists(tempPath))
    {
      Directory.Delete(tempPath, true);
    }
  }
}