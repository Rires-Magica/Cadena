﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cadena._Internal;
using JetBrains.Annotations;

namespace Cadena.Api.Streams
{
    /// <summary>
    /// Receiver task for general streams.
    /// </summary>
    internal static class StreamEngine
    {
        public static async Task Run([NotNull] Stream stream, [NotNull] Action<string> parser,
            TimeSpan readTimeout, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            using (var reader = new CancellableStreamReader(stream))
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    /* 
                     * NOTE: performance information
                     * Creating CancellationTokenSource each time is faster than using Task.Delay.
                     * Simpler way always defeats complex and confusing one.
                     */
                    // create timeout cancellation token source
                    using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        tokenSource.CancelAfter(readTimeout);
                        // execute reading next line
                        var line = (await reader.ReadLineAsync(tokenSource.Token).ConfigureAwait(false));
                        if (line == null)
                        {
                            System.Diagnostics.Debug.WriteLine("#USERSTREAM# CONNECTION CLOSED.");
                            break;
                        }

                        // skip empty response
                        if (String.IsNullOrWhiteSpace(line)) continue;
                        // call parser with read line
#pragma warning disable  4014
                        Task.Run(() => parser(line), cancellationToken).ConfigureAwait(false);
#pragma warning restore  4014
                    }
                }
            }
        }
    }
}
