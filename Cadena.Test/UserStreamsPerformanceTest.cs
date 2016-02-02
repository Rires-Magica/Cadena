﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cadena.Api.Streams;
using Cadena.Data;
using Cadena.Data.Streams;
using Cadena.Engine._Internals.Parsers;
using Cadena.Engine.StreamReceivers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cadena.Test
{
    /// <summary>
    /// UserStreamsPerformanceTest の概要の説明
    /// </summary>
    [TestClass]
    public class UserStreamsPerformanceTest
    {
        [TestMethod]
        public async Task UserStreamParserPerformanceTest()
        {
            var source = new CancellationTokenSource();
            var handler = new PseudoStreamHandler();
            var received = 0;
            source.CancelAfter(TimeSpan.FromSeconds(10));
            foreach (var content in TweetSamples.GetStreamSamples())
            {
                if (source.IsCancellationRequested) break;
                UserStreamParserDynamic.ParseStreamLine(content, handler);
            }
            Trace.WriteLine("received:" + received);
            Trace.WriteLine("handler: statuses: " + handler.ReceivedStatuses + " / events: " + handler.ReceivedEvents);
        }

        [TestMethod]
        public async Task StreamWinderPerformanceTest()
        {
            var source = new CancellationTokenSource();
            var testStream = new InfiniteStream(TweetSamples.GetBinalyStreamSamples(), source.Token);
            var handler = new PseudoStreamHandler();
            var received = 0;
            source.CancelAfter(TimeSpan.FromSeconds(10));
            var receiveTask = StreamWinder.Run(testStream, content =>
            {
                //UserStreamParserDynamic.ParseStreamLine(content, handler);
                received++;
                //System.Diagnostics.Debug.WriteLine(content);
            }, Timeout.InfiniteTimeSpan, source.Token);
            try
            {
                await receiveTask;
            }
            catch (OperationCanceledException)
            {
                // this is expected.
            }
            System.Diagnostics.Debug.WriteLine(received);
            // i promise myself the cadena engine can handle > 10K events per second.
            Trace.WriteLine("received:" + received);
            Trace.WriteLine("handler: statuses: " + handler.ReceivedStatuses + " / events: " + handler.ReceivedEvents);
            // Assert.IsTrue(received > 10000 * 10);
        }

        private class PseudoStreamHandler : IStreamHandler
        {
            private int _receivedStatuses;

            private int _receivedEvents;

            public int ReceivedStatuses
            {
                get { return _receivedStatuses; }
            }

            public int ReceivedEvents
            {
                get { return _receivedEvents; }
            }

            public void OnStatus(TwitterStatus status)
            {
                _receivedStatuses++;
            }

            public void OnMessage(StreamMessage notification)
            {
                _receivedEvents++;
            }

            public void OnException(StreamParseException exception)
            {
                Trace.WriteLine(exception);
            }

            public void OnStateChanged(StreamState state)
            {
            }

            public void Log(string log)
            {
            }
        }

        private class InfiniteStream : Stream
        {
            private readonly IEnumerator<byte[]> _enumerator;
            private readonly CancellationToken _token;

            private byte[] _currentItem;
            private int _currentItemCursor;

            public InfiniteStream(IEnumerable<string> contents, CancellationToken token)
                : this(contents.Select(c => Encoding.UTF8.GetBytes(c)), token)
            {

            }

            public InfiniteStream(IEnumerable<byte[]> contents, CancellationToken token)
            {
                _enumerator = contents.GetEnumerator();
                _token = token;
            }

            public override int ReadTimeout { get; set; }

            public override void Flush()
            {
                throw new System.NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new System.NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new System.NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_token.IsCancellationRequested)
                {
                    return 0;
                }
                if (_currentItem == null || _currentItemCursor >= _currentItem.Length)
                {
                    // switch to next item
                    if (!_enumerator.MoveNext())
                    {
                        // end of enumeration
                        return 0;
                    }
                    _currentItem = _enumerator.Current;
                    _currentItemCursor = 0;
                }
                // read trailing
                var length = _currentItem.Length - _currentItemCursor;
                if (count < length) length = count;
                Array.Copy(_currentItem, _currentItemCursor, buffer, offset, length);
                _currentItemCursor += length;
                return length;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new System.NotImplementedException();
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;

            public override long Length
            {
                get { throw new NotSupportedException(); }
            }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }
        }
    }
}