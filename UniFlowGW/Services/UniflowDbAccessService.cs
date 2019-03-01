using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using UniFlowGW.Controllers;

namespace UniFlowGW.Services
{
    public class UniflowDbAccessService
    {
        #region SQL
        const string SqlCommand =
@"
SELECT
    ServiceProvider_T.Name,ServiceProvider_T.ID,ServiceProvider_T.MgmtData_ModelName,
    ServiceProvider_T.MgmtData_HostName,ServiceProvider_T.MgmtData_Location,
    ServiceProvider_T.MgmtData_MacAddress,ServiceProvider_T.MgmtData_AssetNumber,
    LatestStatus_T.Device, LatestStatus_T.TimeOccured, LatestStatus_T.TimeOccuredDuration, LatestStatus_T.SerialNumber, 
    Status, ManufacturerInfo, StatusEx, TotalCounter,
    PreviousTime_T.PreviousTime as PreviousTime
FROM
    (
        SELECT

            Events_t.Device, max(Time) as TimeOccured, max(Time) as TimeOccuredDuration,
            ManufacturerInfo, SerialNumber, max(Status) as Status, max(StatusEx) as StatusEx,
            max(TotalCounter) as TotalCounter

        FROM

            Events_T,
            (
                select
                    Device, max(Time) as MaximumTime

                from
                    Events_T
                GROUP BY Device
            ) MaxEvent_T
        WHERE

            Events_T.Device = MaxEvent_T.Device AND Events_T.Time = MaximumTime

        GROUP BY

            Events_T.Device, ManufacturerInfo, SerialNumber
    ) LatestStatus_T
LEFT JOIN
    (
        select

            Events_t.Device, max(Time) as PreviousTime

        from
            Events_t
        LEFT JOIN
            (

                select
                    Device, max(Time) as MaximumTime

                from
                    Events_T
                GROUP BY Device
            ) MaxEventToGetPreviousTime
        ON

            Events_t.Device = MaxEventToGetPreviousTime.Device

        WHERE

            Time < MaximumTime

        GROUP BY

            Events_t.Device
    ) PreviousTime_t
ON

    LatestStatus_T.Device = PreviousTime_T.Device
INNER JOIN
    (
        SELECT
            ServiceProvider_T.Name, ServiceProvider_T.ID, ServiceProvider_T.MgmtData_ModelName,
            ServiceProvider_T.MgmtData_HostName, ServiceProvider_T.MgmtData_Location,
            ServiceProvider_T.MgmtData_MacAddress, ServiceProvider_T.MgmtData_AssetNumber
        from

            ServiceProvider_T
        WHERE

            Visibility= 0 and ProviderType = 4
    ) AS ServiceProvider_T
    ON
    LatestStatus_T.Device = ServiceProvider_T.id  
";
        #endregion

        readonly ILogger<UniflowDbAccessService> logger;
        readonly SettingService settings;
        public UniflowDbAccessService(ILogger<UniflowDbAccessService> logger,
            SettingService settings)
        {
            this.logger = logger;
            this.settings = settings;
        }

        public async Task<int?> QueryPrinterCountAsync()
        {
            var table = await QueryPrintersAsync();
            if (table != null)
                return table.Rows.Count;
            return null;
        }

        private async Task<DataTable> QueryPrintersAsync()
        {
            try
            {
                logger.LogTrace("Querying printers");
                var connectionString = settings[SettingsKey.UniflowConnection];
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandText = SqlCommand;

                        SqlDataReader reader = await command.ExecuteReaderAsync();
                        var table = new DataTable();
                        await Task.Run(() => table.Load(reader));

                        return table;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }
        }
    }
}
