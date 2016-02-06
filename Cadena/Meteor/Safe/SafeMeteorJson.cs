﻿namespace Cadena.Meteor.Safe
{
    public static class SafeMeteorJson
    {
        public static unsafe JsonValue Parse(string text)
        {
            return new JsonStringParser(text).Parse();
        }

        private sealed class JsonStringParser : MeteorSafeJsonParserBase
        {
            private readonly string _json;

            public JsonStringParser(string json)
            {
                _json = json;
            }

            public unsafe JsonValue Parse()
            {
                var array = _json.ToCharArray();
                var index = 0;
                var length = array.Length;
                // read value
                var value = ReadValue(ref array, ref index, ref length);
                // check after object
                SkipWhitespaces(ref array, ref index, ref length);
                if (index < length)
                {
                    throw CreateException(array, index, "invalid character is existed after the valid object: " + array[index]);
                }
                // return result
                return value;
            }

            protected override bool ReadMore(ref char[] buffer, ref int index, ref int length)
            {
                return false;
            }

            protected override JsonParseException CreateException(char[] buffer, int index, string message)
            {
                throw new JsonParseException(message, _json, index);
            }
        }
    }
}