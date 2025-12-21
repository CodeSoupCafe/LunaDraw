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
using System.Linq;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Models;
using SkiaSharp;
using Xunit;

namespace LunaDraw.Tests
{
    public class HistoryMementoTests
    {
        [Fact]
        public void SaveState_AddsToHistory()
        {
            var manager = new HistoryMemento();
            var layers = new List<Layer> { new Layer { Name = "L1" } };

            manager.SaveState(layers);

            Assert.False(manager.CanUndo); // CanUndo checks index > 0. Index starts at -1. First save -> 0. So >0 is false.
            Assert.False(manager.CanRedo);
        }

        [Fact]
        public void SaveState_SecondTime_EnablesUndo()
        {
            var manager = new HistoryMemento();
            var layers1 = new List<Layer> { new Layer { Name = "L1" } };
            manager.SaveState(layers1);

            var layers2 = new List<Layer> { new Layer { Name = "L1_Mod" } };
            manager.SaveState(layers2);

            Assert.True(manager.CanUndo);
            Assert.False(manager.CanRedo);
        }

        [Fact]
        public void Undo_RestoresPreviousState()
        {
            var manager = new HistoryMemento();
            var layer1 = new Layer { Name = "L1" };
            layer1.Elements.Add(new TestDrawableElement { IsSelected = false });
            var layers1 = new List<Layer> { layer1 };

            manager.SaveState(layers1);

            // Modify state
            var layer2 = layer1.Clone();
            layer2.Elements.Add(new TestDrawableElement { IsSelected = true });
            var layers2 = new List<Layer> { layer2 };

            manager.SaveState(layers2);

            // Act
            var restoredLayers = manager.Undo();

            // Assert
            Assert.NotNull(restoredLayers);
            Assert.Single(restoredLayers);
            Assert.Equal("L1", restoredLayers[0].Name);
            Assert.Single(restoredLayers[0].Elements); // Should have 1 element like L1
            Assert.False(restoredLayers[0].Elements[0].IsSelected);

            Assert.False(manager.CanUndo);
            Assert.True(manager.CanRedo);
        }

        [Fact]
        public void Redo_RestoresNextState()
        {
            var manager = new HistoryMemento();
            var layers1 = new List<Layer> { new Layer { Name = "L1" } };
            manager.SaveState(layers1);

            var layers2 = new List<Layer> { new Layer { Name = "L2" } };
            manager.SaveState(layers2);

            manager.Undo();

            // Act
            var restoredLayers = manager.Redo();

            // Assert
            Assert.NotNull(restoredLayers);
            Assert.Equal("L2", restoredLayers[0].Name);
            Assert.True(manager.CanUndo);
            Assert.False(manager.CanRedo);
        }

        [Fact]
        public void SaveState_AfterUndo_ClearsRedoHistory()
        {
            var manager = new HistoryMemento();
            manager.SaveState(new List<Layer> { new Layer { Name = "1" } });
            manager.SaveState(new List<Layer> { new Layer { Name = "2" } });
            manager.Undo(); // Back to 1

            // Act - Save "3"
            manager.SaveState(new List<Layer> { new Layer { Name = "3" } });

            // Assert
            Assert.False(manager.CanRedo); // "2" should be gone

            manager.Undo(); // Back to 1
            var layers = manager.Undo(); // Should be null (start of history)
            Assert.Null(layers);
        }

        private class TestDrawableElement : IDrawableElement
        {
            public Guid Id { get; init; } = Guid.NewGuid();
            public SKRect Bounds => SKRect.Empty;
            public SKMatrix TransformMatrix { get; set; }
            public bool IsVisible { get; set; }
            public bool IsSelected { get; set; }
            public int ZIndex { get; set; }
            public byte Opacity { get; set; }
            public SKColor? FillColor { get; set; }
            public SKColor StrokeColor { get; set; }
            public float StrokeWidth { get; set; }
            public bool IsGlowEnabled { get; set; }
            public SKColor GlowColor { get; set; }
            public float GlowRadius { get; set; }

            public void Draw(SKCanvas canvas) { }
            public bool HitTest(SKPoint point) => false;
            public IDrawableElement Clone() => new TestDrawableElement { IsSelected = this.IsSelected };
            public void Translate(SKPoint offset) { }
            public void Transform(SKMatrix matrix) { }
            public SKPath GetPath() => new SKPath();
            public SKPath GetGeometryPath() => new SKPath();
        }
    }
}
