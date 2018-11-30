using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SQLite;
using System.Data;

namespace Licensing
{
    public class LicenseRow
    {
        public long ID { get; set; }
        public string Key { get; set; }
        public int Type { get; set; }
        public int Count { get; internal set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string HardwareInfo { get; set; }
    }
    public class LicensingDb : IDisposable
    {
        SQLiteConnection conn;
        SQLiteTransaction tx;

        public LicensingDb(bool transaction = false)
        {
            conn = new SQLiteConnection(Properties.Settings.Default.ConnectionString);
            conn.Open();
            if (transaction)
                tx = conn.BeginTransaction();
        }

        public void Commit()
        {
            if (tx == null) return;
            tx.Commit();
            tx.Dispose();
            tx = null;
        }

        public void Dispose()
        {
            if (tx != null)
                tx.Dispose();
            conn.Dispose();
        }

        static string SqlCreateTable =
@"CREATE TABLE IF NOT EXISTS license (
    id integer PRIMARY KEY,
    key VARCHAR(25) collate nocase,
    type int,
    count int default 1,
    issuedate date,
    expiredate date,
    hardwareinfo varchar
);";
        public void EnsureTable()
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = SqlCreateTable;
                cmd.ExecuteNonQuery();
            }
        }

        static string SqlSumCount =
            "SELECT IFNULL(SUM(count),0) FROM license" +
            " WHERE hardwareinfo = @HW AND expiredate > DATETIME()";
        public int SumCountOfHW(string hwinfo)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = SqlSumCount;
                cmd.Parameters.Add("@HW", DbType.AnsiString).Value = hwinfo;
                return (int)cmd.ExecuteScalar();
            }
        }

        static string SqlSelectByKeyCode =
            "SELECT id, key, type, count, issuedate, expiredate, hardwareinfo" +
            " FROM license WHERE key = @Key";
        public List<LicenseRow> SelectByKey(string key)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = SqlSelectByKeyCode;
                cmd.Parameters.Add("@Key", System.Data.DbType.AnsiString)
                    .Value = key;
                DataTable table = new DataTable();
                using (var reader = cmd.ExecuteReader())
                    table.Load(reader);
                return ToList(table);
            }
        }
        List<LicenseRow> ToList(DataTable table)
        {
            var list = new List<LicenseRow>();
            foreach (DataRow r in table.Rows)
            {
                var lic = new LicenseRow
                {
                    ID = (long)r[0],
                    Key = (string)r[1],
                    Type = (int)r[2],
                    Count = (int)r[3],
                    IssueDate = r[4] as DateTime?,
                    ExpireDate = r[5] as DateTime?,
                    HardwareInfo = r[6] as string,
                };
                list.Add(lic);
            }
            return list;
        }

        static string SqlUpdate =
            "UPDATE license SET " +
            "issuedate = @IssueDate, expiredate = @ExpireDate, hardwareinfo = @HardwareInfo " +
            "WHERE id = @ID";
        public void Update(LicenseRow row)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = SqlUpdate;
                cmd.Parameters.Add("@IssueDate", System.Data.DbType.Date)
                    .Value = row.IssueDate;
                cmd.Parameters.Add("@ExpireDate", System.Data.DbType.Date)
                    .Value = row.ExpireDate;
                cmd.Parameters.Add("@HardwareInfo", System.Data.DbType.AnsiString)
                    .Value = row.HardwareInfo;
                cmd.Parameters.Add("@ID", System.Data.DbType.Int64)
                    .Value = row.ID;

                cmd.ExecuteNonQuery();
            }

        }
    }
}