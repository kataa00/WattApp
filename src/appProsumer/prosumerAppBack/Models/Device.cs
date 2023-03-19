﻿using System;
namespace prosumerAppBack.Models
{
	public class Device
	{
		public Guid ID { get; set; }
		public string? Name { get; set; }
		public string? OwnerName { get; set; }
		public string? Manufacurer { get; set; }
		public double Wattage { get; set; }
		public double UsageFrequency { get; set; }
		public string? MacAdress { get; set; }
		public TimeSpan UsageTime { get; set; }
		public int DeviceAge { get; set; }
    }
}
