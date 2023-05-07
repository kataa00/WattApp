using System.Linq;
using Internal;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using prosumerAppBack.BusinessLogic;
using prosumerAppBack.Models;
using prosumerAppBack.Models.Device;

namespace prosumerAppBack.DataAccess;

public class PowerUsageRepository : IPowerUsageRepository
{
    private readonly IMongoCollection<PowerUsage> mongoCollection;
    private readonly DataContext _dataContext;
    private readonly IDeviceRepository _deviceRepository;

    public PowerUsageRepository(MongoDataContext mongoDataContext, DataContext dataContext, IDeviceRepository deviceRepository)
    {
        mongoCollection = mongoDataContext.PowerUsage;
        _dataContext = dataContext;
        _deviceRepository = deviceRepository;
    }
    public double GetForDevice(Guid deviceID)
    {
        DateTime currentHourTimestamp = DateTime.Now.Date.AddHours(DateTime.Now.Hour);

        bool isOn = _dataContext.Devices
                    .Where(d => d.ID == deviceID)
                    .Select(ison => ison.IsOn)
                    .FirstOrDefault();

        if(isOn == false)
        {
            return 0;
        }

        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceID)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();

        var powerUsageData = mongoCollection.AsQueryable()
            .FirstOrDefault(d => d.ID.ToString().ToUpper() == deviceTypeID.ToString().ToUpper());

        var data = powerUsageData.TimestampPowerPairs.Find(p => p.Timestamp == currentHourTimestamp);
        
        if (data != null)
            return data.PowerUsage;
        
