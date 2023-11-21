using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    public class Train
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float MaxSpeed { get; set; }
        public int Icon {  get; set; }

        public Train() { }

        public Train(string name, string description, float maxSpeed)
        {
            Name = name;
            Description = description;
            MaxSpeed = maxSpeed;
            Icon = 0;
        }
    }
}
