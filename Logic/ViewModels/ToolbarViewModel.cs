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

using System.Reactive;
using System.Reactive.Linq;
using CommunityToolkit.Maui.Storage;

using LunaDraw.Logic.Models;
using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.ViewModels;

  public class ToolbarViewModel : ReactiveObject
  {
      private readonly ILayerFacade layerFacade;
      private readonly SelectionViewModel selectionVM;
      private readonly HistoryViewModel historyVM;
      private readonly IMessageBus messageBus;
      private readonly IBitmapCache bitmapCacheManager;
      private readonly NavigationModel navigationModel;
      private readonly IFileSaver fileSaver;

      // Tool State Properties
      private IDrawingTool activeTool;
      public virtual IDrawingTool ActiveTool
      {
          get => activeTool;
          set
          {
              this.RaiseAndSetIfChanged(ref activeTool, value);
              messageBus.SendMessage(new ToolChangedMessage(value));
          }
      }

      private SKColor strokeColor = SKColors.MediumPurple;
      public virtual SKColor StrokeColor
      {
          get => strokeColor;
          set => this.RaiseAndSetIfChanged(ref strokeColor, value);
      }

      private SKColor? fillColor = SKColors.SteelBlue;
      public virtual SKColor? FillColor
      {
          get => fillColor;
          set => this.RaiseAndSetIfChanged(ref fillColor, value);
      }

      private float strokeWidth = 40;
      public virtual float StrokeWidth
      {
          get => strokeWidth;
          set => this.RaiseAndSetIfChanged(ref strokeWidth, value);
      }

      private byte opacity = 255;
      public virtual byte Opacity
      {
          get => opacity;
          set => this.RaiseAndSetIfChanged(ref opacity, value);
      }

      private byte flow = 255;
      public virtual byte Flow
      {
          get => flow;
          set => this.RaiseAndSetIfChanged(ref flow, value);
      }

      private float spacing = 1f;
      public virtual float Spacing
      {
          get => spacing;
          set => this.RaiseAndSetIfChanged(ref spacing, value);
      }

      private BrushShape currentBrushShape;
      public virtual BrushShape CurrentBrushShape
      {
          get => currentBrushShape;
          set => this.RaiseAndSetIfChanged(ref currentBrushShape, value);
      }

      private bool isGlowEnabled = false;
      public virtual bool IsGlowEnabled
      {
          get => isGlowEnabled;
          set => this.RaiseAndSetIfChanged(ref isGlowEnabled, value);
      }

      private SKColor glowColor = SKColors.Yellow;
      public virtual SKColor GlowColor
      {
          get => glowColor;
          set => this.RaiseAndSetIfChanged(ref glowColor, value);
      }

      private float glowRadius = 10f;
      public virtual float GlowRadius
      {
          get => glowRadius;
          set => this.RaiseAndSetIfChanged(ref glowRadius, value);
      }

      private bool isRainbowEnabled;
      public virtual bool IsRainbowEnabled
      {
          get => isRainbowEnabled;
          set => this.RaiseAndSetIfChanged(ref isRainbowEnabled, value);
      }

      private float scatterRadius;
      public virtual float ScatterRadius
      {
          get => scatterRadius;
          set => this.RaiseAndSetIfChanged(ref scatterRadius, value);
      }

      private float sizeJitter;
      public virtual float SizeJitter
      {
          get => sizeJitter;
          set => this.RaiseAndSetIfChanged(ref sizeJitter, value);
      }

      private float angleJitter;
      public virtual float AngleJitter
      {
          get => angleJitter;
          set => this.RaiseAndSetIfChanged(ref angleJitter, value);
      }

      private float hueJitter;
      public virtual float HueJitter
      {
          get => hueJitter;
          set => this.RaiseAndSetIfChanged(ref hueJitter, value);
      }

      public List<IDrawingTool> AvailableTools { get; }
      public List<BrushShape> AvailableBrushShapes { get; }

      // Delegated Commands
      public ReactiveCommand<IDrawingTool, Unit> SelectToolCommand { get; }
      public ReactiveCommand<Unit, Unit> UndoCommand => historyVM.UndoCommand;
      public ReactiveCommand<Unit, Unit> RedoCommand => historyVM.RedoCommand;
      public ReactiveCommand<Unit, Unit> CopyCommand => selectionVM.CopyCommand;
      public ReactiveCommand<Unit, Unit> PasteCommand => selectionVM.PasteCommand;
      public ReactiveCommand<Unit, Unit> DeleteSelectedCommand => selectionVM.DeleteSelectedCommand;
      public ReactiveCommand<Unit, Unit> GroupSelectedCommand => selectionVM.GroupSelectedCommand;
      public ReactiveCommand<Unit, Unit> UngroupSelectedCommand => selectionVM.UngroupSelectedCommand;

      // Local Commands
      public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
      public ReactiveCommand<Unit, Unit> ShowShapesFlyoutCommand { get; }
      public ReactiveCommand<Unit, Unit> SelectRectangleCommand { get; }
      public ReactiveCommand<Unit, Unit> SelectCircleCommand { get; }
      public ReactiveCommand<Unit, Unit> SelectLineCommand { get; }
      public ReactiveCommand<Unit, Unit> ShowBrushesFlyoutCommand { get; }
      public ReactiveCommand<BrushShape, Unit> SelectBrushShapeCommand { get; }
      public ReactiveCommand<Unit, Unit> ImportImageCommand { get; }
      public ReactiveCommand<Unit, Unit> SaveImageCommand { get; }

      // UI state properties
      private bool isSettingsOpen = false;
      public bool IsSettingsOpen
      {
          get => isSettingsOpen;
          set => this.RaiseAndSetIfChanged(ref isSettingsOpen, value);
      }

      private bool isShapesFlyoutOpen = false;
      public bool IsShapesFlyoutOpen
      {
          get => isShapesFlyoutOpen;
          set => this.RaiseAndSetIfChanged(ref isShapesFlyoutOpen, value);
      }

      private bool isBrushesFlyoutOpen = false;
      public bool IsBrushesFlyoutOpen
      {
          get => isBrushesFlyoutOpen;
          set => this.RaiseAndSetIfChanged(ref isBrushesFlyoutOpen, value);
      }

      private readonly ObservableAsPropertyHelper<bool> isAnyFlyoutOpen;
      public bool IsAnyFlyoutOpen => isAnyFlyoutOpen.Value;

      private IDrawingTool lastActiveShapeTool;
      public IDrawingTool LastActiveShapeTool
      {
          get => lastActiveShapeTool;
          set => this.RaiseAndSetIfChanged(ref lastActiveShapeTool, value);
      }

      public ToolbarViewModel(
          ILayerFacade layerFacade,
          SelectionViewModel selectionVM,
          HistoryViewModel historyVM,
          IMessageBus messageBus,
          IBitmapCache bitmapCacheManager,
          NavigationModel navigationModel,
          IFileSaver fileSaver)
      {
          this.layerFacade = layerFacade;
          this.selectionVM = selectionVM;
          this.historyVM = historyVM;
          this.messageBus = messageBus;
          this.bitmapCacheManager = bitmapCacheManager;
          this.navigationModel = navigationModel;
          this.fileSaver = fileSaver;

          // Initialize Tools and Shapes
          AvailableTools =
          [
              new SelectTool(messageBus),
              new FreehandTool(messageBus),
              new RectangleTool(messageBus),
              new EllipseTool(messageBus),
              new LineTool(messageBus),
              new FillTool(messageBus),
              new EraserBrushTool(messageBus)
          ];

          AvailableBrushShapes =
          [
              BrushShape.Circle(),
              BrushShape.Square(),
              BrushShape.Star(),
              BrushShape.Heart(),
              BrushShape.Sparkle(),
              BrushShape.Cloud(),
              BrushShape.Moon(),
              BrushShape.Lightning(),
              BrushShape.Diamond(),
              BrushShape.Triangle(),
              BrushShape.Hexagon()
          ];

          activeTool = new FreehandTool(messageBus);
          currentBrushShape = AvailableBrushShapes.First();

          // Initialize commands
          SelectToolCommand = ReactiveCommand.Create<IDrawingTool>(tool =>
          {
              ActiveTool = tool;
          }, outputScheduler: RxApp.MainThreadScheduler);

          isAnyFlyoutOpen = this.WhenAnyValue(x => x.IsSettingsOpen, x => x.IsShapesFlyoutOpen, x => x.IsBrushesFlyoutOpen)
            .Select(values => values.Item1 || values.Item2 || values.Item3)
            .ToProperty(this, x => x.IsAnyFlyoutOpen);

          // Reactive Logic: Close flyouts when ActiveTool changes
          this.WhenAnyValue(x => x.ActiveTool)
              .Skip(1) // Don't trigger on initialization
              .Subscribe(_ =>
              {
                  IsBrushesFlyoutOpen = false;
                  IsShapesFlyoutOpen = false;
                  IsSettingsOpen = false;
              });

          // Listen for messages that update tool state
          this.messageBus.Listen<BrushSettingsChangedMessage>().Subscribe(msg =>
          {
              if (msg.StrokeColor.HasValue) StrokeColor = msg.StrokeColor.Value;
              if (msg.ShouldClearFillColor) FillColor = null;
              else if (msg.FillColor.HasValue) FillColor = msg.FillColor.Value;
              if (msg.Transparency.HasValue) Opacity = msg.Transparency.Value;
              if (msg.Flow.HasValue) Flow = msg.Flow.Value;
              if (msg.Spacing.HasValue) Spacing = msg.Spacing.Value;
              if (msg.StrokeWidth.HasValue) StrokeWidth = msg.StrokeWidth.Value;
              if (msg.IsGlowEnabled.HasValue) IsGlowEnabled = msg.IsGlowEnabled.Value;
              if (msg.GlowColor.HasValue) GlowColor = msg.GlowColor.Value;
              if (msg.GlowRadius.HasValue) GlowRadius = msg.GlowRadius.Value;
              if (msg.IsRainbowEnabled.HasValue) IsRainbowEnabled = msg.IsRainbowEnabled.Value;
              if (msg.ScatterRadius.HasValue) ScatterRadius = msg.ScatterRadius.Value;
              if (msg.SizeJitter.HasValue) SizeJitter = msg.SizeJitter.Value;
              if (msg.AngleJitter.HasValue) AngleJitter = msg.AngleJitter.Value;
              if (msg.HueJitter.HasValue) HueJitter = msg.HueJitter.Value;
          });

          this.messageBus.Listen<BrushShapeChangedMessage>().Subscribe(msg =>
          {
              CurrentBrushShape = msg.Shape;
          });

          lastActiveShapeTool = AvailableTools.FirstOrDefault(t => t is RectangleTool)
                                 ?? AvailableTools.FirstOrDefault(t => t is EllipseTool)
                                 ?? AvailableTools.FirstOrDefault(t => t is LineTool)
                                 ?? new RectangleTool(messageBus);

          ShowShapesFlyoutCommand = ReactiveCommand.Create(() =>
          {
              IsSettingsOpen = false;
              IsBrushesFlyoutOpen = false;

              if (ActiveTool == LastActiveShapeTool)
              {
                  IsShapesFlyoutOpen = !IsShapesFlyoutOpen;
              }
              else
              {
                  SelectToolCommand.Execute(LastActiveShapeTool).Subscribe();
                  IsShapesFlyoutOpen = false;
              }
          });

          ShowBrushesFlyoutCommand = ReactiveCommand.Create(() =>
          {
              IsSettingsOpen = false;
              IsShapesFlyoutOpen = false;

              var freehandTool = AvailableTools.FirstOrDefault(t => t.Type == ToolType.Freehand);

              if (ActiveTool == freehandTool)
              {
                  IsBrushesFlyoutOpen = !IsBrushesFlyoutOpen;
              }
              else
              {
                  if (freehandTool != null)
                      SelectToolCommand.Execute(freehandTool).Subscribe();
                  IsBrushesFlyoutOpen = false;
              }
          });

          SelectBrushShapeCommand = ReactiveCommand.Create<BrushShape>(shape =>
          {
              this.messageBus.SendMessage(new LunaDraw.Logic.Messages.BrushShapeChangedMessage(shape));
              IsBrushesFlyoutOpen = false;

              var freehandTool = AvailableTools.FirstOrDefault(t => t.Type == ToolType.Freehand);
              if (freehandTool != null && ActiveTool != freehandTool)
              {
                  SelectToolCommand.Execute(freehandTool).Subscribe();
              }
          });

          ShowSettingsCommand = ReactiveCommand.Create(() =>
          {
              IsSettingsOpen = !IsSettingsOpen;
              IsShapesFlyoutOpen = false;
              IsBrushesFlyoutOpen = false;
          });

          SelectRectangleCommand = ReactiveCommand.Create(() =>
          {
              var tool = AvailableTools.FirstOrDefault(t => t is RectangleTool) ?? new RectangleTool(messageBus);
              LastActiveShapeTool = tool;
              SelectToolCommand.Execute(tool).Subscribe();
              IsShapesFlyoutOpen = false;
          });

          SelectCircleCommand = ReactiveCommand.Create(() =>
          {
              var tool = AvailableTools.FirstOrDefault(t => t is EllipseTool) ?? new EllipseTool(messageBus);
              LastActiveShapeTool = tool;
              SelectToolCommand.Execute(tool).Subscribe();
              IsShapesFlyoutOpen = false;
          });

          SelectLineCommand = ReactiveCommand.Create(() =>
          {
              var tool = AvailableTools.FirstOrDefault(t => t is LineTool) ?? new LineTool(messageBus);
              LastActiveShapeTool = tool;
              SelectToolCommand.Execute(tool).Subscribe();
              IsShapesFlyoutOpen = false;
          });

          ImportImageCommand = ReactiveCommand.CreateFromTask(async () =>
          {
              try
              {
                  var result = await FilePicker.Default.PickAsync(new PickOptions
                  {
                      PickerTitle = "Select an image to import",
                      FileTypes = FilePickerFileType.Images
                  });

                  if (result != null)
                  {
                      string path = result.FullPath;

                      // On platforms where FullPath is not available, copy to cache
                      if (string.IsNullOrEmpty(path))
                      {
                          path = Path.Combine(FileSystem.CacheDirectory, result.FileName);
                          using var sourceStream = await result.OpenReadAsync();
                          using var destStream = File.Create(path);
                          await sourceStream.CopyToAsync(destStream);
                      }

                      // Load with downsampling (max 2048x2048)
                      var bitmap = await this.bitmapCacheManager.GetBitmapAsync(path, 2048, 2048);

                      if (bitmap != null)
                      {
                          var drawableImage = new DrawableImage(bitmap)
                          {
                              SourcePath = path
                          };

                          this.layerFacade.CurrentLayer?.Elements.Add(drawableImage);
                          this.messageBus.SendMessage(new CanvasInvalidateMessage());
                          this.layerFacade.SaveState();
                      }
                  }
              }
              catch (Exception ex)
              {
                  System.Diagnostics.Debug.WriteLine($"Error importing image: {ex.Message}");
              }
          });

          SaveImageCommand = ReactiveCommand.CreateFromTask(async () =>
          {
              try
              {
                  if (this.navigationModel.CanvasWidth <= 0 || this.navigationModel.CanvasHeight <= 0)
                      return;

                  using var surface = SKSurface.Create(new SKImageInfo((int)this.navigationModel.CanvasWidth, (int)this.navigationModel.CanvasHeight));
                  var canvas = surface.Canvas;
                  canvas.Clear(SKColors.White);

                  canvas.Save();

                  // Apply the view transformation matrix
                  canvas.SetMatrix(this.navigationModel.ViewMatrix);

                  // Draw layers with masking support
                  var layers = this.layerFacade.Layers;
                  for (int i = 0; i < layers.Count; i++)
                  {
                      var layer = layers[i];
                      if (!layer.IsVisible) continue;

                      if (layer.MaskingMode == Logic.Models.MaskingMode.Clip)
                      {
                          layer.Draw(canvas);
                      }
                      else
                      {
                          // Check if next layers are clipping layers
                          bool hasClippingLayers = false;
                          int nextIndex = i + 1;
                          while (nextIndex < layers.Count && layers[nextIndex].MaskingMode == Logic.Models.MaskingMode.Clip)
                          {
                              if (layers[nextIndex].IsVisible) hasClippingLayers = true;
                              nextIndex++;
                          }

                          if (hasClippingLayers)
                          {
                              canvas.SaveLayer();
                              layer.Draw(canvas);

                              using (var paint = new SKPaint { BlendMode = SKBlendMode.SrcATop })
                              {
                                  for (int j = i + 1; j < layers.Count; j++)
                                  {
                                      var clipLayer = layers[j];
                                      if (clipLayer.MaskingMode != Logic.Models.MaskingMode.Clip) break;

                                      if (clipLayer.IsVisible)
                                      {
                                          canvas.SaveLayer(paint);
                                          clipLayer.Draw(canvas);
                                          canvas.Restore();
                                      }

                                      i = j;
                                  }
                              }

                              canvas.Restore();
                          }
                          else
                          {
                              layer.Draw(canvas);
                          }
                      }
                  }

                  canvas.Restore();

                  using var image = surface.Snapshot();
                  using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                  using var stream = data.AsStream();

                  var result = await this.fileSaver.SaveAsync("lunadraw_canvas.png", stream);
              }
              catch (Exception ex)
              {
                  System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
              }
          });
      }
  }