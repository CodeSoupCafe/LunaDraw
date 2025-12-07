using System;
using System.Collections.Generic;
using System.Linq;

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Models;
using Moq;
using Xunit;
using SkiaSharp;

namespace LunaDraw.Tests
{
    public class SelectionManagerTests
    {
        private readonly SelectionManager selectionManager;

        public SelectionManagerTests()
        {
            selectionManager = new SelectionManager();
        }

        [Fact]
        public void Constructor_ShouldInitializeEmptySelection()
        {
            // Arrange
            // Act
            Assert.Empty(selectionManager.Selected);
            Assert.False(selectionManager.HasSelection);
        }

        [Fact]
        public void Add_ShouldAddElementAndSetIsSelected()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            // Act
            selectionManager.Add(mockElement.Object);

            Assert.Contains(mockElement.Object, selectionManager.Selected);
            Assert.True(selectionManager.HasSelection);
            Assert.True(mockElement.Object.IsSelected);
        }

        [Fact]
        public void Add_ShouldRaiseSelectionChangedEvent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            var eventRaised = false;
            selectionManager.SelectionChanged += (sender, args) => eventRaised = true;

            // Act
            selectionManager.Add(mockElement.Object);
            Assert.True(eventRaised);
        }

        [Fact]
        public void AddShouldNotAddDuplicateElements()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionManager.Add(mockElement.Object); // Add once

            // Act
            selectionManager.Add(mockElement.Object); // Try to add again

            // Assert
            Assert.Single(selectionManager.Selected, x => x == mockElement.Object);
            Assert.Single(selectionManager.Selected);
        }

        [Fact]
        public void RemoveShouldRemoveElement()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionManager.Add(mockElement.Object); // Add element first

            // Act
            selectionManager.Remove(mockElement.Object);

            // Assert
            Assert.Empty(selectionManager.Selected);
        }

        [Fact]
        public void RemoveShouldSetIsSelectedFalse()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionManager.Add(mockElement.Object); // Add element first

            // Act
            selectionManager.Remove(mockElement.Object);

            // Assert
            Assert.False(mockElement.Object.IsSelected);
        }

        [Fact]
        public void Remove_ShouldRaiseSelectionChangedEvent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionManager.Add(mockElement.Object); // Add element first
            var eventRaised = false;
            selectionManager.SelectionChanged += (sender, args) => eventRaised = true; // Reset and listen again

            // Act
            selectionManager.Remove(mockElement.Object);
            Assert.True(eventRaised);
        }

        [Fact]
        public void Remove_ShouldIgnoreNullOrNonExistentElement()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            IDrawableElement nonExistentElement = new Mock<IDrawableElement>().Object; // A different element
            selectionManager.Add(mockElement.Object); // Add one element

            // Act
            selectionManager.Remove(nonExistentElement);

            // Assert
            Assert.Single(selectionManager.Selected, x => x == mockElement.Object); // Fixed
            Assert.True(selectionManager.HasSelection);
        }

        [Fact]
        public void Clear_ShouldClearAllElements()
        {
            // Arrange
            var mockElement1 = new Mock<IDrawableElement>();
            var mockElement2 = new Mock<IDrawableElement>();
            mockElement1.SetupAllProperties();
            mockElement2.SetupAllProperties();
            selectionManager.Add(mockElement1.Object);
            selectionManager.Add(mockElement2.Object);

            // Act
            selectionManager.Clear();

            // Assert
            Assert.Empty(selectionManager.Selected);
            Assert.False(selectionManager.HasSelection);
            Assert.False(mockElement1.Object.IsSelected);
            Assert.False(mockElement2.Object.IsSelected);
        }

        [Fact]
        public void Clear_ShouldRaiseSelectionChangedEventIfElementsWerePresent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionManager.Add(mockElement.Object);
            var eventRaised = false;
            selectionManager.SelectionChanged += (sender, args) => eventRaised = true; // Reset and listen

            // Act
            selectionManager.Clear();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Clear_ShouldNotRaiseSelectionChangedEventIfAlreadyEmpty()
        {
            // Arrange
            var eventRaised = false;
            selectionManager.SelectionChanged += (sender, args) => eventRaised = true;

            // Act
            selectionManager.Clear();

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
            selectionManager.Toggle(mockElement.Object);

            // Assert
            Assert.Single(selectionManager.Selected, x => x == mockElement.Object);
            Assert.True(mockElement.Object.IsSelected);
        }

        [Fact]
        public void Toggle_ShouldRemoveElementIfSelected()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionManager.Add(mockElement.Object); // Add it first

            // Act
            selectionManager.Toggle(mockElement.Object);

            // Assert
            Assert.Empty(selectionManager.Selected);
            Assert.False(mockElement.Object.IsSelected);
        }

        [Fact]
        public void Contains_ShouldReturnTrueForSelectedElement()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            selectionManager.Add(mockElement.Object);

            // Act
            var result = selectionManager.Contains(mockElement.Object);

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
            var result = selectionManager.Contains(nonSelectedElement);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetAll_ShouldReturnAllSelectedElements()
        {
            // Arrange
            var mockElement1 = new Mock<IDrawableElement>().Object;
            var mockElement2 = new Mock<IDrawableElement>().Object;
            selectionManager.Add(mockElement1);
            selectionManager.Add(mockElement2);

            // Act
            var allElements = selectionManager.GetAll();

            // Assert
            Assert.Equal(2, allElements.Count());
            Assert.Contains(mockElement1, allElements);
            Assert.Contains(mockElement2, allElements);
        }

        [Fact]
        public void GetBounds_ShouldReturnEmptyRectForEmptySelection()
        {
            // Act
            var bounds = selectionManager.GetBounds();

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
            selectionManager.Add(mockElement.Object);

            // Act
            var bounds = selectionManager.GetBounds();

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
            selectionManager.Add(mockElement1.Object);
            selectionManager.Add(mockElement2.Object);

            // Act
            var bounds = selectionManager.GetBounds();

            // Assert
            Assert.Equal(new SKRect(0, 0, 75, 75), bounds);
        }
    }
}