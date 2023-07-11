// Not ported to .NET 7 yet, use old JsonObjectReader if required...

//using System;
//using System.IO;
//using System.Threading.Tasks;

//namespace Inspiring.Json {
//    /// <summary>
//    /// Reads multiple (potentially polymorphic) objects from a JSON file and handles any errors that may occur. See code sample
//    /// for more details.
//    /// </summary>
//    /// <typeparam name="T">
//    /// The common type to which the objects in the file should be deserialized. <typeparamref name="T"/> may be an abstract type 
//    /// or interface as long the are correctly annotated with the <see cref="ContractAttribute"/>.
//    /// </typeparam>
//    /// <example>
//    ///     <code><![CDATA[
//    /// class VehicleJsonReader : JsonObjectReader<IVehicle> { }
//    /// 
//    /// [Contract(DiscriminatorName = "Type")]
//    /// interface IVehicle { int Speed { get; } }
//    /// 
//    /// [Contract]
//    /// class Car { public int Speed { get; set; } }
//    /// 
//    /// [Contract]
//    /// class Bike { public int Speed { get; set; } }
//    /// 
//    /// VehicleJsonReader r = new VehicleJsonReader(@"
//    ///   { 'Type': 'Car', 'Speed': '200' }
//    ///   { 'Type': 'Bike', 'Speed': '50' }
//    /// ";
//    /// 
//    /// await r.ReadAsync();
//    /// ]]></code>
//    /// </example>
//    public class JsonObjectReader<T> : IDisposable where T : class {
//        private static readonly JsonSerializerSettings __defaultSettings = CreateDefaultSettings();

//        private readonly JsonReader _reader;
//        private Func<T, JObject, Task>? _onObjectRead;
//        private Func<string, JObject, Exception?, Task>? _onObjectError;
//        private Func<string, Exception?, Task>? _onDocumentError;
//        private JsonSerializerSettings _settings;

//        protected JsonObjectReader(string json, JsonSerializerSettings? settings = null)
//            : this(new JsonTextReader(new StringReader(json)) { CloseInput = true }, settings) { }

//        protected JsonObjectReader(JsonReader reader, JsonSerializerSettings? settings = null) {
//            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
//            _reader.SupportMultipleContent = true;
//            _settings = settings ?? __defaultSettings;
//        }

//        /// <summary>
//        /// Reads the objects using the reader given in the constructor and calls <see cref="OnObjectRead"/>, 
//        /// <see cref="OnObjectError"/> or <see cref="OnDocumentError"/>.
//        /// </summary>
//        public async Task ReadAsync() {
//            JsonSerializer serializer = JsonSerializer.Create(_settings);

//            try {
//                while (await _reader.ReadAsync()) {
//                    // Save the start position of the new object
//                    ReaderPositionInfo? pos = ReaderPositionInfo.From(_reader);

//                    // IMPORTANT: To get correct LineNumber/LinePosition infos in thrown exceptions we have to use JObject.Load 
//                    // here and NOT serializer.Deserialize<JObject> which does not set any positions! It shouldn't be a problem
//                    // that we don't use the JsonSerializerSettings here because we are just buffering the JSON 1-to-1. These
//                    // settings are considered when we actually deserialize the concrete type at the end of this method.
//                    //
//                    // This will throw a JsonReaderException if a null token is encountered. But this should be a very rare 
//                    // and obscure case, for example "{ 'Type': 'T1' } null { 'Type': 'T2' }".
//                    JObject json = await JObject.LoadAsync(_reader);

//                    T? obj = null;
//                    try {
//                        obj = json.ToObject<T>(serializer);
//                    } catch (JsonException ex) {
//                        // We cannot catch a document level error here, because deserialization to JObject was already
//                        // successful, which means it is valid JSON.
//                        await OnObjectError(ex.Message, json, ex);
//                    } catch (ArgumentException ex) {
//                        await HandleObjectArgumentException(ex, json, pos);
//                    }

//                    // We have to call 'OnObjectRead' outside of the try-block, otherwise we would accidentially catch 
//                    // exceptions thrown by 'OnObjectRead'.
//                    if (obj != null) {
//                        await OnObjectRead(obj, json);
//                    }
//                }
//            } catch (JsonReaderException ex) {
//                await OnDocumentError(ex.Message, ex);
//            }
//        }
//        public void Dispose() {
//            _reader?.Close();
//        }

