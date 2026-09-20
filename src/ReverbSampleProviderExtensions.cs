using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NAudioEffects
{
    /// <summary>
    /// Extension methods for <see cref="ReverbSampleProvider"/> to support asynchronous offline rendering.
    /// </summary>
    public static class ReverbSampleProviderExtensions
    {
        /// <summary>
        /// Asynchronously renders the reverb effect to a destination stream in chunks.
        /// </summary>
        /// <param name="source">The source audio provider.</param>
        /// <param name="destination">The destination stream (e.g., <see cref="WaveFileWriter"/>).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
        public static async Task RenderToAsync(
            ISampleProvider source,
            Stream destination,
            CancellationToken cancellationToken = default,
            IProgress<float>? progress = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            var reverb = new ReverbSampleProvider(source);
            var buffer = new float[4096];
            int samplesRead;

            progress?.Report(0f);

            while ((samplesRead = reverb.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await destination.WriteAsync(buffer.AsMemory(0, samplesRead), cancellationToken).ConfigureAwait(false);
            }

            progress?.Report(1f);
        }
    }
}
