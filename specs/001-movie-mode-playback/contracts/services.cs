using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LunaDraw.Logic.Models;

namespace LunaDraw.Logic.Services
{
    /// <summary>
    /// Manages the recording of drawing events (creation timestamps).
    /// </summary>
    public interface IRecordingService
    {
        /// <summary>
        /// Stamps the element with the current time.
        /// Call this when a new element is added to the canvas.
        /// </summary>
        void RecordCreation(IDrawableElement element);
    }

    /// <summary>
    /// Controls the playback of the drawing history.
    /// </summary>
    public interface IPlaybackService
    {
        /// <summary>
        /// Current state of playback.
        /// </summary>
        IObservable<PlaybackState> CurrentState { get; }

        /// <summary>
        /// Prepares the playback sequence from a list of layers.
        /// Extracts all elements, sorts by CreatedAt, and prepares the queue.
        /// </summary>
        /// <param name="layers">The layers to reconstruct.</param>
        void Load(IEnumerable<Layer> layers);

        /// <summary>
        /// Starts or Resumes playback.
        /// </summary>
        /// <param name="speed">Desired playback speed.</param>
        Task PlayAsync(PlaybackSpeed speed);

        /// <summary>
        /// Pauses playback.
        /// </summary>
        Task PauseAsync();

        /// <summary>
        /// Stops playback and resets to the final state (or initial state depending on UX).
        /// </summary>
        Task StopAsync();
    }
}
