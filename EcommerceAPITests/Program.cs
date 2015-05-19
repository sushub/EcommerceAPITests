using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;

namespace EcommerceAPITests
{
    class Program
    {
        private const string apiUri = "http://api.data.gov/nrel/alt-fuel-stations/v1/";
        private const string apiKey = "X4AF9hazjyWhQuhh4Odc9PTGyyQVe8ujqLjo8S4E";

        static void Main(string[] args)
        {
            int stationId= searchStationTest();
            //using the stationId returned by Nearest station api to find station address.
            if (stationId != 0)
            {
                verifyStationAddressTest(stationId);
            }
            Console.ReadLine();      
        }
        
        #region JsonFieldMethods
        //As per JSON fields documented here: https://api.data.gov/docs/nrel/transportation/alt-fuel-stations-v1/ ,Created classes for each node.

        public class Precision
        {
            public string name { get; set; }
            public int value { get; set; }
            public IList<string> types { get; set; }
        }

        public class Fuel
        {
            public int Total_results { get; set; }
            public int Offset { get; set; }
            public string Station_locator_url { get; set; }
            public decimal latitude { get; set; }
            public decimal longitude { get; set; }
            public Precision PrecisionValue { get; set; }
            public IList<Fuel_Stations> Fuel_Stations { get; set; }
        }

        public class FederalAgency
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Fuel_Stations
        {
            public string Fuel_type_code { get; set; }
            public string Station_name { get; set; }
            public string Street_address { get; set; }
            public string intersection_directions { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string zip { get; set; }
            public string plus4 { get; set; }
            public string station_phone { get; set; }
            public string status_code { get; set; }
            public int? expected_date { get; set; }
            public string groups_with_access_code { get; set; }
            public string access_days_time { get; set; }
            public string cards_accepted { get; set; }
            public string owner_type_code { get; set; }
            public IList<FederalAgency> federal_agency { get; set; }
            public string bd_blends { get; set; }
            public bool? e85_blender_pump { get; set; }
            public bool? lpg_primary { get; set; }
            public string ng_fill_type_code { get; set; }
            public string ng_psi { get; set; }
            public string ng_vehicle_class { get; set; }
            public int? ev_level1_evse_num { get; set; }
            public int? ev_level2_evse_num { get; set; }
            public int? ev_dc_fast_num { get; set; }
            public string ev_other_evse { get; set; }
            public string ev_network { get; set; }
            public string ev_network_web { get; set; }
            public string hy_status_link { get; set; }
            public string geocode_status { get; set; }
            public decimal latitude { get; set; }
            public decimal longitude { get; set; }
            public int? open_date { get; set; }
            public string date_last_confirmed { get; set; }
            public string updated_at { get; set; }
            public int id { get; set; }
            public decimal distance { get; set; }
        }     
      #endregion        
      #region TestMethods
      /// <summary>
      /// Query for nearest stations to Austin, TX that are part of the “ChargePoint Network” and return the Station Id of the HYATT AUSTIN station.
      /// </summary>      
    static int searchStationTest()
    {
        int stationid = 0;
        string stringExpected = "HYATT AUSTIN";
        var url = apiUri + "nearest.json?";
        var urlParams = new NameValueCollection();
        urlParams.Add("api_key", apiKey);
        urlParams.Add("location","Austin, TX");
        urlParams.Add("fuel_type", "ELEC");
        urlParams.Add("ev_network", "ChargePoint Network");
        string searchQueryString = ToQueryString(urlParams);
        string searchString = string.Concat(url, searchQueryString);             
        var fuel = GetData<Fuel>(searchString); 
        
        for (int i=0; i<fuel.Fuel_Stations.Count; i++)
        {            
            if (fuel.Fuel_Stations[i].Station_name.ToLower() == stringExpected.ToLower())
            {
                resultLogger("searchStationTest Passed: Found the desired station :" + fuel.Fuel_Stations[i].Station_name);
                stationid = fuel.Fuel_Stations[i].id;
                return stationid;
            }
        }
        resultLogger("searchStationTest failed:No stations found");
        return stationid;
    }

    /// <summary>
    /// API to return the Street Address of that station. Verfy the Station address for the specified station id
    /// </summary>
    /// <param name="stationid">Use the Station ID from previous test</param>   
    static void verifyStationAddressTest(int stationid)
    {
        string apiUriFormat = apiUri + stationid.ToString() + "." + "json" + "?";       
        var searchUrlParams = new NameValueCollection();        
        searchUrlParams.Add("api_key", apiKey);
        string searchQueryString = ToQueryString(searchUrlParams);
        string searchString = string.Concat(apiUriFormat, searchQueryString);
        var searchQueryUri = new Uri(searchString);       
        string fuelStationRecord = GetProviderResults(searchQueryUri);
        // Set the Expected Address 
        string expectedValue_streetAddress = "208 Barton Springs" ;
        string expectedValue_City = "Austin" ;
        string expectedValue_Zipcode = "78704";
     
        if (fuelStationRecord.Contains(expectedValue_streetAddress) == true && fuelStationRecord.Contains(expectedValue_City) == true && fuelStationRecord.Contains(expectedValue_Zipcode) == true)
        {
            resultLogger("verifyStationAddressTest Passed:Station address matches for given stationid");
        }
        else
        {
            resultLogger(" verifyStationAddressTest Failed: No Address matched");
        }
    }
 #endregion
    #region UtilitiesMethods
    /// <summary>
    /// Method to de-serialize the Json 
    /// </summary>
    /// <param name="url">provider url to make the call</param>
    /// returns the de-serialized json
    private static T GetData<T>(string url) where T : new()
    {
        using (var requestData = new WebClient())
        {
            var responseData = string.Empty;          
            try
            {
                responseData = requestData.DownloadString(url);
            }
            catch (Exception)
            {
            }
            //Serialize Json data , if response string is not empty
            return !string.IsNullOrEmpty(responseData) ? JsonConvert.DeserializeObject<T>(responseData) : new T();
        }
    }

    /// <summary>
    /// Get the response from provider of the API services
    /// </summary>
    /// <param name="providerUri">provider uri to make the call</param>
    /// <returns>return the results as string or null if empty</returns>
    public static string GetProviderResults(Uri providerUri)
    {
        WebClient newRequest = new WebClient();

        //Get the Response 
        string fuelStationRecord = newRequest.DownloadString(providerUri);
        return fuelStationRecord;
    }

    /// <summary>
    /// Builds the QueryString to pass it to the Service call 
    /// </summary>
    /// <param name="source">Name Value Collection of query parameters</param>
    /// <returns>return the results as string</returns>
    public static string ToQueryString(NameValueCollection source)
    {
        return source != null ? String.Join("&", source.AllKeys.Where(key => source.GetValues(key).Any(value => !String.IsNullOrEmpty(value)))
            .SelectMany(key => source.GetValues(key)
                .Where(value => !String.IsNullOrEmpty(value))
                .Select(value => String.Format("{0}={1}", key, value ?? string.Empty)))
            .ToArray())
            : string.Empty;
    }

    /// <summary>
    /// Displays the Test Results to console and can be extended later for logging as well
    /// </summary>
    /// <param name="result">output to display on the console</param>   
    static void resultLogger(string result)
    {
        Console.WriteLine(result);
    }
    #endregion
  }
}