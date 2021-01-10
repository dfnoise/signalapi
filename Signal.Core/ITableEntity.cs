﻿using System;
using System.Collections.Generic;

namespace Signal.Core
{
    public interface ITableEntityKey
    {
        string PartitionKey { get; }

        string RowKey { get; }
    }

    public class TableEntityKey : ITableEntityKey
    {
        public TableEntityKey(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public string PartitionKey { get; }
        
        public string RowKey { get; }
    }
    
    public interface ITableEntity : ITableEntityKey
    {
    }

    public interface IUserAssignedEntitiesTableEntry : ITableEntity
    {
        public IEnumerable<string> Devices { get; }
    }

    public enum UserData
    {
        AssignedEntities
    }

    public class UserAssignedEntitiesTableEntry : IUserAssignedEntitiesTableEntry
    {
        public UserAssignedEntitiesTableEntry(string userId, UserData data, IEnumerable<string> deviceIds)
        {
            this.PartitionKey = userId;
            this.RowKey = data.ToString();
            this.Devices = deviceIds;
        }

        public string PartitionKey { get; }

        public string RowKey { get; }

        public IEnumerable<string> Devices { get; }
    }

    public interface IDeviceTableEntity : ITableEntity
    {
        public string DeviceIdentifier { get; set; }

        public string Alias { get; set; }
    }

    public class DeviceTableEntity : IDeviceTableEntity
    {
        public DeviceTableEntity(string deviceId, string deviceIdentifier, string alias)
        {
            this.PartitionKey = "device";
            this.RowKey = deviceId;
            this.DeviceIdentifier = deviceIdentifier;
            this.Alias = alias;
        }

        public string PartitionKey { get; }
        public string RowKey { get; }
        public string DeviceIdentifier { get; set; }
        public string Alias { get; set; }
    }

    public interface IDeviceStateHistoryTableEntity : ITableEntity
    {
        string? ValueSerialized { get; }
    }
    
    public class DeviceStateHistoryTableEntity : IDeviceStateHistoryTableEntity
    {
        public DeviceStateHistoryTableEntity(
            string deviceId,
            string channelName,
            string contactName,
            string? valueSerialized,
            DateTime timeStamp)
        {
            this.PartitionKey = $"{deviceId}-{channelName}-{contactName}";
            this.RowKey = timeStamp.ToString("O");
            this.ValueSerialized = valueSerialized;
        }
        
        public string PartitionKey { get; }
        
        public string RowKey { get; }
        
        public string? ValueSerialized { get; }
    }
    
    public interface IDeviceStateTableEntity : ITableEntity
    {
        string ChannelName { get; }
        
        string ContactName { get; }
        
        string? ValueSerialized { get; }
        
        DateTime TimeStamp { get; }
    }

    public class DeviceStateTableEntity : IDeviceStateTableEntity
    {
        public DeviceStateTableEntity(
            string deviceId,
            string channelName, 
            string contactName, 
            string? valueSerialized, 
            DateTime timeStamp)
        {
            this.PartitionKey = $"{deviceId}";
            this.RowKey = $"{channelName}-{contactName}";
            this.ChannelName = channelName;
            this.ContactName = contactName;
            this.ValueSerialized = valueSerialized;
            this.TimeStamp = timeStamp;
        }

        public string PartitionKey { get; }
        
        public string RowKey { get; }
        
        public string ChannelName { get; }
        
        public string ContactName { get; }
        
        public string? ValueSerialized { get; }
        
        public DateTime TimeStamp { get; }
    }
}