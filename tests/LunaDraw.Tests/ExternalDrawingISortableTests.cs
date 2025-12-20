using LunaDraw.Logic.Models;
using System.Text.Json;
using Xunit;

namespace LunaDraw.Tests;

public class ExternalDrawingISortableTests
{
  [Fact]
  public void Should_Map_Name_To_Title_When_ISortable_Accessed_Returns_Same_Value()
  {
    // Arrange
    var drawing = new External.Drawing
    {
      Id = Guid.NewGuid(),
      Name = "Test Drawing",
      LastModified = DateTime.UtcNow
    };

    // Act
    string title = drawing.Title;

    // Assert
    Assert.Equal("Test Drawing", title);
    Assert.Equal(drawing.Name, title);
  }

  [Fact]
  public void Should_Convert_LastModified_To_DateCreated_When_ISortable_Accessed_Returns_DateTimeOffset()
  {
    // Arrange
    var lastModified = new DateTime(2025, 12, 19, 10, 30, 0, DateTimeKind.Utc);
    var drawing = new External.Drawing
    {
      Id = Guid.NewGuid(),
      Name = "Test Drawing",
      LastModified = lastModified
    };

    // Act
    DateTimeOffset dateCreated = drawing.DateCreated;

    // Assert
    Assert.Equal(new DateTimeOffset(lastModified), dateCreated);
  }

  [Fact]
  public void Should_Convert_LastModified_To_DateUpdated_When_ISortable_Accessed_Returns_DateTimeOffset()
  {
    // Arrange
    var lastModified = new DateTime(2025, 12, 19, 14, 45, 30, DateTimeKind.Utc);
    var drawing = new External.Drawing
    {
      Id = Guid.NewGuid(),
      Name = "Test Drawing",
      LastModified = lastModified
    };

    // Act
    DateTimeOffset dateUpdated = drawing.DateUpdated;

    // Assert
    Assert.Equal(new DateTimeOffset(lastModified), dateUpdated);
  }

  [Fact]
  public void Should_Not_Serialize_ISortable_Properties_When_Drawing_Serialized_Returns_Original_Json()
  {
    // Arrange
    var drawing = new External.Drawing
    {
      Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
      Name = "My Drawing",
      LastModified = new DateTime(2025, 12, 19, 12, 0, 0, DateTimeKind.Utc),
      CanvasWidth = 800,
      CanvasHeight = 600,
      Layers = []
    };

    // Act
    string json = JsonSerializer.Serialize(drawing);

    // Assert
    Assert.DoesNotContain("\"Title\"", json);
    Assert.DoesNotContain("\"DateCreated\"", json);
    Assert.DoesNotContain("\"DateUpdated\"", json);
    Assert.Contains("\"i\":", json);
    Assert.Contains("\"n\":\"My Drawing\"", json);
    Assert.Contains("\"lm\":", json);
  }

  [Fact]
  public void Should_Deserialize_Without_ISortable_Properties_When_Json_Loaded_Returns_Valid_Drawing()
  {
    // Arrange
    string json = """
    {
      "i": "12345678-1234-1234-1234-123456789012",
      "n": "Restored Drawing",
      "lm": "2025-12-19T12:00:00Z",
      "cw": 1024,
      "ch": 768,
      "l": []
    }
    """;

    // Act
    var drawing = JsonSerializer.Deserialize<External.Drawing>(json);

    // Assert
    Assert.NotNull(drawing);
    Assert.Equal("Restored Drawing", drawing.Name);
    Assert.Equal("Restored Drawing", drawing.Title);
    Assert.Equal(1024, drawing.CanvasWidth);
    Assert.Equal(768, drawing.CanvasHeight);
  }

  [Fact]
  public void Should_Have_Same_DateCreated_And_DateUpdated_When_LastModified_Set_Returns_Equal_Values()
  {
    // Arrange
    var drawing = new External.Drawing
    {
      Id = Guid.NewGuid(),
      Name = "Test Drawing",
      LastModified = DateTime.UtcNow
    };

    // Act
    DateTimeOffset dateCreated = drawing.DateCreated;
    DateTimeOffset dateUpdated = drawing.DateUpdated;

    // Assert
    Assert.Equal(dateCreated, dateUpdated);
  }

  [Theory]
  [InlineData("")]
  [InlineData("Untitled")]
  [InlineData("My Amazing Artwork 2025")]
  public void Should_Return_Correct_Title_When_Name_Varies_Returns_Mapped_Value(string name)
  {
    // Arrange
    var drawing = new External.Drawing
    {
      Id = Guid.NewGuid(),
      Name = name,
      LastModified = DateTime.UtcNow
    };

    // Act
    string title = drawing.Title;

    // Assert
    Assert.Equal(name, title);
  }

  [Fact]
  public void Should_Implement_ISortable_Interface_When_Drawing_Created_Returns_True()
  {
    // Arrange & Act
    var drawing = new External.Drawing
    {
      Id = Guid.NewGuid(),
      Name = "Test",
      LastModified = DateTime.UtcNow
    };

    // Assert
    Assert.IsAssignableFrom<CodeSoupCafe.Maui.Models.ISortable>(drawing);
  }
}
