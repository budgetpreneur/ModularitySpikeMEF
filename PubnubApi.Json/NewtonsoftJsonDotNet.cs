using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PubnubApi
{
    [Export(typeof(IJsonPluggableLibrary))]
    public class NewtonsoftJsonDotNet : IJsonPluggableLibrary
    {
        #region "IL2CPP workarounds"
        //Got an exception when using JSON serialisation for [],
        //IL2CPP needs to know about the array type at compile time.
        //So please define private static filed like this:
#pragma warning disable
        private static readonly System.String[][] _unused;
        private static readonly System.Int32[][] _unused2;
        private static readonly System.Int64[][] _unused3;
        private static readonly System.Int16[][] _unused4;
        private static readonly System.UInt16[][] _unused5;
        private static readonly System.UInt64[][] _unused6;
        private static readonly System.UInt32[][] _unused7;
        private static readonly System.Decimal[][] _unused8;
        private static readonly System.Double[][] _unused9;
        private static readonly System.Boolean[][] _unused91;
        private static readonly System.Object[][] _unused92;

        private static readonly long[][] _unused10;
        private static readonly int[][] _unused11;
        private static readonly float[][] _unused12;
        private static readonly decimal[][] _unused13;
        private static readonly uint[][] _unused14;
        private static readonly ulong[][] _unused15;
#pragma warning restore

        #endregion

        public NewtonsoftJsonDotNet()
        {
        }

        #region IJsonPlugableLibrary methods implementation
        private static bool IsValidJson(string jsonString, PNOperationType operationType)
        {
            bool ret = false;
            try
            {
                if (operationType == PNOperationType.PNPublishOperation
                    || operationType == PNOperationType.PNHistoryOperation
                    || operationType == PNOperationType.PNTimeOperation
                    || operationType == PNOperationType.PNPublishFileMessageOperation)
                {
                    JArray.Parse(jsonString);
                }
                else
                {
                    JObject.Parse(jsonString);
                }
                ret = true;
            }
            catch
            {
                try
                {
                    if (operationType == PNOperationType.PNPublishOperation
                        || operationType == PNOperationType.PNHistoryOperation
                        || operationType == PNOperationType.PNTimeOperation
                        || operationType == PNOperationType.PNPublishFileMessageOperation)
                    {
                        JObject.Parse(jsonString);
                        ret = true;
                    }
                }
                catch { /* igonore */ }
            }
            return ret;
        }

        public object BuildJsonObject(string jsonString)
        {
            object ret = null;

            try
            {
                var token = JToken.Parse(jsonString);
                ret = token;
            }
            catch {  /* ignore */ }

            return ret;
        }

        public bool IsDictionaryCompatible(string jsonString, PNOperationType operationType)
        {
            bool ret = false;
            if (IsValidJson(jsonString, operationType))
            {
                try
                {
                    using (StringReader strReader = new StringReader(jsonString))
                    {
                        using (JsonTextReader jsonTxtreader = new JsonTextReader(strReader))
                        {
                            while (jsonTxtreader.Read())
                            {
                                if (jsonTxtreader.LineNumber == 1 && jsonTxtreader.LinePosition == 1 && jsonTxtreader.TokenType == JsonToken.StartObject)
                                {
                                    ret = true;
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            jsonTxtreader.Close();
                        }
#if (NET35 || NET40 || NET45 || NET461)
                        strReader.Close();
#endif
                    }
                }
                catch {  /* ignore */ }
            }
            return ret;
        }

        public string SerializeToJsonString(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }

        public List<object> DeserializeToListOfObject(string jsonString)
        {
            List<object> result = JsonConvert.DeserializeObject<List<object>>(jsonString);

            return result;
        }

        public object DeserializeToObject(string jsonString)
        {
            object result = JsonConvert.DeserializeObject<object>(jsonString, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            if (result.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
            {
                JArray jarrayResult = result as JArray;
                List<object> objectContainer = jarrayResult.ToObject<List<object>>();
                if (objectContainer != null && objectContainer.Count > 0)
                {
                    for (int index = 0; index < objectContainer.Count; index++)
                    {
                        if (objectContainer[index].GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                        {
                            JArray internalItem = objectContainer[index] as JArray;
                            objectContainer[index] = internalItem.Select(item => (object)item).ToArray();
                        }
                    }
                    result = objectContainer;
                }
            }
            return result;
        }

        public void PopulateObject(string value, object target)
        {
            JsonConvert.PopulateObject(value, target);
        }

        public virtual T DeserializeToObject<T>(string jsonString)
        {
            T ret = default(T);

            try
            {
                ret = JsonConvert.DeserializeObject<T>(jsonString, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            }
            catch { /* ignore */ }

            return ret;
        }

        private bool IsGenericTypeForMessage<T>()
        {
            bool ret = false;
            //TODO:PANDU
            //PNPlatform.Print(config, pubnubLog);

            //TODO:PANDU
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, IsGenericTypeForMessage = {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ret.ToString()), config.LogVerbosity);
            return ret;
        }

        public virtual T DeserializeToObject<T>(List<object> listObject)
        {
            T ret = default(T);

            if (listObject == null)
            {
                return ret;
            }

            if (typeof(T) == typeof(PNPublishResult))
            {
                #region "PNPublishResult"
                PNPublishResult result = null;
                if (listObject.Count >= 2)
                {
                    long publishTimetoken;
                    Int64.TryParse(listObject[2].ToString(), out publishTimetoken);
                    result = new PNPublishResult
                    {
                        Timetoken = publishTimetoken
                    };
                }

                ret = (T)Convert.ChangeType(result, typeof(PNPublishResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DeserializeToObject<T>(list) => NO MATCH");
                try
                {
                    ret = (T)(object)listObject;
                }
                catch {  /* ignore */ }
            }

            return ret;
        }

        public Dictionary<string, object> DeserializeToDictionaryOfObject(string jsonString)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            }
            catch
            {
                return null;
            }
        }

        public Dictionary<string, object> ConvertToDictionaryObject(object localContainer)
        {
            Dictionary<string, object> ret = null;

            try
            {
                if (localContainer != null)
                {
                    if (localContainer.GetType().ToString() == "Newtonsoft.Json.Linq.JObject")
                    {
                        ret = new Dictionary<string, object>();

                        IDictionary<string, JToken> jsonDictionary = localContainer as JObject;
                        if (jsonDictionary != null)
                        {
                            foreach (KeyValuePair<string, JToken> pair in jsonDictionary)
                            {
                                JToken token = pair.Value;
                                ret.Add(pair.Key, ConvertJTokenToObject(token));
                            }
                        }
                    }
                    else if (localContainer.GetType().ToString() == "System.Collections.Generic.Dictionary`2[System.String,System.Object]")
                    {
                        ret = new Dictionary<string, object>();
                        Dictionary<string, object> dictionary = localContainer as Dictionary<string, object>;
                        foreach (string key in dictionary.Keys)
                        {
                            ret.Add(key, dictionary[key]);
                        }
                    }
                    else if (localContainer.GetType().ToString() == "Newtonsoft.Json.Linq.JProperty")
                    {
                        ret = new Dictionary<string, object>();

                        JProperty jsonProp = localContainer as JProperty;
                        if (jsonProp != null)
                        {
                            string propName = jsonProp.Name;
                            ret.Add(propName, ConvertJTokenToObject(jsonProp.Value));
                        }
                    }
                    else if (localContainer.GetType().ToString() == "System.Collections.Generic.List`1[System.Object]")
                    {
                        List<object> localList = localContainer as List<object>;
                        if (localList != null)
                        {
                            if (localList.Count > 0 && localList[0].GetType() == typeof(KeyValuePair<string, object>))
                            {
                                ret = new Dictionary<string, object>();
                                foreach (object item in localList)
                                {
                                    if (item is KeyValuePair<string, object> kvpItem)
                                    {
                                        ret.Add(kvpItem.Key, kvpItem.Value);
                                    }
                                    else
                                    {
                                        ret = null;
                                        break;
                                    }
                                }
                            }
                            else if (localList.Count == 1 && localList[0].GetType() == typeof(Dictionary<string, object>))
                            {
                                ret = new Dictionary<string, object>();

                                Dictionary<string, object> localDic = localList[0] as Dictionary<string, object>;
                                foreach (object item in localDic)
                                {
                                    if (item is KeyValuePair<string, object> kvpItem)
                                    {
                                        ret.Add(kvpItem.Key, kvpItem.Value);
                                    }
                                    else
                                    {
                                        ret = null;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { /* ignore */ }

            return ret;

        }

        public object[] ConvertToObjectArray(object localContainer)
        {
            object[] ret = null;

            try
            {
                if (localContainer.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                {
                    JArray jarrayResult = localContainer as JArray;
                    List<object> objectContainer = jarrayResult.ToObject<List<object>>();
                    if (objectContainer != null && objectContainer.Count > 0)
                    {
                        for (int index = 0; index < objectContainer.Count; index++)
                        {
                            if (objectContainer[index].GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                            {
                                JArray internalItem = objectContainer[index] as JArray;
                                objectContainer[index] = internalItem.Select(item => (object)item).ToArray();
                            }
                        }
                        ret = objectContainer.ToArray<object>();
                    }
                }
                else if (localContainer.GetType().ToString() == "System.Collections.Generic.List`1[System.Object]")
                {
                    List<object> listResult = localContainer as List<object>;
                    ret = listResult.ToArray<object>();
                }
            }
            catch { /* ignore */ }

            return ret;
        }

        private static object ConvertJTokenToObject(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            var jsonValue = token as JValue;
            if (jsonValue != null)
            {
                return jsonValue.Value;
            }

            var jsonContainer = token as JArray;
            if (jsonContainer != null)
            {
                List<object> jsonList = new List<object>();
                foreach (JToken arrayItem in jsonContainer)
                {
                    jsonList.Add(ConvertJTokenToObject(arrayItem));
                }
                return jsonList;
            }

            IDictionary<string, JToken> jsonObject = token as JObject;
            if (jsonObject != null)
            {
                var jsonDict = new Dictionary<string, object>();
                List<JProperty> propertyList = (from childToken in token
                                                where childToken is JProperty
                                                select childToken as JProperty).ToList();
                foreach (JProperty property in propertyList)
                {
                    jsonDict.Add(property.Name, ConvertJTokenToObject(property.Value));
                }

                return jsonDict;
            }

            return null;
        }

        private static object ConvertToDataType(Type dataType, object inputValue)
        {
            if (dataType == inputValue.GetType())
            {
                return inputValue;
            }

            object userMessage = inputValue;
            switch (dataType.FullName)
            {
                case "System.Int32":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Int32), CultureInfo.InvariantCulture);
                    break;
                case "System.Int16":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Int16), CultureInfo.InvariantCulture);
                    break;
                case "System.UInt64":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.UInt64), CultureInfo.InvariantCulture);
                    break;
                case "System.UInt32":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.UInt32), CultureInfo.InvariantCulture);
                    break;
                case "System.UInt16":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.UInt16), CultureInfo.InvariantCulture);
                    break;
                case "System.Byte":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Byte), CultureInfo.InvariantCulture);
                    break;
                case "System.SByte":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.SByte), CultureInfo.InvariantCulture);
                    break;
                case "System.Decimal":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Decimal), CultureInfo.InvariantCulture);
                    break;
                case "System.Boolean":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Boolean), CultureInfo.InvariantCulture);
                    break;
                case "System.Double":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Double), CultureInfo.InvariantCulture);
                    break;
                case "System.Char":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Char), CultureInfo.InvariantCulture);
                    break;
                case "System.String":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.String), CultureInfo.InvariantCulture);
                    break;
                case "System.Object":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Object), CultureInfo.InvariantCulture);
                    break;
                default:
                    break;
            }

            return userMessage;
        }

        #endregion

    }
}