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
