using SmartTrainApplication.Data;
using System.Text.Json.Serialization;

namespace SmartTrainApplication.Models
{
    /// <summary>
    /// The Train that will move along the railway TrainRoutes
    /// <list type="bullet">
    /// <item>(string) Name</item>
    /// <item>(string) Description</item>
    /// <item>(float) MaxSpeed</item>
    /// <item>(int) Icon</item>
    /// </list>
    /// </summary>
    public class Train
    {
        [JsonIgnore]
        public string Id { get; set; }
        [JsonIgnore]
        public string FilePath { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float MaxSpeed { get; set; }
        public float Acceleration { get; set; }
        public int Icon {  get; set; }

        public Train() { }

        public Train(string name, string description, float maxSpeed, float acceleration, int icon, string ID = "", string filePath = "")
        {
            Name = name;
            Description = description;
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            Icon = icon;
            if (ID == "")
                Id = DataManager.CreateID();
            else
                Id = ID;
            FilePath = filePath;
        }

        public void SetValues(Train NewTrain)
        {
            Name = NewTrain.Name;
            Description = NewTrain.Description;
            MaxSpeed = NewTrain.MaxSpeed;
            Acceleration = NewTrain.Acceleration;
            Icon = NewTrain.Icon;
        }
    }
}
