using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NordInvasion.Utils
{
    /// <summary>
    /// Минимальный JSON-парсер (без внешних зависимостей - в Bannerlord нечего подключать).
    /// Поддерживает объекты, массивы, строки, числа, bool, null.
    /// Result: Dictionary&lt;string,object&gt; / List&lt;object&gt; / string / double / bool / null.
    /// </summary>
    public static class NIJson
    {
        public static object Parse(string json)
        {
            int i = 0;
            SkipWs(json, ref i);
            if (i >= json.Length) return null;
            return ParseValue(json, ref i);
        }

        public static Dictionary<string, object> ParseObject(string json)
        {
            var v = Parse(json) as Dictionary<string, object>;
            return v ?? new Dictionary<string, object>();
        }

        public static string GetString(Dictionary<string, object> obj, string key, string def = "")
        {
            object v;
            if (obj != null && obj.TryGetValue(key, out v) && v != null) return v.ToString();
            return def;
        }

        public static int GetInt(Dictionary<string, object> obj, string key, int def = 0)
        {
            object v;
            if (obj == null || !obj.TryGetValue(key, out v) || v == null) return def;
            double d;
            if (double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return (int)d;
            return def;
        }

        /// <summary>Поле-массив строк: ["a","b"] -> string[]. Не массив -> пустой.</summary>
        public static string[] GetStringArray(Dictionary<string, object> obj, string key)
        {
            object v;
            if (obj == null || !obj.TryGetValue(key, out v)) return new string[0];
            if (!(v is List<object>)) return new string[0];
            var list = (List<object>)v;
            var res = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                res[i] = list[i] != null ? list[i].ToString() : "";
            return res;
        }

        /// <summary>Поле-массив чисел: [1,2] -> int[].</summary>
        public static int[] GetIntArray(Dictionary<string, object> obj, string key)
        {
            object v;
            if (obj == null || !obj.TryGetValue(key, out v)) return new int[0];
            if (!(v is List<object>)) return new int[0];
            var list = (List<object>)v;
            var res = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                double d;
                res[i] = list[i] != null && double.TryParse(list[i].ToString(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out d) ? (int)d : 0;
            }
            return res;
        }

        /// <summary>Поле-массив объектов: [{"a":1},{"a":2}] -> список словарей.</summary>
        public static List<Dictionary<string, object>> GetObjectArray(Dictionary<string, object> obj, string key)
        {
            var res = new List<Dictionary<string, object>>();
            object v;
            if (obj == null || !obj.TryGetValue(key, out v)) return res;
            var list = v as List<object>;
            if (list == null) return res;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i] as Dictionary<string, object>;
                if (row != null) res.Add(row);
            }
            return res;
        }

        /// <summary>Поле-bool (true/1).</summary>
        public static bool GetBool(Dictionary<string, object> obj, string key, bool def = false)
        {
            object v;
            if (obj == null || !obj.TryGetValue(key, out v) || v == null) return def;
            var s2 = v.ToString();
            if (s2 == "1" || s2 == "true" || s2 == "True") return true;
            if (s2 == "0" || s2 == "false" || s2 == "False") return false;
            return def;
        }

        /// <summary>Ответ-массив объектов: [{...},{...}] (например /api/campaign/villages).</summary>
        public static List<Dictionary<string, object>> ParseObjectArray(string json)
        {
            var res = new List<Dictionary<string, object>>();
            var v = Parse(json);
            var list = v as List<object>;
            if (list == null) return res;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i] as Dictionary<string, object>;
                if (row != null) res.Add(row);
            }
            return res;
        }

        // ===== internals =====

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
        }

        static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) return null;
            char c = s[i];
            if (c == '{') return ParseObj(s, ref i);
            if (c == '[') return ParseArr(s, ref i);
            if (c == '"') return ParseStr(s, ref i);
            if (c == 't' || c == 'f') return ParseBool(s, ref i);
            if (c == 'n') { i += 4; return null; } // null
            return ParseNum(s, ref i);
        }

        static Dictionary<string, object> ParseObj(string s, ref int i)
        {
            var dict = new Dictionary<string, object>();
            i++; // {
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return dict; }
            while (i < s.Length)
            {
                SkipWs(s, ref i);
                if (i >= s.Length) break;
                if (s[i] != '"') { i++; continue; } // повреждённый ввод - пропускаем
                string key = ParseStr(s, ref i);
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ':') i++;
                object val = ParseValue(s, ref i);
                dict[key] = val;
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; break; }
                if (i >= s.Length) break;
            }
            return dict;
        }

        static List<object> ParseArr(string s, ref int i)
        {
            var list = new List<object>();
            i++; // [
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (i < s.Length)
            {
                object val = ParseValue(s, ref i);
                list.Add(val);
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; break; }
                if (i >= s.Length) break;
            }
            return list;
        }

        static string ParseStr(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // opening quote
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c == '\\' && i < s.Length)
                {
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'u':
                            if (i + 4 <= s.Length)
                            {
                                string hex = s.Substring(i, 4);
                                i += 4;
                                int code;
                                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                                    sb.Append((char)code);
                            }
                            break;
                        default: sb.Append(e); break;
                    }
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        static object ParseNum(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
            double d;
            if (i > start && double.TryParse(s.Substring(start, i - start), NumberStyles.Any,
                CultureInfo.InvariantCulture, out d))
                return d;
            return 0.0;
        }

        static bool ParseBool(string s, ref int i)
        {
            if (s.StartsWith("true", i)) { i += 4; return true; }
            i += 5; // false
            return false;
        }
    }
}
