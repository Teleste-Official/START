using SmartTrainApplication.Data;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartTrainApplication.Models
{
    /// <summary>
    /// The railway route that the Train will follow
    /// <list type="bullet">
    /// <item>(string) Name</item>
    /// <item>(List of RouteCoordinate) Coords</item>
    /// </list>
    /// </summary>
    public class TrainRoute
    {
        [JsonIgnore]
        public string Id { get; set; }
        [JsonIgnore]
        public string FilePath { get; set; }
        [JsonIgnore]
        public bool Edited { get; set; }
        public string Name { get; set; }
        public List<RouteCoordinate> Coords { get; set; }
        [JsonIgnore]
        public string Specifier = "Route";

        public TrainRoute() { }

        public TrainRoute(string name, List<RouteCoordinate> coords, string ID = "", string filePath = "")
        {
            Name = name;
            Coords = coords;
            if (ID == "")
                Id = DataManager.CreateID();
            else
                Id = ID;

            if (filePath == "")               
                FilePath = DataManager.CreateFilePath(Id, Specifier);
            else
                FilePath = filePath;

            Edited = false;
        }
    }
}
