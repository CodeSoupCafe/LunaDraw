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

using Xunit;
using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using LunaDraw.Logic.Services;

namespace LunaDraw.Tests;

public class ThumbnailCacheFacadeTests : IDisposable
{
    private readonly string testCacheDirectory;
    private readonly ThumbnailCacheFacade thumbnailCacheFacade;

    public ThumbnailCacheFacadeTests()
    {
        testCacheDirectory = Path.Combine(Path.GetTempPath(), "LunaDrawThumbnailCacheTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testCacheDirectory);

        thumbnailCacheFacade = new ThumbnailCacheFacade(testCacheDirectory);
    }

    [Fact]
    public async Task Should_Return_Null_When_Cache_Does_Not_Exist()
    {
        // Arrange
        var drawingId = Guid.NewGuid();

        // Act
        var result = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_Save_And_Retrieve_Thumbnail_When_Valid_Base64_Data_Provided()
    {
        // Arrange
        var drawingId = Guid.NewGuid();
        var base64Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId, base64Data);
        var result = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId);

        // Assert
        result.Should().Be(base64Data);
    }

    [Fact]
    public async Task Should_Overwrite_Existing_Thumbnail_When_Saving_Same_DrawingId()
    {
        // Arrange
        var drawingId = Guid.NewGuid();
        var firstBase64Data = "firstData";
        var secondBase64Data = "secondData";

        // Act
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId, firstBase64Data);
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId, secondBase64Data);
        var result = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId);

        // Assert
        result.Should().Be(secondBase64Data);
        result.Should().NotBe(firstBase64Data);
    }

    [Fact]
    public async Task Should_Invalidate_Thumbnail_When_Exists()
    {
        // Arrange
        var drawingId = Guid.NewGuid();
        var base64Data = "testData";
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId, base64Data);

        // Act
        await thumbnailCacheFacade.InvalidateThumbnailAsync(drawingId);
        var result = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_Not_Throw_When_Invalidating_Non_Existent_Thumbnail()
    {
        // Arrange
        var drawingId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await thumbnailCacheFacade.InvalidateThumbnailAsync(drawingId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_Clear_All_Cache_When_Multiple_Thumbnails_Exist()
    {
        // Arrange
        var drawingId1 = Guid.NewGuid();
        var drawingId2 = Guid.NewGuid();
        var drawingId3 = Guid.NewGuid();
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId1, "data1");
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId2, "data2");
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId3, "data3");

        // Act
        await thumbnailCacheFacade.ClearAllCacheAsync();
        var result1 = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId1);
        var result2 = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId2);
        var result3 = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId3);

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    [Fact]
    public void Should_Return_Correct_Cache_Path_When_DrawingId_Provided()
    {
        // Arrange
        var drawingId = Guid.NewGuid();
        var expectedPath = Path.Combine(testCacheDirectory, "thumbnails", $"{drawingId}.thumb");

        // Act
        var result = thumbnailCacheFacade.GetCachePath(drawingId);

        // Assert
        result.Should().Be(expectedPath);
    }

    [Fact]
    public async Task Should_Create_Thumbnail_File_When_Saving()
    {
        // Arrange
        var drawingId = Guid.NewGuid();
        var base64Data = "testFileCreation";
        var expectedPath = thumbnailCacheFacade.GetCachePath(drawingId);

        // Act
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId, base64Data);

        // Assert
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task Should_Delete_Thumbnail_File_When_Invalidating()
    {
        // Arrange
        var drawingId = Guid.NewGuid();
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId, "testDelete");
        var filePath = thumbnailCacheFacade.GetCachePath(drawingId);

        // Act
        await thumbnailCacheFacade.InvalidateThumbnailAsync(drawingId);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==")]
    public async Task Should_Handle_Various_Base64_String_Lengths_When_Saving(string base64Data)
    {
        // Arrange
        var drawingId = Guid.NewGuid();

        // Act
        await thumbnailCacheFacade.SaveThumbnailAsync(drawingId, base64Data);
        var result = await thumbnailCacheFacade.GetThumbnailBase64Async(drawingId);

        // Assert
        result.Should().Be(base64Data);
    }

    public void Dispose()
    {
        if (Directory.Exists(testCacheDirectory))
        {
            Directory.Delete(testCacheDirectory, true);
        }
    }
}
