using System.Globalization;
using LunaDraw.Converters;
using LunaDraw.Logic.Tools;
using Moq;
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
            // We can't easily mock GetType().Name without a real class, so let's use a real one or subclass.
            var tool = new SelectTool(null); 

            var result = converter.Convert(tool, typeof(bool), "SelectTool", CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        [Fact]
        public void Convert_ReturnsFalse_WhenToolDoesNotMatch()
        {
            var converter = new IsToolActiveConverter();
            var tool = new SelectTool(null);

            var result = converter.Convert(tool, typeof(bool), "FreehandTool", CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        [Fact]
        public void Convert_ReturnsTrue_ForShapesGroup()
        {
            var converter = new IsToolActiveConverter();
            var tool = new RectangleTool(null);

            var result = converter.Convert(tool, typeof(bool), "Shapes", CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        [Fact]
        public void Convert_ReturnsFalse_ForShapesGroup_WhenNotShape()
        {
            var converter = new IsToolActiveConverter();
            var tool = new SelectTool(null);

            var result = converter.Convert(tool, typeof(bool), "Shapes", CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }
    }
}
