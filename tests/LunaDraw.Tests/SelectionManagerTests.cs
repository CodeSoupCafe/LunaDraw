using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
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
            // (SelectionManager initialized in constructor)

            // Assert
            selectionManager.Selected.Should().BeEmpty();
            selectionManager.HasSelection.Should().BeFalse();
        }

        [Fact]
        public void Add_ShouldAddElementAndSetIsSelected()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties(); // Enable setting IsSelected property

            // Act
            selectionManager.Add(mockElement.Object);

            // Assert
            selectionManager.Selected.Should().ContainSingle(x => x == mockElement.Object); // Fixed
            selectionManager.HasSelection.Should().BeTrue();
            mockElement.Object.IsSelected.Should().BeTrue();
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

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void Add_ShouldNotAddDuplicateElements()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionManager.Add(mockElement.Object); // Add once

            // Act
            selectionManager.Add(mockElement.Object); // Add again

            // Assert
            selectionManager.Selected.Should().ContainSingle(x => x == mockElement.Object); // Fixed
        }

        [Fact]
        public void Remove_ShouldRemoveElementAndSetIsSelectedFalse()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            selectionManager.Add(mockElement.Object); // Add element first

            // Act
            selectionManager.Remove(mockElement.Object);

            // Assert
            selectionManager.Selected.Should().BeEmpty();
            selectionManager.HasSelection.Should().BeFalse();
            mockElement.Object.IsSelected.Should().BeFalse();
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

            // Assert
            eventRaised.Should().BeTrue();
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
            selectionManager.Selected.Should().ContainSingle(x => x == mockElement.Object); // Fixed
            selectionManager.HasSelection.Should().BeTrue();
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
            selectionManager.Selected.Should().BeEmpty();
            selectionManager.HasSelection.Should().BeFalse();
            mockElement1.Object.IsSelected.Should().BeFalse();
            mockElement2.Object.IsSelected.Should().BeFalse();
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
            eventRaised.Should().BeTrue();
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
            eventRaised.Should().BeFalse();
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
            selectionManager.Selected.Should().ContainSingle(x => x == mockElement.Object); // Fixed
            mockElement.Object.IsSelected.Should().BeTrue();
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
            selectionManager.Selected.Should().BeEmpty();
            mockElement.Object.IsSelected.Should().BeFalse();
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
            result.Should().BeTrue();
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
            result.Should().BeFalse();
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
            allElements.Should().HaveCount(2);
            allElements.Should().Contain(mockElement1);
            allElements.Should().Contain(mockElement2);
        }

        [Fact]
        public void GetBounds_ShouldReturnEmptyRectForEmptySelection()
        {
            // Act
            var bounds = selectionManager.GetBounds();

            // Assert
            bounds.Should().Be(SKRect.Empty);
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
            bounds.Should().Be(expectedBounds);
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
            bounds.Should().Be(new SKRect(0, 0, 75, 75));
        }
    }
}