        return -1;
    }

    public double AveragePowerUsageProduction(Guid userID)
    {
        var devices = _deviceRepository.GetDevicesForUser(userID);

        double sum = 0;

        foreach (var device in devices)
        {
            Guid deviceTypeID = _dataContext.Devices
           .Where(d => d.ID == device.ID)
           .Select(d => d.DeviceTypeID)
           .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                .Where(g => g.ID == _dataContext.DeviceTypes
                    .Where(dt => dt.ID == deviceTypeID)
                    .Select(dt => dt.GroupID)
                    .FirstOrDefault())
                .Select(g => g.Name)
                .FirstOrDefault();

            if (deviceGroupName == null)
            {
                return 0;
            }

            if (deviceGroupName == "Producer" && device.IsOn == true)
            {
                foreach (var VARIABLE in devices)
                {
                    sum += GetForDevice(VARIABLE.ID);
                }
            }          
        }
        return sum / devices.Count();
    }

    public double AveragePowerUsageConsumption(Guid userID)
    {
        var devices = _deviceRepository.GetDevicesForUser(userID);

        double sum = 0;

        foreach (var device in devices)
        {
            Guid deviceTypeID = _dataContext.Devices
           .Where(d => d.ID == device.ID)
           .Select(d => d.DeviceTypeID)
           .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                .Where(g => g.ID == _dataContext.DeviceTypes
                    .Where(dt => dt.ID == deviceTypeID)
                    .Select(dt => dt.GroupID)
                    .FirstOrDefault())
                .Select(g => g.Name)
                .FirstOrDefault();


            if(deviceGroupName == null)
            {
                return 0;
            }

            if (deviceGroupName == "Consumer" && device.IsOn == true)
            {              
                    sum += GetForDevice(device.ID);
            }
        }
        return sum / devices.Count();
    }

    public double GetPowerUsageForDay(Guid deviceID, DateTime today)
    {
        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceID)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();

            var powerUsageData = mongoCollection
                .AsQueryable()
                .FirstOrDefault(p => p.ID.ToString() == deviceTypeID.ToString().ToUpper());

            if (powerUsageData == null)
            {
                return 0;
            }

            double totalPowerUsage = powerUsageData.TimestampPowerPairs
                .Where(pair => pair.Timestamp.Date == today)
                .Sum(pair => pair.PowerUsage);
            return totalPowerUsage;

    }


    public PowerUsage GetPowerUsageFor7Days(Guid deviceId, int direction)
    {
        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceId)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();

            var powerUsage = new PowerUsage();
            powerUsage.TimestampPowerPairs = new List<TimestampPowerPair>();
            var today = DateTime.Today;

            for (int i = 1; i <= 7; i++)
            {
                var day = today.AddDays(i * direction);
                var powerUsageD = GetPowerUsageForDay(deviceId, day);
                var ts = new TimestampPowerPair();
                ts.PowerUsage = powerUsageD;
                ts.Timestamp = day;
                powerUsage.TimestampPowerPairs.Add(ts);

            }

            if (direction == -1)
                powerUsage.TimestampPowerPairs.Reverse();

        return powerUsage;
    }

    public PowerUsage GetPowerUsageForAMonth(Guid deviceId, int direction)
    {
        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceId)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();

        var powerUsage = new PowerUsage();
        powerUsage.TimestampPowerPairs = new List<TimestampPowerPair>();
        var today = DateTime.Today;

        for (int i = 1; i <= 31; i++)
        {
            var day = today.AddDays(i * direction);
            var powerUsageD = GetPowerUsageForDay(deviceId, day);
            var ts = new TimestampPowerPair();
            ts.PowerUsage = powerUsageD;
            ts.Timestamp = day;
            powerUsage.TimestampPowerPairs.Add(ts);

        }

        if (direction == -1)
            powerUsage.TimestampPowerPairs.Reverse();

        return powerUsage;
    }

    public double CurrentSumPowerUsageConsumption(Guid userID)
    {
        double sum = 0;
        DateTime currentHourTimestamp = DateTime.Now.Date.AddHours(DateTime.Now.Hour);

        var devicesTypes = _deviceRepository.GetDevicesForUser(userID);
        foreach(var device in devicesTypes)
        {
            Guid deviceTypeID = _dataContext.Devices
               .Where(d => d.ID == device.ID)
               .Select(d => d.DeviceTypeID)
               .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                .Where(g => g.ID == _dataContext.DeviceTypes
                    .Where(dt => dt.ID.ToString().ToUpper() == deviceTypeID.ToString().ToUpper())
                    .Select(dt => dt.GroupID)
                    .FirstOrDefault())
                .Select(g => g.Name)
                .FirstOrDefault();

            if (deviceGroupName == "Consumer" && device.IsOn == true)
            {
                var powerUsageData = mongoCollection.AsQueryable()
                    .Where(p => p.ID.ToString().ToUpper() == deviceTypeID.ToString().ToUpper())
                    .ToList()
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(t => t.Timestamp == currentHourTimestamp);

                sum += powerUsageData.Sum(p => p.PowerUsage);
            }
        } 
        return sum;
    }

    public double CurrentSumPowerUsageProduction(Guid userID)
    {
        double sum = 0;
        DateTime currentHourTimestamp = DateTime.Now.Date.AddHours(DateTime.Now.Hour);

        var devicesTypes = _deviceRepository.GetDevicesForUser(userID);
        foreach (var device in devicesTypes)
        {
            Guid deviceTypeID = _dataContext.Devices
               .Where(d => d.ID == device.ID)
               .Select(d => d.DeviceTypeID)
               .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                .Where(g => g.ID == _dataContext.DeviceTypes
                    .Where(dt => dt.ID.ToString().ToUpper() == deviceTypeID.ToString().ToUpper())
                    .Select(dt => dt.GroupID)
                    .FirstOrDefault())
                .Select(g => g.Name)
                .FirstOrDefault();

            if (deviceGroupName == "Producer" && device.IsOn == true)
            {
                var powerUsageData = mongoCollection.AsQueryable()
                    .Where(p => p.ID.ToString().ToUpper() == deviceTypeID.ToString().ToUpper())
                    .ToList()
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(t => t.Timestamp == currentHourTimestamp);

                sum += powerUsageData.Sum(p => p.PowerUsage);
            }
        }
        return sum;
    }

    public double CurrentSumPowerUsageSystemProducer()
    {
        DateTime currentHourTimestamp = DateTime.Now.AddHours(-1);
        double sum = 0;

        var powerUsageData = mongoCollection.AsQueryable()
           .Select(d => d.ID)
           .ToList();

        foreach (var device in powerUsageData)
        {

            string deviceGroupName = _dataContext.DeviceGroups
           .Where(g => g.ID == _dataContext.DeviceTypes
               .Where(dt => dt.ID == device)
               .Select(dt => dt.GroupID)
               .FirstOrDefault())
           .Select(g => g.Name)
           .FirstOrDefault();

            bool isOn = _dataContext.Devices
                    .Where(d => d.DeviceTypeID.ToString().ToUpper() == device.ToString().ToUpper())
                    .Select(ison => ison.IsOn)
                    .FirstOrDefault();

            if (deviceGroupName == "Producer" && isOn == true)
            {
                sum += GetCurrentPowerUsage(currentHourTimestamp, device);
            }
        }    

        return sum;
    }

    public double CurrentSumPowerUsageSystemConsumer()
    {
        DateTime currentHourTimestamp = DateTime.Now.AddHours(-1);
        double sum = 0;

        var powerUsageData = mongoCollection.AsQueryable()
           .Select(d => d.ID)
           .ToList();

        foreach (var device in powerUsageData)
        {

            string deviceGroupName = _dataContext.DeviceGroups
           .Where(g => g.ID == _dataContext.DeviceTypes
               .Where(dt => dt.ID == device)
               .Select(dt => dt.GroupID)
               .FirstOrDefault())
           .Select(g => g.Name)
           .FirstOrDefault();

            bool isOn = _dataContext.Devices
                    .Where(d => d.DeviceTypeID.ToString().ToUpper() == device.ToString().ToUpper())
                    .Select(ison => ison.IsOn)
                    .FirstOrDefault();

            if (deviceGroupName == "Consumer" && isOn == true)
            {
                sum += GetCurrentPowerUsage(currentHourTimestamp, device);
            }
        }

        return sum;
    }

    public IEnumerable<TimestampPowerPair> GetForDeviceByHour(Guid deviceID)
    {
        DateTime currentDay = DateTime.Today;
        DateTime currentHour = DateTime.Now.AddHours(-1);

        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceID)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();


        var powerUsageData = mongoCollection.AsQueryable()
            .FirstOrDefault(p => p.ID.ToString() == deviceTypeID.ToString().ToUpper())
            ?.TimestampPowerPairs
            .Where(t => t.Timestamp.Date == currentDay && t.Timestamp <= currentHour);

        return powerUsageData;
    }

    public double GetPowerUsageForAMonthSystemConsumer(int direction)
    {
        var startOfMonth = DateTime.Now.AddDays(-DateTime.Now.Day + 1).AddMonths(direction); // pocetak proslog meseca (npr 04.05.)
        var endOfMonth = startOfMonth.AddMonths(1); // (04. 06.)

        var deviceTypes = mongoCollection.AsQueryable().ToList();

        double powerUsages = 0;
        foreach (var device in deviceTypes)
        {
            string deviceGroupName = _dataContext.DeviceGroups
               .Where(g => g.ID == _dataContext.DeviceTypes
                   .Where(dt => dt.ID.ToString().ToUpper() == device.ID.ToString().ToUpper())
                   .Select(dt => dt.GroupID)
                   .FirstOrDefault())
               .Select(g => g.Name)
               .FirstOrDefault();


            if (deviceGroupName == "Consumer")
            {
                 powerUsages = deviceTypes
                .Sum(p => p.TimestampPowerPairs.Where(t => t.Timestamp >= startOfMonth && t.Timestamp <= endOfMonth).Sum(p => p.PowerUsage));

            }
        }     
        return powerUsages;
    }

    public double GetPowerUsageForAMonthSystemProducer(int direction)
    {
        var startOfMonth = DateTime.Now.AddDays(-DateTime.Now.Day + 1).AddMonths(direction); // pocetak proslog meseca (npr 04.05.)
        var endOfMonth = startOfMonth.AddMonths(1); // (04. 06.)

        var deviceTypes = mongoCollection.AsQueryable().ToList();

        double powerUsages = 0;
        foreach (var device in deviceTypes)
        {
            string deviceGroupName = _dataContext.DeviceGroups
               .Where(g => g.ID == _dataContext.DeviceTypes
                   .Where(dt => dt.ID.ToString().ToUpper() == device.ID.ToString().ToUpper())
                   .Select(dt => dt.GroupID)
                   .FirstOrDefault())
               .Select(g => g.Name)
               .FirstOrDefault();


            if (deviceGroupName == "Producer")
            {
                powerUsages = deviceTypes
               .Sum(p => p.TimestampPowerPairs.Where(t => t.Timestamp >= startOfMonth && t.Timestamp <= endOfMonth).Sum(p => p.PowerUsage));

            }
        }
        return powerUsages;
    }

    public List<PowerUsage> GetPowerUsageSumByDeviceConsumer(int direction)
    {
        var startOfMonth = DateTime.Now.AddMonths(direction);
        var endOfMonth = startOfMonth.AddMonths(1);

        var deviceTypes = mongoCollection.AsQueryable().ToList();

        List<PowerUsage> listPU = new List<PowerUsage>();

        foreach (var device in deviceTypes)
        {
            double sum = 0;
            PowerUsage sums;

            string deviceGroupName = _dataContext.DeviceGroups
               .Where(g => g.ID == _dataContext.DeviceTypes
                   .Where(dt => dt.ID.ToString().ToUpper() == device.ID.ToString().ToUpper())
                   .Select(dt => dt.GroupID)
                   .FirstOrDefault())
               .Select(g => g.Name)
               .FirstOrDefault();

            if (deviceGroupName == "Consumer")
            {
                sums = new PowerUsage();
                sums.TimestampPowerPairs = new List<TimestampPowerPair>();
                sums.ID = device.ID;

                var powerUsageData = deviceTypes
                    .Where(p => device.ID.ToString().ToUpper() == p.ID.ToString().ToUpper())
                    .ToList()
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(t => t.Timestamp >= startOfMonth && t.Timestamp <= endOfMonth);

                sum = powerUsageData.Sum(p => p.PowerUsage);

                var tsp = new TimestampPowerPair();
                tsp.PowerUsage = sum;
                sums.TimestampPowerPairs.Add(tsp);
                listPU.Add(sums);
            }
        }

        return listPU;
    }

    public List<PowerUsage> GetPowerUsageSumByDeviceProducer(int direction)
    {
        var startOfMonth = DateTime.Now.AddMonths(direction);
        var endOfMonth = startOfMonth.AddMonths(1);

        var deviceTypes = mongoCollection.AsQueryable().ToList();

        List<PowerUsage> listPU = new List<PowerUsage>();

        foreach (var device in deviceTypes)
        {
            double sum = 0;
            PowerUsage sums;

            string deviceGroupName = _dataContext.DeviceGroups
               .Where(g => g.ID == _dataContext.DeviceTypes
                   .Where(dt => dt.ID.ToString().ToUpper() == device.ID.ToString().ToUpper())
                   .Select(dt => dt.GroupID)
                   .FirstOrDefault())
               .Select(g => g.Name)
               .FirstOrDefault();

            if (deviceGroupName == "Producer")
            {
                sums = new PowerUsage();
                sums.TimestampPowerPairs = new List<TimestampPowerPair>();
                sums.ID = device.ID;

                var powerUsageData = deviceTypes
                    .Where(p => device.ID.ToString().ToUpper() == p.ID.ToString().ToUpper())
                    .ToList()
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(t => t.Timestamp >= startOfMonth && t.Timestamp <= endOfMonth);

                sum = powerUsageData.Sum(p => p.PowerUsage);

                var tsp = new TimestampPowerPair();
                tsp.PowerUsage = sum;
                sums.TimestampPowerPairs.Add(tsp);
                listPU.Add(sums);
            }
        }

        return listPU;
    }

    public PowerUsage GetPowerUsagesForEachDayConsumptionMonth(int direction)
    {
        var devices = _dataContext.Devices.Select(d => d.ID);

        PowerUsage pu = null;

        foreach (var device in devices)
        {
            var deviceType = _dataContext.Devices
                            .Where(d => d.ID == device)
                            .Select(d => d.DeviceTypeID)
                            .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceType)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if( deviceGroupName == "Consumer")
            {
                double sum = 0;
                var pu2 = GetPowerUsageForAMonth(device, direction);
                if (pu == null)
                {
                    pu = pu2;
                    continue;
                }
                foreach (var tsp in pu2.TimestampPowerPairs)
                {
                    foreach (var y in pu.TimestampPowerPairs)
                    {
                        if (y.Timestamp == tsp.Timestamp)
                            y.PowerUsage += tsp.PowerUsage;
                    }
                }
            }
        }

        return pu;
    }

    public PowerUsage GetPowerUsagesForEachDayConsumptionWeek(int direction)
    {
        var devices = _dataContext.Devices.Select(d => d.ID);

        PowerUsage pu = null;

        foreach (var device in devices)
        {
            var deviceType = _dataContext.Devices
                            .Where(d => d.ID == device)
                            .Select(d => d.DeviceTypeID)
                            .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceType)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Consumer")
            {
                double sum = 0;
                var pu2 = GetPowerUsageFor7Days(device, direction);
                if (pu == null)
                {
                    pu = pu2;
                    continue;
                }
                foreach (var tsp in pu2.TimestampPowerPairs)
                {
                    foreach (var y in pu.TimestampPowerPairs)
                    {
                        if (y.Timestamp == tsp.Timestamp)
                            y.PowerUsage += tsp.PowerUsage;
                    }
                }
            }
        }

        return pu;
    }

    public PowerUsage GetPowerUsagesForEachDayProductionWeek(int direction)
    {
        var devices = _dataContext.Devices.Select(d => d.ID);

        PowerUsage pu = null;

        foreach (var device in devices)
        {
            var deviceType = _dataContext.Devices
                            .Where(d => d.ID == device)
                            .Select(d => d.DeviceTypeID)
                            .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceType)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Producer")
            {
                double sum = 0;
                var pu2 = GetPowerUsageFor7Days(device, direction);
                if (pu == null)
                {
                    pu = pu2;
                    continue;
                }
                foreach (var tsp in pu2.TimestampPowerPairs)
                {
                    foreach (var y in pu.TimestampPowerPairs)
                    {
                        if (y.Timestamp == tsp.Timestamp)
                            y.PowerUsage += tsp.PowerUsage;
                    }
                }
            }
        }

        return pu;
    }

    public PowerUsage GetPowerUsagesForEachDayProduction24h(int direction)
    {
        var devices = _dataContext.Devices.Select(d => d.ID);

        PowerUsage pu = null;

        foreach (var device in devices)
        {
            var deviceType = _dataContext.Devices
                            .Where(d => d.ID == device)
                            .Select(d => d.DeviceTypeID)
                            .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceType)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Producer")
            {
                double sum = 0;
                var pu2 = GetPowerUsageForDevicePast24Hours(device, direction);
                if (pu == null)
                {
                    pu = pu2;
                    continue;
                }
                foreach (var tsp in pu2.TimestampPowerPairs)
                {
                    foreach (var y in pu.TimestampPowerPairs)
                    {
                        if (y.Timestamp == tsp.Timestamp)
                            y.PowerUsage += tsp.PowerUsage;
                    }
                }
            }
        }

        return pu;
    }

    public PowerUsage GetPowerUsagesForEachDayConsumption24h(int direction)
    {
        var devices = _dataContext.Devices.Select(d => d.ID);

        PowerUsage pu = null;

        foreach (var device in devices)
        {
            var deviceType = _dataContext.Devices
                            .Where(d => d.ID == device)
                            .Select(d => d.DeviceTypeID)
                            .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceType)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Consumer")
            {
                double sum = 0;
                var pu2 = GetPowerUsageForDevicePast24Hours(device, direction);
                if (pu == null)
                {
                    pu = pu2;
                    continue;
                }
                foreach (var tsp in pu2.TimestampPowerPairs)
                {
                    foreach (var y in pu.TimestampPowerPairs)
                    {
                        if (y.Timestamp == tsp.Timestamp)
                            y.PowerUsage += tsp.PowerUsage;
                    }
                }
            }
        }

        return pu;
    }


    public PowerUsage GetPowerUsagesForEachDayProductionMonth(int direction)
    {
        var devices = _dataContext.Devices.Select(d => d.ID);

        PowerUsage pu = null;

        foreach (var device in devices)
        {
            var deviceType = _dataContext.Devices
                            .Where(d => d.ID == device)
                            .Select(d => d.DeviceTypeID)
                            .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceType)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Producer")
            {
                double sum = 0;
                var pu2 = GetPowerUsageForAMonth(device, direction);
                if (pu == null)
                {
                    pu = pu2;
                    continue;
                }
                foreach (var tsp in pu2.TimestampPowerPairs)
                {
                    foreach (var y in pu.TimestampPowerPairs)
                    {
                        if (y.Timestamp == tsp.Timestamp)
                            y.PowerUsage += tsp.PowerUsage;
                    }
                }
            }
        }

        return pu;
    }


    public List<PowerUsage> GetPowerUsageForDevicesConsumption(Guid userID, int direction)
    {
        IEnumerable<String> deviceTypeIds = _deviceRepository.GetDevicesForUser(userID).Select(d => d.DeviceTypeID.ToString().ToUpper());

        List<PowerUsage> puList = new List<PowerUsage>();

        if(deviceTypeIds == null)
        {
            return null;
        }

        PowerUsage pu = new PowerUsage();        
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        
        for (int i = 1; i <= 31; i++)
        {
            TimestampPowerPair tsp = new TimestampPowerPair();
            var date = DateTime.UtcNow.AddDays(direction * i).Date;
            tsp.Timestamp = date;
            foreach (var deviceType in deviceTypeIds)
            {
                string deviceGroupName = _dataContext.DeviceGroups
                    .Where(g => g.ID == _dataContext.DeviceTypes
                        .Where(dt => dt.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                        .Select(dt => dt.GroupID)
                        .FirstOrDefault())
                    .Select(g => g.Name)
                    .FirstOrDefault();

                if (deviceGroupName == "Consumer")
                {
                    var powerUsageData = mongoCollection.AsQueryable()
                        .Where(p => deviceType.Contains(p.ID.ToString()))
                        .ToList();

                    var powerUsage = powerUsageData
                    .Where(p => p.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(tp => tp.Timestamp.Date == date)
                    .Sum(tp => tp.PowerUsage);                 
                    tsp.PowerUsage += powerUsage;
                }
            }
            pu.TimestampPowerPairs.Add(tsp);
        } 

        puList.Add(pu);
        return puList;
    }

    public List<PowerUsage> GetPowerUsageForDevicesProduction(Guid userID, int direction)
    {
        IEnumerable<String> deviceTypeIds = _deviceRepository.GetDevicesForUser(userID).Select(d => d.DeviceTypeID.ToString().ToUpper());

        List<PowerUsage> puList = new List<PowerUsage>();

        if (deviceTypeIds == null)
        {
            return null;
        }

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();

        for (int i = 1; i <= 31; i++)
        {
            TimestampPowerPair tsp = new TimestampPowerPair();
            var date = DateTime.UtcNow.AddDays(direction * i).Date;
            tsp.Timestamp = date;
            foreach (var deviceType in deviceTypeIds)
            {
                string deviceGroupName = _dataContext.DeviceGroups
                    .Where(g => g.ID == _dataContext.DeviceTypes
                        .Where(dt => dt.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                        .Select(dt => dt.GroupID)
                        .FirstOrDefault())
                    .Select(g => g.Name)
                    .FirstOrDefault();

                if (deviceGroupName == "Producer")
                {
                    var powerUsageData = mongoCollection.AsQueryable()
                        .Where(p => deviceType.Contains(p.ID.ToString()))
                        .ToList();

                    var powerUsage = powerUsageData
                    .Where(p => p.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(tp => tp.Timestamp.Date == date)
                    .Sum(tp => tp.PowerUsage);
                    tsp.PowerUsage += powerUsage;
                }
            }
            pu.TimestampPowerPairs.Add(tsp);
        }

        puList.Add(pu);
        return puList;
    }

    public List<PowerUsage> GetPowerUsageForDevicesConsumptionFor7Days(Guid userID, int direction)
    {
        IEnumerable<String> deviceTypeIds = _deviceRepository.GetDevicesForUser(userID).Select(d => d.DeviceTypeID.ToString().ToUpper());
        
        List<PowerUsage> puList = new List<PowerUsage>();

        if (deviceTypeIds == null)
        {
            return null;
        }

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();

        for (int i = 1; i <= 7; i++)
        {
            TimestampPowerPair tsp = new TimestampPowerPair();
            var date = DateTime.UtcNow.AddDays(direction * i).Date;
            tsp.Timestamp = date;
            foreach (var deviceType in deviceTypeIds)
            {
                string deviceGroupName = _dataContext.DeviceGroups
                    .Where(g => g.ID == _dataContext.DeviceTypes
                        .Where(dt => dt.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                        .Select(dt => dt.GroupID)
                        .FirstOrDefault())
                    .Select(g => g.Name)
                    .FirstOrDefault();

                if (deviceGroupName == "Consumer")
                {
                    var powerUsageData = mongoCollection.AsQueryable()
                        .Where(p => deviceType.Contains(p.ID.ToString()))
                        .ToList();

                    var powerUsage = powerUsageData
                    .Where(p => p.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(tp => tp.Timestamp.Date == date)
                    .Sum(tp => tp.PowerUsage);
                    tsp.PowerUsage += powerUsage;
                }
            }
            pu.TimestampPowerPairs.Add(tsp);
        }

        puList.Add(pu);
        return puList;
    }

    public List<PowerUsage> GetPowerUsageForDevicesProductionFor7Days(Guid userID, int direction)
    {
        IEnumerable<String> deviceTypeIds = _deviceRepository.GetDevicesForUser(userID).Select(d => d.DeviceTypeID.ToString().ToUpper());

        List<PowerUsage> puList = new List<PowerUsage>();

        if (deviceTypeIds == null)
        {
            return null;
        }

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();

        for (int i = 1; i <= 7; i++)
        {
            TimestampPowerPair tsp = new TimestampPowerPair();
            var date = DateTime.UtcNow.AddDays(direction * i).Date;
            tsp.Timestamp = date;
            foreach (var deviceType in deviceTypeIds)
            {
                string deviceGroupName = _dataContext.DeviceGroups
                    .Where(g => g.ID == _dataContext.DeviceTypes
                        .Where(dt => dt.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                        .Select(dt => dt.GroupID)
                        .FirstOrDefault())
                    .Select(g => g.Name)
                    .FirstOrDefault();

                if (deviceGroupName == "Producer")
                {
                    var powerUsageData = mongoCollection.AsQueryable()
                        .Where(p => deviceType.Contains(p.ID.ToString()))
                        .ToList();

                    var powerUsage = powerUsageData
                    .Where(p => p.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(tp => tp.Timestamp.Date == date)
                    .Sum(tp => tp.PowerUsage);
                    tsp.PowerUsage += powerUsage;
                }
            }
            pu.TimestampPowerPairs.Add(tsp);
        }

        puList.Add(pu);
        return puList;
    }

    public List<PowerUsage> GetPowerUsageForDevicesConsumptionFor24Hours(Guid userID, int direction)
    {
        IEnumerable<String> deviceTypeIds = _deviceRepository.GetDevicesForUser(userID).Select(d => d.DeviceTypeID.ToString().ToUpper());

        List<PowerUsage> puList = new List<PowerUsage>();

        if (deviceTypeIds == null)
        {
            return null;
        }

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();

        for (int i = 1; i <= 24; i++)
        {
            TimestampPowerPair tsp = new TimestampPowerPair();
            var date = DateTime.Now.AddHours(direction * i);
            tsp.Timestamp = date;
            var dateplus1 = date.AddHours(1);
            foreach (var deviceType in deviceTypeIds)
            {
                string deviceGroupName = _dataContext.DeviceGroups
                    .Where(g => g.ID == _dataContext.DeviceTypes
                        .Where(dt => dt.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                        .Select(dt => dt.GroupID)
                        .FirstOrDefault())
                    .Select(g => g.Name)
                    .FirstOrDefault();

                if (deviceGroupName == "Consumer")
                {
                    var powerUsageData = mongoCollection.AsQueryable()
                        .Where(p => deviceType.Contains(p.ID.ToString()))
                        .ToList();

                    var powerUsage = powerUsageData
                    .Where(p => p.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(tp => tp.Timestamp >= date && tp.Timestamp <= dateplus1)
                    .Sum(tp => tp.PowerUsage);
                    tsp.PowerUsage += powerUsage;
                }
            }
            pu.TimestampPowerPairs.Add(tsp);
        }

        puList.Add(pu);
        return puList;
    }
    public List<PowerUsage> GetPowerUsageForDevicesProductionFor24Hours(Guid userID, int direction)
    {
        IEnumerable<String> deviceTypeIds = _deviceRepository.GetDevicesForUser(userID).Select(d => d.DeviceTypeID.ToString().ToUpper());

        List<PowerUsage> puList = new List<PowerUsage>();

        if (deviceTypeIds == null)
        {
            return null;
        }

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();

        for (int i = 1; i <= 24; i++)
        {
            TimestampPowerPair tsp = new TimestampPowerPair();
            var date = DateTime.Now.AddHours(direction * i);
            tsp.Timestamp = date;
            var dateplus1 = date.AddHours(1);
            foreach (var deviceType in deviceTypeIds)
            {
                string deviceGroupName = _dataContext.DeviceGroups
                    .Where(g => g.ID == _dataContext.DeviceTypes
                        .Where(dt => dt.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                        .Select(dt => dt.GroupID)
                        .FirstOrDefault())
                    .Select(g => g.Name)
                    .FirstOrDefault();

                if (deviceGroupName == "Producer")
                {
                    var powerUsageData = mongoCollection.AsQueryable()
                        .Where(p => deviceType.Contains(p.ID.ToString()))
                        .ToList();

                    var powerUsage = powerUsageData
                    .Where(p => p.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                    .SelectMany(p => p.TimestampPowerPairs)
                    .Where(tp => tp.Timestamp >= date && tp.Timestamp <= dateplus1)
                    .Sum(tp => tp.PowerUsage);
                    tsp.PowerUsage += powerUsage;
                }
            }
            pu.TimestampPowerPairs.Add(tsp);
        }

        puList.Add(pu);
        return puList;
    }

    public PowerUsage GetPowerUsageForDevicePast24Hours(Guid deviceID, int direction)
    {
        var utcNow = DateTime.Now;

        PowerUsage pu = new PowerUsage();
        pu.ID = deviceID;
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();

        var startOf24Period = direction > 0
            ? utcNow
            : utcNow.AddHours(-25);

        var endOf24Period = direction > 0 
            ? utcNow.AddHours(24) 
            : utcNow.AddHours(-1);


        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceID)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();

        var powerUsages = mongoCollection.AsQueryable()
            .Where(p => p.ID.ToString().ToUpper() == deviceTypeID.ToString().ToUpper())
            .SelectMany(p => p.TimestampPowerPairs)
            .ToList()
            .Where(t => t.Timestamp >= startOf24Period && t.Timestamp <= endOf24Period)
            .ToList();

        foreach(var powerUsage in powerUsages)
        {
            TimestampPowerPair tsp = new TimestampPowerPair();
            tsp.Timestamp = powerUsage.Timestamp;
            tsp.PowerUsage = powerUsage.PowerUsage;
            pu.TimestampPowerPairs.Add(tsp);
        }

        return pu;
    }

    public PowerUsage Get12hoursBefore12hoursAfter(Guid deviceID)
    {
        var moment = DateTime.Now;
        var endOf12 = moment.AddHours(12);
        var startOf12 = moment.AddHours(-12);

        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceID)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();

        var powerUsage = new PowerUsage();
        powerUsage.TimestampPowerPairs = new List<TimestampPowerPair>();

        var powerUsages = mongoCollection.AsQueryable()
            .Where(p => deviceTypeID.ToString().ToUpper().Contains(p.ID.ToString().ToUpper()))
            .FirstOrDefault();

        if (powerUsages == null)
        {
            return null;
        }

        powerUsage.ID = powerUsages.ID;

        var currentDate = startOf12;

        while (currentDate <= endOf12)
        {
            var ts = new TimestampPowerPair();
            var sum = GetCurrentPowerUsage(currentDate, powerUsages.ID);
            ts.Timestamp = currentDate;
            ts.PowerUsage = sum;
            powerUsage.TimestampPowerPairs.Add(ts);
            currentDate = currentDate.AddHours(1);
        }


        return powerUsage;
    }

    public List<PowerUsage> GetPowerUsageForDevicePast24Hoursv2(Guid deviceId, int direction)
    {
        var utcNow = DateTime.UtcNow;

        var startOf24Period = direction > 0
            ? utcNow
            : utcNow.AddHours(-24);

        var endOf24Period = direction > 0 
            ? utcNow.AddHours(24) 
            : utcNow.AddHours(-1);

        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceId)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();

        var powerUsages = mongoCollection.AsQueryable()
            .Where(p => p.ID.ToString().ToUpper() == deviceTypeID.ToString().ToUpper())
            .SelectMany(p => p.TimestampPowerPairs)
            .ToList()
            .Where(t => t.Timestamp >= startOf24Period && t.Timestamp <= endOf24Period)
            .GroupBy(t => t.Timestamp.Date)
            .Select(g => new PowerUsage
            {
                ID = deviceId,
                TimestampPowerPairs = g.ToList()
            })
            .ToList();

        return powerUsages;
    }
    

    public PowerUsage GetPowerProducedForADaySystem()
    {
        var startOf24Period = DateTime.Today;
        var endOf24Period = DateTime.Now;
        double sum = 0;
        var powerUsage = new PowerUsage();
        powerUsage.TimestampPowerPairs = new List<TimestampPowerPair>();

        var devices = _dataContext.Devices.Select(d => d.DeviceTypeID).ToList();

        var currentDate = startOf24Period;

        while (currentDate <= endOf24Period)
        {
            var ts = new TimestampPowerPair();
            foreach (var device in devices)
            {
                string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID.ToString().ToUpper() == device.ToString().ToUpper())
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();
                if (deviceGroupName == "Producer")
                {
                    sum += GetCurrentPowerUsage(currentDate, device);
                }
            }
            ts.Timestamp = currentDate;
            ts.PowerUsage = sum;
            powerUsage.TimestampPowerPairs.Add(ts);
            sum = 0;
            currentDate = currentDate.AddHours(1);
        }

        return powerUsage;
    }
    public PowerUsage GetPowerConsumedForADaySystem()
    {
        var startOf24Period = DateTime.Today;
        var endOf24Period = DateTime.Now;
        double sum = 0;
        var powerUsage = new PowerUsage();
        powerUsage.TimestampPowerPairs = new List<TimestampPowerPair>();

        var devices = _dataContext.Devices.Select(d => d.DeviceTypeID).ToList();
       
        var currentDate = startOf24Period;
        
        while (currentDate <= endOf24Period)
        {
            var ts = new TimestampPowerPair();
            foreach (var device in devices)
            {
                string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID.ToString().ToUpper() == device.ToString().ToUpper())
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();
                if (deviceGroupName == "Consumer")
                {
                    sum += GetCurrentPowerUsage(currentDate, device);
                }
            }
            ts.Timestamp = currentDate;
            ts.PowerUsage = sum;
            powerUsage.TimestampPowerPairs.Add(ts);
            sum = 0;
            currentDate = currentDate.AddHours(1);
        } 

        return powerUsage;
    }

    public double GetCurrentPowerConsumption()
    {
        double powerUsages = 0;
        var startOfAnHour = DateTime.Now.AddHours(-1);
        var endOfAnHour = DateTime.Now;

        var devices = _dataContext.Devices.Select(d => d.DeviceTypeID).ToList();

        foreach (var device in devices)
        {
            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == device)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();
            if (deviceGroupName == "Consumer")
            {
                powerUsages += mongoCollection.AsQueryable().ToList()
                    .Where(d => d.ID.ToString().ToUpper() == device.ToString().ToUpper())
                    .Sum(p => p.TimestampPowerPairs.Where(t => t.Timestamp >= startOfAnHour && t.Timestamp < endOfAnHour).Sum(p => p.PowerUsage));
            }
        }

        return powerUsages;
    }
    public double GetCurrentPowerProduction()
    {
        double powerUsages = 0;
        var startOfAnHour = DateTime.Now.AddHours(-1);
        var endOfAnHour = DateTime.Now;

        var devices = _dataContext.Devices.Select(d => d.DeviceTypeID).ToList();

        foreach (var device in devices)
        {    
            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == device)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();
            if (deviceGroupName == "Producer")
            {
                powerUsages += mongoCollection.AsQueryable().ToList()
                    .Where(d => d.ID.ToString().ToUpper() == device.ToString().ToUpper())
                    .Sum(p => p.TimestampPowerPairs.Where(t => t.Timestamp >= startOfAnHour && t.Timestamp < endOfAnHour).Sum(p => p.PowerUsage));
            }
        }

        return powerUsages;
    }

   
    public double GetCurrentPowerUsage(DateTime date,Guid deviceTypeID)
    {
        var startOfAnHour = date;
        var endOfAnHour = date.AddHours(1);

        var deviceUsages = mongoCollection.AsQueryable().Where(p => p.ID.ToString() == deviceTypeID.ToString().ToUpper()).ToList();

        var powerUsages = deviceUsages
            .Sum(p => p.TimestampPowerPairs.Where(t => t.Timestamp >= startOfAnHour && t.Timestamp < endOfAnHour).Sum(p => p.PowerUsage));

        return powerUsages;
    }

    public double GetCurrentPowerUsage(DateTime date)
    {
        var startOfAnHour = date;
        var endOfAnHour = date.AddHours(1);

        var deviceUsages = mongoCollection.AsQueryable().ToList();

        var powerUsages = deviceUsages
            .Sum(p => p.TimestampPowerPairs.Where(t => t.Timestamp >= startOfAnHour && t.Timestamp < endOfAnHour).Sum(p => p.PowerUsage));

        return powerUsages;
    }

    public PowerUsage GetDeviceWithMaxPowerUsage24Consumption(Guid userID)
    {
        var deviceTypes = _deviceRepository.GetDevicesForUser(userID).Select(p => p.DeviceTypeID);

        var maxDeviceID = Guid.Empty;
        double maxPowerUsage = 0;

        DateTime endTime = DateTime.Now;
        DateTime startTime = endTime.AddDays(-1);
        DateTime currentTime = startTime;

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        TimestampPowerPair tsp = new TimestampPowerPair();

        foreach(var device in deviceTypes)
        {

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == device)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();
            if(deviceGroupName == "Consumer")
            {
                while (currentTime <= endTime)
                {
                    double powerUsageSum = GetCurrentPowerUsage(currentTime, device);
                    if (powerUsageSum > maxPowerUsage)
                    {
                        maxPowerUsage = powerUsageSum;
                        maxDeviceID = device;
                    }

                    currentTime = currentTime.AddHours(1);
                }
            }
        }

        pu.ID = maxDeviceID;
        tsp.PowerUsage = maxPowerUsage;
        pu.TimestampPowerPairs.Add(tsp);

        return pu;
    }

    public PowerUsage GetDeviceWithMaxPowerUsage24Production(Guid userID)
    {
        var deviceTypes = _deviceRepository.GetDevicesForUser(userID).Select(p => p.DeviceTypeID);

        var maxDeviceID = Guid.Empty;
        double maxPowerUsage = 0;

        DateTime endTime = DateTime.Now;
        DateTime startTime = endTime.AddDays(-1);
        DateTime currentTime = startTime;

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        TimestampPowerPair tsp = new TimestampPowerPair();

        foreach (var device in deviceTypes)
        {

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == device)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();
            if (deviceGroupName == "Producer")
            {
                while (currentTime <= endTime)
                {
                    double powerUsageSum = GetCurrentPowerUsage(currentTime, device);
                    if (powerUsageSum > maxPowerUsage)
                    {
                        maxPowerUsage = powerUsageSum;
                        maxDeviceID = device;
                    }

                    currentTime = currentTime.AddHours(1);
                }
            }
        }

        pu.ID = maxDeviceID;
        tsp.PowerUsage = maxPowerUsage;
        pu.TimestampPowerPairs.Add(tsp);

        return pu;
    }

    public PowerUsage GetDeviceWithMaxPowerUsagePreviousWeekProduction(Guid userID)
    {
        var deviceTypes = _deviceRepository.GetDevicesForUser(userID).Select(p => p.DeviceTypeID);

        var maxDeviceID = Guid.Empty;
        double maxPowerUsage = 0;

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        TimestampPowerPair tsp = new TimestampPowerPair();

        DateTime endDate = DateTime.Now;
        DateTime startDate = endDate.AddDays(-7);
        DateTime currentTime = startDate;

        foreach (var device in deviceTypes)
        {

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == device)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if(deviceGroupName == "Producer")
            {
                while (currentTime <= endDate)
                {
                    double powerUsageSum = GetCurrentPowerUsage(currentTime, device);
                    if (powerUsageSum > maxPowerUsage)
                    {
                        maxPowerUsage = powerUsageSum;
                        maxDeviceID = device;
                    }

                    currentTime = currentTime.AddHours(1);
                }
            }

        }

        pu.ID = maxDeviceID;
        tsp.PowerUsage = maxPowerUsage;
        pu.TimestampPowerPairs.Add(tsp);

        return pu;
    }

    public PowerUsage GetDeviceWithMaxPowerUsagePreviousWeekConsumption(Guid userID)
    {
        var deviceTypes = _deviceRepository.GetDevicesForUser(userID).Select(p => p.DeviceTypeID);

        var maxDeviceID = Guid.Empty;
        double maxPowerUsage = 0;

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        TimestampPowerPair tsp = new TimestampPowerPair();

        DateTime endDate = DateTime.Now;
        DateTime startDate = endDate.AddDays(-7);
        DateTime currentTime = startDate;

        foreach (var device in deviceTypes)
        {

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == device)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Consumer")
            {
                while (currentTime <= endDate)
                {
                    double powerUsageSum = GetCurrentPowerUsage(currentTime, device);
                    if (powerUsageSum > maxPowerUsage)
                    {
                        maxPowerUsage = powerUsageSum;
                        maxDeviceID = device;
                    }

                    currentTime = currentTime.AddHours(1);
                }
            }

        }

        pu.ID = maxDeviceID;
        tsp.PowerUsage = maxPowerUsage;
        pu.TimestampPowerPairs.Add(tsp);

        return pu;
    }

    public PowerUsage GetDeviceWithMaxPowerUsagePreviousMonthConsumption(Guid userID, int direction) // ubacen direction da diktira koliko meseci ide unazad (direction => broj meseci)
    {
        DateTime endDate = DateTime.Now;
        DateTime startDate = endDate.AddMonths( -1 * direction );

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        TimestampPowerPair tsp = new TimestampPowerPair();

        var devices = _deviceRepository.GetDevicesForUser(userID).Select(dt => dt.DeviceTypeID);

        var maxDeviceID = Guid.Empty;
        double maxPowerUsage = 0;

        foreach (var device in devices)
        {

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == device)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            DateTime currentTime = startDate;

            if(deviceGroupName == "Consumer")
            {
                while (currentTime <= endDate)
                {
                    double powerUsageSum = GetCurrentPowerUsage(currentTime, device);

                    if (powerUsageSum > maxPowerUsage)
                    {
                        maxPowerUsage = powerUsageSum;
                        maxDeviceID = device;
                    }
                    currentTime = currentTime.AddHours(1);
                }
            }
        }

           pu.ID = maxDeviceID;
           tsp.PowerUsage = maxPowerUsage;
           pu.TimestampPowerPairs.Add(tsp);

           return pu;
    }

    public PowerUsage GetDeviceWithMaxPowerUsagePreviousMonthProduction(Guid userID, int direction)
    {
        DateTime endDate = DateTime.Now;
        DateTime startDate = endDate.AddMonths( -1 * direction );

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        TimestampPowerPair tsp = new TimestampPowerPair();

        var devices = _deviceRepository.GetDevicesForUser(userID).Select(dt => dt.DeviceTypeID);

        var maxDeviceID = Guid.Empty;
        double maxPowerUsage = 0;

        foreach (var device in devices)
        {

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == device)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            DateTime currentTime = startDate;

            if (deviceGroupName == "Producer")
            {
                while (currentTime <= endDate)
                {
                    double powerUsageSum = GetCurrentPowerUsage(currentTime, device);

                    if (powerUsageSum > maxPowerUsage)
                    {
                        maxPowerUsage = powerUsageSum;
                        maxDeviceID = device;
                    }
                    currentTime = currentTime.AddHours(1);
                }
            }
        }

        pu.ID = maxDeviceID;
        tsp.PowerUsage = maxPowerUsage;
        pu.TimestampPowerPairs.Add(tsp);

        return pu;
    }

    public PowerUsage GetDeviceWithMaxPowerUsageCurrentProduction(Guid userID)
    {
        var devices = _deviceRepository.GetDevicesForUser(userID);

        DateTime endHour = DateTime.UtcNow;
        DateTime startHour = endHour.AddHours(-1);

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        TimestampPowerPair tsp = new TimestampPowerPair();

        var maxDeviceID = Guid.Empty;
        double maxPowerUsage = 0;

        foreach (var device in devices)
        {
            Guid deviceTypeID = _dataContext.Devices
               .Where(d => d.ID == device.ID)
               .Select(d => d.DeviceTypeID)
               .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceTypeID)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if(deviceGroupName == "Producer")
            {
                double powerUsageSum = GetCurrentPowerUsage(startHour, deviceTypeID);

                if (powerUsageSum > maxPowerUsage)
                {
                    maxPowerUsage = powerUsageSum;
                    maxDeviceID = device.ID;
                }
            }
        }

        pu.ID = maxDeviceID;
        tsp.PowerUsage = maxPowerUsage;
        pu.TimestampPowerPairs.Add(tsp);

        return pu;

    }

    public PowerUsage GetDeviceWithMaxPowerUsageCurrentConsumption(Guid userID)
    {
        var devices = _deviceRepository.GetDevicesForUser(userID);

        DateTime endHour = DateTime.UtcNow;
        DateTime startHour = endHour.AddHours(-1);

        PowerUsage pu = new PowerUsage();
        pu.TimestampPowerPairs = new List<TimestampPowerPair>();
        TimestampPowerPair tsp = new TimestampPowerPair();

        var maxDeviceID = Guid.Empty;
        double maxPowerUsage = 0;

        foreach (var device in devices)
        {
            Guid deviceTypeID = _dataContext.Devices
               .Where(d => d.ID == device.ID)
               .Select(d => d.DeviceTypeID)
               .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceTypeID)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Consumer" && device.IsOn == true)
            {
                double powerUsageSum = GetCurrentPowerUsage(startHour, deviceTypeID);

                if (powerUsageSum > maxPowerUsage)
                {
                    maxPowerUsage = powerUsageSum;
                    maxDeviceID = device.ID;
                }
            }
        }

        pu.ID = maxDeviceID;
        tsp.PowerUsage = maxPowerUsage;
        pu.TimestampPowerPairs.Add(tsp);

        return pu;

    }
    public double GetHowMuchUserIsConsuming(Guid userId)
    {
        var userDevices = _deviceRepository.GetDevicesForUser(userId).Select(d => d.DeviceTypeID);
        double maximumConsumption = 0;
        foreach (var userdevice in userDevices)
        {
            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == userdevice)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Consumer")
            {
                maximumConsumption += _dataContext.DeviceTypes.Where(d => d.ID == userdevice).Sum(d => d.Wattage);
            }
        }

        var currentUserConsumption = this.CurrentSumPowerUsageConsumption(userId);


        var savedEnergy = ((maximumConsumption - currentUserConsumption) / maximumConsumption) * 100;

        return savedEnergy;
    }
    public double deviceEnergySaved(Guid deviceID)
    {
        DateTime previousHour = DateTime.Now.AddHours(-2);
        DateTime thisHour = DateTime.Now.AddHours(-1);

        Guid deviceTypeID = _dataContext.Devices
               .Where(d => d.ID == deviceID)
               .Select(d => d.DeviceTypeID)
               .FirstOrDefault();

        double previousHourPowerUsage = GetCurrentPowerUsage(previousHour, deviceTypeID);
        Console.WriteLine("PROSLI SAT " + previousHourPowerUsage);
        double thisHourPowerUsage = GetCurrentPowerUsage(thisHour, deviceTypeID);
        Console.WriteLine("TRENUTNI SAT " + thisHourPowerUsage);

        return (previousHourPowerUsage - thisHourPowerUsage) / 100;
    }

    public double SavedEnergySystemProducer()
    {
        var lastMonth = this.GetPowerUsageForAMonthSystemProducer(-2);
        var thisMonth = this.GetPowerUsageForAMonthSystemProducer(-1);

        var savedEnergy = ((lastMonth - thisMonth) / lastMonth) * 100;

        return savedEnergy;
    }

    public double SavedEnergySystemConsumer()
    {
        var lastMonth = this.GetPowerUsageForAMonthSystemConsumer(-2);
        var thisMonth = this.GetPowerUsageForAMonthSystemConsumer(-1);

        var savedEnergy = ((lastMonth - thisMonth) / lastMonth) * 100;

        return savedEnergy;
    }

    public double savedEnergyForUserConsumer(Guid userID, int direction)
    {
        DateTime endDate = DateTime.Now;
        DateTime startDate = DateTime.Now;

        if (direction == 1)
        {
            endDate = DateTime.Now;
            startDate = endDate.AddMonths(-1);
        }

        if (direction == 2)
        {
            endDate = DateTime.Now.AddMonths(-1);
            startDate = endDate.AddMonths(-1);
        }


        double sum = 0;

        var devices = _deviceRepository.GetDevicesForUser(userID);

        foreach(var device in devices)
        {
            var deviceType = _dataContext.Devices
                            .Where(d => d.ID == device.ID)
                            .Select(dt => dt.DeviceTypeID)
                            .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceType)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if(deviceGroupName == "Consumer")
            {
                var powerUsages = mongoCollection.AsQueryable()
                    .Where(p => p.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                    .SelectMany(p => p.TimestampPowerPairs)
                    .ToList()
                    .Where(t => t.Timestamp >= startDate && t.Timestamp <= endDate)
                    .ToList();

                foreach(var powerUsage in powerUsages)
                {
                    sum += powerUsage.PowerUsage;
                }
            }
        }

        return sum;
    }

    public double savedEnergyForUserProducer(Guid userID, int direction)
    {
        DateTime endDate = DateTime.Now;
        DateTime startDate = DateTime.Now;

        if (direction == 1)
        {
            endDate = DateTime.Now;
            startDate = endDate.AddMonths(-1);
        }

        if (direction == 2)
        {
            endDate = DateTime.Now.AddMonths(-1);
            startDate = endDate.AddMonths(-1);
        }

        double sum = 0;

        var devices = _deviceRepository.GetDevicesForUser(userID);

        foreach (var device in devices)
        {
            var deviceType = _dataContext.Devices
                            .Where(d => d.ID == device.ID)
                            .Select(dt => dt.DeviceTypeID)
                            .FirstOrDefault();

            string deviceGroupName = _dataContext.DeviceGroups
                        .Where(g => g.ID == _dataContext.DeviceTypes
                            .Where(dt => dt.ID == deviceType)
                            .Select(dt => dt.GroupID)
                            .FirstOrDefault())
                        .Select(g => g.Name)
                        .FirstOrDefault();

            if (deviceGroupName == "Producer")
            {
                var powerUsages = mongoCollection.AsQueryable()
                    .Where(p => p.ID.ToString().ToUpper() == deviceType.ToString().ToUpper())
                    .SelectMany(p => p.TimestampPowerPairs)
                    .ToList()
                    .Where(t => t.Timestamp >= startDate && t.Timestamp <= endDate)
                    .ToList();

                foreach (var powerUsage in powerUsages)
                {
                    sum += powerUsage.PowerUsage;
                }
            }
        }

        return sum;
    }

    public double savedEnergyForUserConsumer(Guid userID)
    {
        var previousMonth = savedEnergyForUserConsumer(userID, 2);
        var thisMonth = savedEnergyForUserConsumer(userID, 1);

        var savedEnergy = ((previousMonth - thisMonth) / previousMonth) * 100;

        return savedEnergy;
    }

    public double savedEnergyForUserProducer(Guid userID)
    {
        var previousMonth = savedEnergyForUserProducer(userID, 2);
        var thisMonth = savedEnergyForUserProducer(userID, 1);

        var savedEnergy = ((previousMonth - thisMonth) / previousMonth) * 100;

        return savedEnergy;
    }

    public PowerUsage GetPowerUsageForAMonthConsumption(Guid deviceId, int direction)
    {
        Guid deviceTypeID = _dataContext.Devices
            .Where(d => d.ID == deviceId)
            .Select(d => d.DeviceTypeID)
            .FirstOrDefault();

        var powerUsage = new PowerUsage();
        powerUsage.TimestampPowerPairs = new List<TimestampPowerPair>();
        var today = DateTime.Today;

        for (int i = 1; i <= 31; i++)
        {
            var day = today.AddDays(i * direction);
            var powerUsageD = GetPowerUsageForDay(deviceId, day);
            var ts = new TimestampPowerPair();
            ts.PowerUsage = powerUsageD;
            ts.Timestamp = day;
            powerUsage.TimestampPowerPairs.Add(ts);

        }

        if (direction == -1)
            powerUsage.TimestampPowerPairs.Reverse();

        return powerUsage;
    }



    public double percentPowerUsageForPreviousHour(Guid deviceID)
    {
        DateTime date = DateTime.Now.AddHours(-1);

        Guid deviceTypeID = _dataContext.Devices
                .Where(d => d.ID == deviceID)
                .Select(d => d.DeviceTypeID)
                .FirstOrDefault();

        double previousHourSystemUsage = 0;
        double previopusHourDeviceUsage = this.GetCurrentPowerUsage(date, deviceTypeID);

        string deviceGroupName = _dataContext.DeviceGroups
                .Where(g => g.ID == _dataContext.DeviceTypes
                    .Where(dt => dt.ID == deviceTypeID)
                    .Select(dt => dt.GroupID)
                    .FirstOrDefault())
                .Select(g => g.Name)
                .FirstOrDefault();

        if(deviceGroupName == "Producer")
            previousHourSystemUsage = this.CurrentSumPowerUsageSystemProducer();
        if (deviceGroupName == "Consumer")
            previousHourSystemUsage = this.CurrentSumPowerUsageSystemConsumer();

        return (previopusHourDeviceUsage / previousHourSystemUsage) * 100;
    }

}