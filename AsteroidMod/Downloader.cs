using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using SFS.Parsers.Json;
using SFS.UI.ModGUI;
using Newtonsoft.Json;

namespace AsteroidMod
{
    public class SelectorSettings
    {
        public (bool, float) diameterMin = (false, 0); // NOTE: API diameters are in KM.
        public (bool, float) diameterMax = (false, 1000);

        public int amount = 200;
        public int amountRandom = 0;

        public bool mainBeltAsteroids = true;
        public bool jupiterTrojans = false;
        // Will probably make request using full_name. Names can be found here: https://www.minorplanetcenter.net/iau/lists/MPNames
        public List<string> mustInclude = new List<string>(); 
        public (bool, string) customSearchQuery = (false, "");

        public string ToQueryURI(string fields, bool fullPrec)
        {
            if (customSearchQuery.Item1)
                return customSearchQuery.Item2;

            List<string> output = new List<string>();
            List<string> constraints = new List<string>();

            output.Add($"limit={amount}");
            output.Add($"fields={fields}");

            if (diameterMin.Item1)
                constraints.Add($"diameter|GT|{diameterMin.Item2}");
            if (diameterMax.Item1)
                constraints.Add($"diameter|LT|{diameterMax.Item2}");

            if (mainBeltAsteroids && jupiterTrojans)
                output.Add("sb-class=MBA,TJN");
            else if (mainBeltAsteroids)
                output.Add("sb-class=MBA");
            else if (jupiterTrojans)
                output.Add("sb-class=TJN");

            if (fullPrec)
                output.Add("full-prec=true");

            if (constraints.Count > 0)
            {
                string json = "{\"OR\":{\"AND\":"+JsonWrapper.ToJson(constraints, false)+"}}";
                output.Add($"sb-cdata={System.Net.WebUtility.UrlEncode(json)}");
            }

            return string.Join("&", output);
        }

        public static class SelectionUI
        {
            public static SelectorSettings ss = new SelectorSettings();
            public static Box UIHolder = null;

            public static async void CreateUI(Transform holder)
            {
                
                if (UIHolder != null)
                    GameObject.Destroy(UIHolder.gameObject);
                UIHolder = Builder.CreateBox(holder, 200, 900);
                UIHolder.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter, padding: new RectOffset(5,5,5,5));
                
