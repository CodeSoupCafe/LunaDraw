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
        private readonly SelectionManager sut;

        public SelectionManagerTests()
        {
            sut = new SelectionManager();
        }

        [Fact]
        public void Constructor_ShouldInitializeEmptySelection()
        {
            // Arrange
            // Act
            // (SUT initialized in constructor)

            // Assert
            sut.Selected.Should().BeEmpty();
            sut.HasSelection.Should().BeFalse();
        }

        [Fact]
        public void Add_ShouldAddElementAndSetIsSelected()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties(); // Enable setting IsSelected property

            // Act
            sut.Add(mockElement.Object);

            // Assert
            sut.Selected.Should().ContainSingle(x => x == mockElement.Object); // Fixed
            sut.HasSelection.Should().BeTrue();
            mockElement.Object.IsSelected.Should().BeTrue();
        }

        [Fact]
        public void Add_ShouldRaiseSelectionChangedEvent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            var eventRaised = false;
            sut.SelectionChanged += (sender, args) => eventRaised = true;

            // Act
            sut.Add(mockElement.Object);

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void Add_ShouldNotAddDuplicateElements()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            sut.Add(mockElement.Object); // Add once

            // Act
            sut.Add(mockElement.Object); // Add again

            // Assert
            sut.Selected.Should().ContainSingle(x => x == mockElement.Object); // Fixed
        }

        [Fact]
        public void Add_ShouldIgnoreNullElement()
        {
            // Arrange
            IDrawableElement nullElement = null;

            // Act
            sut.Add(nullElement);

            // Assert
            sut.Selected.Should().BeEmpty();
            sut.HasSelection.Should().BeFalse();
        }

        [Fact]
        public void Remove_ShouldRemoveElementAndSetIsSelectedFalse()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            sut.Add(mockElement.Object); // Add element first

            // Act
            sut.Remove(mockElement.Object);

            // Assert
            sut.Selected.Should().BeEmpty();
            sut.HasSelection.Should().BeFalse();
            mockElement.Object.IsSelected.Should().BeFalse();
        }

        [Fact]
        public void Remove_ShouldRaiseSelectionChangedEvent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            sut.Add(mockElement.Object); // Add element first
            var eventRaised = false;
            sut.SelectionChanged += (sender, args) => eventRaised = true; // Reset and listen again

            // Act
            sut.Remove(mockElement.Object);

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
            sut.Add(mockElement.Object); // Add one element

            // Act
            sut.Remove(null);
            sut.Remove(nonExistentElement);

            // Assert
            sut.Selected.Should().ContainSingle(x => x == mockElement.Object); // Fixed
            sut.HasSelection.Should().BeTrue();
        }

        [Fact]
        public void Clear_ShouldClearAllElements()
        {
            // Arrange
            var mockElement1 = new Mock<IDrawableElement>();
            var mockElement2 = new Mock<IDrawableElement>();
            mockElement1.SetupAllProperties();
            mockElement2.SetupAllProperties();
            sut.Add(mockElement1.Object);
            sut.Add(mockElement2.Object);

            // Act
            sut.Clear();

            // Assert
            sut.Selected.Should().BeEmpty();
            sut.HasSelection.Should().BeFalse();
            mockElement1.Object.IsSelected.Should().BeFalse();
            mockElement2.Object.IsSelected.Should().BeFalse();
        }

        [Fact]
        public void Clear_ShouldRaiseSelectionChangedEventIfElementsWerePresent()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            sut.Add(mockElement.Object);
            var eventRaised = false;
            sut.SelectionChanged += (sender, args) => eventRaised = true; // Reset and listen

            // Act
            sut.Clear();

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void Clear_ShouldNotRaiseSelectionChangedEventIfAlreadyEmpty()
        {
            // Arrange
            var eventRaised = false;
            sut.SelectionChanged += (sender, args) => eventRaised = true;

            // Act
            sut.Clear();

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
            sut.Toggle(mockElement.Object);

            // Assert
            sut.Selected.Should().ContainSingle(x => x == mockElement.Object); // Fixed
            mockElement.Object.IsSelected.Should().BeTrue();
        }

        [Fact]
        public void Toggle_ShouldRemoveElementIfSelected()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            mockElement.SetupAllProperties();
            sut.Add(mockElement.Object); // Add it first

            // Act
            sut.Toggle(mockElement.Object);

            // Assert
            sut.Selected.Should().BeEmpty();
            mockElement.Object.IsSelected.Should().BeFalse();
        }

        [Fact]
        public void Toggle_ShouldIgnoreNullElement()
        {
            // Arrange
            IDrawableElement nullElement = null;
            var initialCount = sut.Selected.Count;

            // Act
            sut.Toggle(nullElement);

            // Assert
            sut.Selected.Count.Should().Be(initialCount); // No change
        }

        [Fact]
        public void Contains_ShouldReturnTrueForSelectedElement()
        {
            // Arrange
            var mockElement = new Mock<IDrawableElement>();
            sut.Add(mockElement.Object);

            // Act
            var result = sut.Contains(mockElement.Object);

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
            var result = sut.Contains(nonSelectedElement);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Contains_ShouldReturnFalseForNullElement()
        {
            // Arrange
            IDrawableElement nullElement = null;

            // Act
            var result = sut.Contains(nullElement);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetAll_ShouldReturnAllSelectedElements()
        {
            // Arrange
            var mockElement1 = new Mock<IDrawableElement>().Object;
            var mockElement2 = new Mock<IDrawableElement>().Object;
            sut.Add(mockElement1);
            sut.Add(mockElement2);

            // Act
            var allElements = sut.GetAll();

            // Assert
            allElements.Should().HaveCount(2);
            allElements.Should().Contain(mockElement1);
            allElements.Should().Contain(mockElement2);
        }

        [Fact]
        public void GetBounds_ShouldReturnEmptyRectForEmptySelection()
        {
            // Act
            var bounds = sut.GetBounds();

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
            sut.Add(mockElement.Object);

            // Act
            var bounds = sut.GetBounds();

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
            sut.Add(mockElement1.Object);
            sut.Add(mockElement2.Object);

            // Act
            var bounds = sut.GetBounds();

            // Assert
            bounds.Should().Be(new SKRect(0, 0, 75, 75));
        }
    }
}