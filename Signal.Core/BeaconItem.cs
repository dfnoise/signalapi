﻿using System;

namespace Signal.Core
{
    public class BeaconItem : ITableEntity
    {
        public BeaconItem(string userId, string beaconId)
        {
            this.PartitionKey = userId;
            this.RowKey = beaconId;
        }
        
        public string PartitionKey { get; }
        
        public string RowKey { get; }
        
        public string? Alias { get; set; }
        
        public string? RefreshToken { get; set; }
        
        public DateTime RegisteredTimeStamp { get; set; }
    }
}