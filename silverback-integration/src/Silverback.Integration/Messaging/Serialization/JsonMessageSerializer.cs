﻿using System;
using System.Text;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    /// Serializes the message as JSON and then converts them to a UTF8 encoded byte array. 
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Serialization.IMessageSerializer" />
    public class JsonMessageSerializer : IMessageSerializer
    {
        public byte[] Serialize(IEnvelope envelope)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            var json = JsonConvert.SerializeObject(envelope, typeof(IEnvelope), SerializerSettings);

            return Encoding.UTF8.GetBytes(json);
        }

        public IEnvelope Deserialize(byte[] message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var json = Encoding.UTF8.GetString(message);

            return JsonConvert.DeserializeObject<IEnvelope>(json, SerializerSettings);
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };
    }
}