                if (!await AsteroidDownloader.CheckAPIConnection())
                {
                    Builder.CreateLabel(UIHolder, 1000, 50, text: "Unable to connect to JPL's SBDB, cannot create asteroids");
                    Builder.CreateButton(UIHolder, 300, 50, onClick: () => { CreateUI(holder); }, text: "Refresh");
                }
                else
                {
                    Builder.CreateLabel(UIHolder, 1000, 50, text: "Yay!");
                }
            }
        }
    }

    public static class AsteroidDownloader
    {
        static string apiURL = "https://ssd-api.jpl.nasa.gov/sbdb_query.api";
        public static async Task<bool> CheckAPIConnection()
        {
            UnityWebRequestAsyncOperation request = UnityWebRequest.Get(apiURL).SendWebRequest();

            while (!request.isDone)
                await Task.Yield();

            return request.webRequest.result == UnityWebRequest.Result.Success;
        }

        public static async Task<APIQueryResponse> SendQuery(SelectorSettings selector, string fields, bool full_prec)
        {
            UnityWebRequestAsyncOperation request = UnityWebRequest.Get(apiURL + "?" + selector.ToQueryURI(fields, full_prec)).SendWebRequest();

            while (!request.isDone)
                await Task.Yield();

            if (request.webRequest.result != UnityWebRequest.Result.Success)
                throw new UnityException($"Asteroids Mod - Failed to recieve query results! Result: {request.webRequest.result}, Response code: {request.webRequest.responseCode}, Response: {request.webRequest.downloadHandler.text}, URL: {request.webRequest.url}");
            // Debug.Log(request.webRequest.downloadHandler.text);

            return JsonWrapper.FromJson<APIQueryResponse>(request.webRequest.downloadHandler.text);
        }
    }

    [System.Serializable]
    public class APIQueryResponse
    {
        public (string source, string version) signature;
        public List<string> fields;

        [JsonProperty("data")]
        List<List<string>> recieved_data = new List<List<string>>();
        public int count;
        [JsonIgnore]
        public List<List<object>> data;

        public bool HasField(string field) => fields.Contains(field);
        public int GetFieldIndex(string field) => fields.IndexOf(field);
        public T GetFieldFromData<T>(string field, List<object> data) => (T)data[GetFieldIndex(field)];

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            data = new List<List<object>>();
            int length = fields.Count;
            foreach (List<string> asteroidData in recieved_data)
            {
                var d = new List<object>();

                for (int i = 0; i < length; i++)
                {
                    d.Add(GetConverter(fields[i])(asteroidData[i]));
                }
                data.Add(d);
            }
        }

        public delegate object ConverterDelegate(string fieldValue);
        public static ConverterDelegate GetConverter(string fieldName)
        {
            switch (fieldName)
            {
                case "spkid" : return StringConverter;
                case "full_name" : return StringConverter;
                case "kind" : return StringConverter;
                case "pdes" : return StringConverter;
                case "name" : return StringConverter;
                case "prefix" : return StringConverter;
                case "class" : return StringConverter;
                case "neo" : return BooleanConverter;
                case "pha" : return BooleanConverter;
                case "t_jup" : return NumberConverter;
                case "moid" : return NumberConverter;
                case "moid_jup" : return NumberConverter;
                case "orbit_id" : return StringConverter;
                case "epoch" : return NumberConverter;
                case "equinox" : return StringConverter;
                case "e" : return NumberConverter;
                case "a" : return NumberConverter;
                case "q" : return NumberConverter;
                case "i" : return NumberConverter;
                case "om" : return NumberConverter;
                case "w" : return NumberConverter;
                case "ma" : return NumberConverter;
                case "tp" : return NumberConverter;
                case "per" : return NumberConverter;
                case "n" : return NumberConverter;
                case "ad" : return NumberConverter;
                case "sigma_e" : return NumberConverter;
                case "sigma_a" : return NumberConverter;
                case "sigma_q" : return NumberConverter;
                case "sigma_i" : return NumberConverter;
                case "sigma_om" : return NumberConverter;
                case "sigma_w" : return NumberConverter;
                case "sigma_tp" : return NumberConverter;
                case "sigma_ma" : return NumberConverter;
                case "sigma_per" : return NumberConverter;
                case "sigma_n" : return NumberConverter;
                case "sigma_ad" : return NumberConverter;
                case "source" : return StringConverter;
                case "soln_date" : return StringConverter;
                case "producer" : return StringConverter;
                case "data_arc" : return NumberConverter;
                case "first_obs" : return StringConverter;
                case "last_obs" : return StringConverter;
                case "n_obs_used" : return NumberConverter;
                case "n_del_obs_used" : return NumberConverter;
                case "n_dop_obs_used" : return NumberConverter;
                case "two_body" : return StringConverter;
                case "pe_used" : return StringConverter;
                case "sb_used" : return StringConverter;
                case "condition_code" : return StringConverter;
                case "rms" : return NumberConverter;
                case "A1" : return NumberConverter;
                case "A2" : return NumberConverter;
                case "A3" : return NumberConverter;
                case "DT" : return NumberConverter;
                case "S0" : return NumberConverter;
                case "A1_sigma" : return NumberConverter;
                case "A2_sigma" : return NumberConverter;
                case "A3_sigma" : return NumberConverter;
                case "DT_sigma" : return NumberConverter;
                case "S0_sigma" : return NumberConverter;
                case "H" : return NumberConverter;
                case "G" : return NumberConverter;
                case "M1" : return NumberConverter;
                case "K1" : return NumberConverter;
                case "M2" : return NumberConverter;
                case "K2" : return NumberConverter;
                case "PC" : return NumberConverter;
                case "H_sigma" : return NumberConverter;
                case "diameter" : return NumberConverter;
                case "extent" : return StringConverter;
                case "GM" : return NumberConverter;
                case "density" : return NumberConverter;
                case "rot_per" : return NumberConverter;
                case "pole" : return StringConverter;
                case "albedo" : return NumberConverter;
                case "BV" : return NumberConverter;
                case "UB" : return NumberConverter;
                case "IR" : return NumberConverter;
                case "spec_T" : return StringConverter;
                case "spec_B" : return StringConverter;
                case "diameter_sigma" : return NumberConverter;
                default : throw new UnityException("Asteroids Mod - Invalid field name!");
            }
        }
        public static ConverterDelegate StringConverter = (string fieldValue) => 
        {
            if (fieldValue != null)
                return fieldValue.Trim();
            return null;
        };
        public static ConverterDelegate NumberConverter = (string fieldValue) => 
        {
            if (fieldValue != null)
                return double.Parse(fieldValue);
            return null;
        };
        public static ConverterDelegate BooleanConverter = (string fieldValue) => fieldValue == "Y";
    }
}