//        public static JsonObjectReader<T> Create(
//            string json,
//            Func<T, JObject, Task>? onObjectRead = null,
//            Func<string, JObject, Exception?, Task>? onObjectError = null,
//            Func<string, Exception?, Task>? onDocumentError = null,
//            JsonSerializerSettings? settings = null
//        ) {
//            return new JsonObjectReader<T>(json, settings) {
//                _onObjectRead = onObjectRead,
//                _onObjectError = onObjectError,
//                _onDocumentError = onDocumentError
//            };
//        }

//        public static JsonObjectReader<T> Create(
//            JsonReader reader,
//            Func<T, JObject, Task>? onObjectRead = null,
//            Func<string, JObject, Exception?, Task>? onObjectError = null,
//            Func<string, Exception?, Task>? onDocumentError = null,
//            JsonSerializerSettings? settings = null
//        ) {
//            return new JsonObjectReader<T>(reader, settings) {
//                _onObjectRead = onObjectRead,
//                _onObjectError = onObjectError,
//                _onDocumentError = onDocumentError
//            };
//        }

//        /// <summary>
//        /// Called by <see cref="ReadAsync"/> when syntax of the JSON document is invalid. After a document error the
//        /// reader cannot reader any further objects.
//        /// </summary>
//        protected virtual Task OnDocumentError(string message, Exception? exception)
//            => _onDocumentError?.Invoke(message, exception) ?? Task.CompletedTask;

//        /// <summary>
//        /// Called by <see cref="ReadAsync"/> when the basic syntax is valid but a single object cannot be deserialized 
//        /// successfully (for example invalid property values). After an object error, the reader continues to read further
//        /// objects.
//        /// </summary>
//        protected virtual Task OnObjectError(string message, JObject json, Exception? exception)
//            => _onObjectError?.Invoke(message, json, exception) ?? Task.CompletedTask;

//        /// <summary>
//        /// Called by <see cref="ReadAsync"/> when an object was successfully deserialized.
//        /// </summary>
//        protected virtual Task OnObjectRead(T obj, JObject json)
//            => _onObjectRead?.Invoke(obj, json) ?? Task.CompletedTask;

//        /// <summary>
//        /// Called when the instantiation of the target object type throws any <see cref="ArgumentException"/>. Uses <see cref="ArgumentException.ParamName"/>
//        /// to guess the JSON property name (by capitalizing the first letter) and calls <see cref="OnObjectError"/> with an
//        /// useful error message.
//        /// </summary>
//        protected virtual Task HandleObjectArgumentException(ArgumentException ex, JObject json, ReaderPositionInfo? pos) {
//            string propertyName = "<unknown>";
//            string message = ex.Message;

//            // If the ArgumentException specifies a name, we guess the property name by capitalizing the
//            // first letter of the parameter name (that's how constructor matching usually works).
//            if (!String.IsNullOrEmpty(ex.ParamName)) {
//                propertyName = Char.ToUpper(ex.ParamName[0]) + ex.ParamName.Substring(1);
//                message = message
//                    .Replace($" (Parameter '{ex.ParamName}')", "") // Remove the parameter information appended by the 'Message' property
//                    .Replace(ex.ParamName, propertyName);
//            }

//            // If the Inspiring.Json.ContractJsonConverter is used, it adds additional information
//            // to any exception that is thrown.
//            string discriminatorValue = ex.Data["DiscriminatorValue"] as string ?? typeof(T).Name;

//            return OnObjectError(
//                LJson.ArgumentExceptionOnDeserialization.FormatWith(discriminatorValue, propertyName, message, pos?.Format()),
//                json,
//                ex);
//        }

//        private static JsonSerializerSettings CreateDefaultSettings() {
//            var s = new JsonSerializerSettings();
//            s.Converters.Add(ContractJsonConverter.Default);
//            return s;
//        }

//        protected class ReaderPositionInfo {
//            public string Path { get; private set; } = "";
//            public int LineNumber { get; private set; }
//            public int LinePosition { get; private set; }

//            internal string Format() =>
//                LJson.PositionInfo.FormatWith(Path, LineNumber, LinePosition);

//            internal static ReaderPositionInfo? From(JsonReader reader) =>
//                reader is IJsonLineInfo info && info.HasLineInfo() ?
//                    new ReaderPositionInfo { Path = reader.Path, LineNumber = info.LineNumber, LinePosition = info.LinePosition } :
//                    null;
//        }
//    }
//}
