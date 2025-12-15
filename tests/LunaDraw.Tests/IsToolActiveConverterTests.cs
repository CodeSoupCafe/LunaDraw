using System.Globalization;
using LunaDraw.Converters;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Tools;
using Moq;
using ReactiveUI;
using Xunit;

namespace LunaDraw.Tests
{
    public class IsToolActiveConverterTests
    {
        [Fact]
        public void Convert_ReturnsTrue_WhenToolMatches()
        {
            var converter = new IsToolActiveConverter();
            var mockTool = new Mock<IDrawingTool>();
            var mockBus = new Mock<IMessageBus>();
            // We can't easily mock GetType().Name without a real class, so let's use a real one or subclass.
            var tool = new SelectTool(mockBus.Object); 

            var result = converter.Convert(tool, typeof(bool), "SelectTool", CultureInfo.InvariantCulture);

            Assert.True(result as bool?);
        }

        [Fact]
        public void Convert_ReturnsFalse_WhenToolDoesNotMatch()
        {
            var converter = new IsToolActiveConverter();
            var mockBus = new Mock<IMessageBus>();
            var tool = new SelectTool(mockBus.Object);

            var result = converter.Convert(tool, typeof(bool), "FreehandTool", CultureInfo.InvariantCulture);

            Assert.False(result as bool?);
        }

        [Fact]
        public void Convert_ReturnsTrue_ForShapesGroup()
        {
            var converter = new IsToolActiveConverter();
            var mockBus = new Mock<IMessageBus>();
            var tool = new RectangleTool(mockBus.Object);

            var result = converter.Convert(tool, typeof(bool), "Shapes", CultureInfo.InvariantCulture);

            Assert.True(result as bool?);
        }

        [Fact]
        public void Convert_ReturnsFalse_ForShapesGroup_WhenNotShape()
        {
            var converter = new IsToolActiveConverter();
            var mockBus = new Mock<IMessageBus>();
            var tool = new SelectTool(mockBus.Object);

            var result = converter.Convert(tool, typeof(bool), "Shapes", CultureInfo.InvariantCulture);

            Assert.False(result as bool?);
        }
    }
}
