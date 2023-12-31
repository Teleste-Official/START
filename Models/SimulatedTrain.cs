﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    public class SimulatedTrain : Train
    {
        public float Odometer {  get; set; }
        public float CurrentSpeed { get; set; }

        public SimulatedTrain() { }

        public SimulatedTrain(string name, string description, float maxSpeed)
        {
            Name = name;
            Description = description;
            MaxSpeed = maxSpeed;
            Icon = 0;
            Odometer = 0;
            CurrentSpeed = 0;
        }
    }
}
