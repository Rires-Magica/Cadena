﻿using System;
using System.Linq;
using Cadena.Data;
using Cadena.Data.Streams;
using Cadena.Data.Streams.Events;
using Cadena.Data.Streams.Warnings;
using Cadena.Engine.StreamReceivers;
using Cadena.Meteor;
using Cadena.Util;

namespace Cadena.Engine._Internals.Parsers
{
    internal static class UserStreamParser
    {
        const string EventSourceKey = "source";
        const string EventTargetKey = "target";
        const string EventCreatedAtKey = "target";
        const string EventTargetObjectKey = "target_object";

        /// <summary>
        /// Parse streamed JSON line
        /// </summary>
        /// <param name="line">JSON line</param>
        /// <param name="handler">result handler</param>
        public static void ParseStreamLine(string line, IStreamHandler handler)
        {
            try
            {
                var element = MeteorJson.Parse(line);
                ParseStreamLine(element, handler);
            }
            catch (Exception ex)
            {
                handler.OnException(new StreamParseException(
                    "JSON parse failed.", line, ex));
            }
        }

        /// <summary>
        /// Parse streamed JSON line
        /// </summary>
        /// <param name="graph">JSON object graph</param>
        /// <param name="handler">result handler</param>
        internal static void ParseStreamLine(JsonValue graph, IStreamHandler handler)
        {


            try
            {
                // element.foo() -> element.IsDefined("foo")

                //
                // fast path: first, identify standard status payload
                ////////////////////////////////////////////////////////////////////////////////////
                if (TwitterStreamParser.ParseStreamLineAsStatus(graph, handler))
                {
                    return;
                }

                //
                // parse stream-specific elements
                //

                // friends lists
                JsonValue friends;
                if (graph.TryGetValue("friends", out friends))
                {
                    // friends enumeration
                    var friendsIds = ((JsonArray)friends).Select(v => v.GetLong()).ToArray();
                    handler.OnMessage(new StreamEnumeration(friendsIds));
                    return;
                }
                if (graph.TryGetValue("friends_str", out friends))
                {
                    // friends enumeration(stringified)
                    var friendsIds = ((JsonArray)friends).Select(v => v.GetString().ParseLong()).ToArray();
                    handler.OnMessage(new StreamEnumeration(friendsIds));
                    return;
                }

                JsonValue @event;
                if (graph.TryGetValue("event", out @event))
                {
                    ParseStreamEvent(@event.GetString().ToLower(), graph, handler);
                    return;
                }

                // too many follows warning
                JsonValue warning;
                if (graph.TryGetValue("warning", out warning))
                {
                    var code = warning["code"].GetString();
                    if (code == "FOLLOWS_OVER_LIMIT")
                    {
                        handler.OnMessage(new StreamTooManyFollowsWarning(
                            code,
                            warning["message"].GetString(),
                            warning["user_id"].GetLong(),
                            TwitterStreamParser.GetTimestamp(warning)));
                        return;
                    }
                }

                // fallback to default stream handler
                TwitterStreamParser.ParseNotStatusStreamLine(graph, handler);
            }
            catch (Exception ex)
            {
                handler.OnException(new StreamParseException(
                    "Stream graph parse failed.", graph.ToString(), ex));
            }
        }

        /// <summary>
        /// Parse streamed twitter event
        /// </summary>
        /// <param name="ev">event name</param>
        /// <param name="graph">JSON object graph</param>
        /// <param name="handler">result handler</param>
        private static void ParseStreamEvent(string ev, JsonValue graph, IStreamHandler handler)
        {
            try
            {
                var source = new TwitterUser(graph[EventSourceKey]);
                var target = new TwitterUser(graph[EventTargetKey]);
                var timestamp = graph[EventCreatedAtKey].GetString().ParseTwitterDateTime();
                switch (ev)
                {
                    case "favorite":
                    case "unfavorite":
                    case "quoted_tweet":
                    case "favorited_retweet":
                    case "retweeted_retweet":
                        handler.OnMessage(new StreamStatusEvent(source, target,
                            new TwitterStatus(graph[EventTargetObjectKey]), ev, timestamp));
                        break;
                    case "block":
                    case "unblock":
                    case "follow":
                    case "unfollow":
                    case "mute":
                    case "unmute":
                    case "user_update":
                    case "user_delete":
                    case "user_suspend":
                        handler.OnMessage(new StreamUserEvent(source, target,
                            ev, timestamp));
                        break;
                    case "list_created":
                    case "list_destroyed":
                    case "list_updated":
                    case "list_member_added":
                    case "list_member_removed":
                    case "list_user_subscribed":
                    case "list_user_unsubscribed":
                        handler.OnMessage(new StreamListEvent(source, target,
                            new TwitterList(graph[EventTargetObjectKey]), ev, timestamp));
                        break;
                    case "access_revoked":
                    case "access_unrevoked":
                        handler.OnMessage(new StreamAccessInformationEvent(source, target,
                            new AccessInformation(graph[EventTargetObjectKey]), ev, timestamp));
                        break;
                }
            }
            catch (Exception ex)
            {
                handler.OnException(new StreamParseException(
                    "Event parse failed:" + ev, graph.ToString(), ex));
            }
        }
    }
}
