﻿using System;
using Cadena.Util;

namespace Cadena.Data.Streams
{
    /// <summary>
    /// Base class of stream message messages.
    /// </summary>
    public abstract class StreamMessage
    {
        private static readonly DateTime SerialTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Initialize stream message.
        /// </summary>
        /// <param name="timestampMs">serial timestamp (millisec, from 1970/01/01 00:00:00)</param>
        protected StreamMessage(string timestampMs)
            : this(timestampMs.ParseLong())
        {
        }

        /// <summary>
        /// Initialize stream message.
        /// </summary>
        /// <param name="timestampMs">serial timestamp (millisec, from 1970/01/01 00:00:00)</param>
        protected StreamMessage(long timestampMs)
        {
            Timestamp = SerialTime.AddMilliseconds(timestampMs).ToLocalTime();
        }

        /// <summary>
        /// Initialize stream message.
        /// </summary>
        /// <param name="timestamp">timestamp</param>
        protected StreamMessage(DateTime timestamp)
        {
            Timestamp = timestamp;
        }

        /// <summary>
        /// Message timestamp
        /// </summary>
        public DateTime Timestamp { get; }
    }
}
