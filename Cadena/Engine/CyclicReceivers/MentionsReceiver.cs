﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cadena.Api.Rest;
using Cadena.Data;
using JetBrains.Annotations;

namespace Cadena.Engine.CyclicReceivers
{
    public class MentionsReceiver : CyclicTimelineReceiverBase
    {
        private readonly ApiAccessor _accessor;
        private readonly Action<Exception> _exceptionHandler;
        private readonly int _receiveCount;
        private readonly bool _includeRetweets;

        public MentionsReceiver([NotNull] ApiAccessor accessor, [NotNull] Action<TwitterStatus> handler,
            [NotNull] Action<Exception> exceptionHandler, int receiveCount = 100, bool includeRetweets = false) : base(handler)
        {
            if (accessor == null) throw new ArgumentNullException(nameof(accessor));
            if (exceptionHandler == null) throw new ArgumentNullException(nameof(exceptionHandler));
            _accessor = accessor;
            _exceptionHandler = exceptionHandler;
            _receiveCount = receiveCount;
            _includeRetweets = includeRetweets;
        }

        protected override async Task<RateLimitDescription> Execute(CancellationToken token)
        {
            try
            {
                var result = await _accessor.GetMentionsAsync(_receiveCount,
                    LastSinceId, null, _includeRetweets, token).ConfigureAwait(false);
                result.Result?.ForEach(CallHandler);
                return result.RateLimit;
            }
            catch (Exception ex)
            {
                _exceptionHandler(ex);
                throw;
            }
        }
    }
}
