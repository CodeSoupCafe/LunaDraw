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

using LunaDraw.Logic.Drawing;
using LunaDraw.Logic.Models;
using Moq;
using Xunit;
using SkiaSharp;

namespace LunaDraw.Tests
{
    public class SelectionObserverTests
    {
        private readonly SelectionObserver selectionObserver;

        public SelectionObserverTests()
        {
            selectionObserver = new SelectionObserver();
        }

        [Fact]
        public void Constructor_ShouldInitializeEmptySelection()
        {
            // Arrange
            // Act
            Assert.Empty(selectionObserver.Selected);
            Assert.False(selectionObserver.HasSelection);
        }

        [Fact]
        public void Add_ShouldAddElementAndSetIsSelected()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            // Act
            selectionObserver.Add(mockElement.Object);

            Assert.Contains(mockElement.Object, selectionObserver.Selected);
            Assert.True(selectionObserver.HasSelection);
            Assert.True(mockElement.Object.IsSelected);
        }

        [Fact]
        public void Add_ShouldRaiseSelectionChangedEvent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            var eventRaised = false;
            selectionObserver.SelectionChanged += (sender, args) => eventRaised = true;

            // Act
            selectionObserver.Add(mockElement.Object);
            Assert.True(eventRaised);
        }

        [Fact]
        public void AddShouldNotAddDuplicateElements()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionObserver.Add(mockElement.Object); // Add once

            // Act
            selectionObserver.Add(mockElement.Object); // Try to add again

            // Assert
            Assert.Single(selectionObserver.Selected, x => x == mockElement.Object);
            Assert.Single(selectionObserver.Selected);
        }

        [Fact]
        public void RemoveShouldRemoveElement()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionObserver.Add(mockElement.Object); // Add element first

            // Act
            selectionObserver.Remove(mockElement.Object);

            // Assert
            Assert.Empty(selectionObserver.Selected);
        }

        [Fact]
        public void RemoveShouldSetIsSelectedFalse()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionObserver.Add(mockElement.Object); // Add element first

            // Act
            selectionObserver.Remove(mockElement.Object);

            // Assert
            Assert.False(mockElement.Object.IsSelected);
        }

        [Fact]
        public void Remove_ShouldRaiseSelectionChangedEvent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionObserver.Add(mockElement.Object); // Add element first
            var eventRaised = false;
            selectionObserver.SelectionChanged += (sender, args) => eventRaised = true; // Reset and listen again

            // Act
            selectionObserver.Remove(mockElement.Object);
            Assert.True(eventRaised);
        }

        [Fact]
        public void Remove_ShouldIgnoreNullOrNonExistentElement()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            IDrawableElement nonExistentElement = new Mock<IDrawableElement>().Object; // A different element
            selectionObserver.Add(mockElement.Object); // Add one element

            // Act
            selectionObserver.Remove(nonExistentElement);

            // Assert
            Assert.Single(selectionObserver.Selected, x => x == mockElement.Object); // Fixed
            Assert.True(selectionObserver.HasSelection);
        }

        [Fact]
        public void Clear_ShouldClearAllElements()
        {
            // Arrange
            var mockElement1 = new Mock<IDrawableElement>();
            var mockElement2 = new Mock<IDrawableElement>();
            mockElement1.SetupAllProperties();
            mockElement2.SetupAllProperties();
            selectionObserver.Add(mockElement1.Object);
            selectionObserver.Add(mockElement2.Object);

            // Act
            selectionObserver.Clear();

            // Assert
            Assert.Empty(selectionObserver.Selected);
            Assert.False(selectionObserver.HasSelection);
            Assert.False(mockElement1.Object.IsSelected);
            Assert.False(mockElement2.Object.IsSelected);
        }

        [Fact]
        public void Clear_ShouldRaiseSelectionChangedEventIfElementsWerePresent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionObserver.Add(mockElement.Object);
            var eventRaised = false;
            selectionObserver.SelectionChanged += (sender, args) => eventRaised = true; // Reset and listen

            // Act
            selectionObserver.Clear();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Clear_ShouldNotRaiseSelectionChangedEventIfAlreadyEmpty()
        {
            // Arrange
            var eventRaised = false;
            selectionObserver.SelectionChanged += (sender, args) => eventRaised = true;

            // Act
            selectionObserver.Clear();

            // Assert
            Assert.False(eventRaised);
        }

        [Fact]
        public void Toggle_ShouldAddElementIfNotSelected()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();

            // Act
            selectionObserver.Toggle(mockElement.Object);

            // Assert
            Assert.Single(selectionObserver.Selected, x => x == mockElement.Object);
            Assert.True(mockElement.Object.IsSelected);
        }

        [Fact]
        public void Toggle_ShouldRemoveElementIfSelected()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionObserver.Add(mockElement.Object); // Add it first

            // Act
            selectionObserver.Toggle(mockElement.Object);

            // Assert
            Assert.Empty(selectionObserver.Selected);
            Assert.False(mockElement.Object.IsSelected);
        }

        [Fact]
        public void Contains_ShouldReturnTrueForSelectedElement()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            selectionObserver.Add(mockElement.Object);

            // Act
            var result = selectionObserver.Contains(mockElement.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Contains_ShouldReturnFalseForNonSelectedElement()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            var nonSelectedElement = new Mock<IDrawableElement>().Object;

            // Act
            var result = selectionObserver.Contains(nonSelectedElement);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetAll_ShouldReturnAllSelectedElements()
        {
            // Arrange
            var mockElement1 = new Mock<IDrawableElement>().Object;
            var mockElement2 = new Mock<IDrawableElement>().Object;
            selectionObserver.Add(mockElement1);
            selectionObserver.Add(mockElement2);

            // Act
            var allElements = selectionObserver.GetAll();

            // Assert
            Assert.Equal(2, allElements.Count());
            Assert.Contains(mockElement1, allElements);
            Assert.Contains(mockElement2, allElements);
        }

        [Fact]
        public void GetBounds_ShouldReturnEmptyRectForEmptySelection()
        {
            // Act
            var bounds = selectionObserver.GetBounds();

            // Assert
            Assert.Equal(SKRect.Empty, bounds);
        }

        [Fact]
        public void GetBounds_ShouldReturnCorrectBoundsForSingleElement()
        {
            // Arrange
            var expectedBounds = new SKRect(0, 0, 100, 100);
            var mockElement = new Mock<IDrawableElement>();
            mockElement.Setup(e => e.Bounds).Returns(expectedBounds);
            selectionObserver.Add(mockElement.Object);

            // Act
            var bounds = selectionObserver.GetBounds();

            // Assert
            Assert.Equal(expectedBounds, bounds);
        }

        [Fact]
        public void GetBounds_ShouldReturnUnionOfBoundsForMultipleElements()
        {
            // Arrange
            var mockElement1 = new Mock<IDrawableElement>();
            mockElement1.Setup(e => e.Bounds).Returns(new SKRect(0, 0, 50, 50));
            var mockElement2 = new Mock<IDrawableElement>();
            mockElement2.Setup(e => e.Bounds).Returns(new SKRect(25, 25, 75, 75));
            selectionObserver.Add(mockElement1.Object);
            selectionObserver.Add(mockElement2.Object);

            // Act
            var bounds = selectionObserver.GetBounds();

            // Assert
            Assert.Equal(new SKRect(0, 0, 75, 75), bounds);
        }
    }
}
