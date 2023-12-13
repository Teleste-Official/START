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
        public string Name { get; set; }
        public List<RouteCoordinate> Coords { get; set; }

        public TrainRoute() { }

        public TrainRoute(string name, List<RouteCoordinate> coords, string ID = "", string filePath = "")
        {
            Name = name;
            Coords = coords;
            if (ID == "")
                Id = DataManager.CreateID();
            else
                Id = ID;
            FilePath = filePath;
        }
    }
}
