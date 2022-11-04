using System;
using System.Data;
using System.Data.SqlClient;

namespace Ipt
{

    class Class_ERPDB
    {
        //public static string conStr = @"Data Source=192.168.2.5;Initial Catalog=ERP_2;Persist Security Info=True;User ID=sa;Password=inter07@";
        private readonly SqlConnection sqlCon = new SqlConnection();
        private string SqlQueryString = "";

        public void ConnectDB(string conStr)
        {
            try
            {
                SqlQueryString = conStr;
            }
            catch(Exception)
            {
                throw;
            }
            if(sqlCon.State.ToString().Equals("Closed"))
            {
                sqlCon.ConnectionString = SqlQueryString;
                sqlCon.Open();
            }
        }

        public void CloseDB()
        {
            if(sqlCon != null)
            {
                sqlCon.Close();
            }
        }

        public DataTable GetDBtable(string sql)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, sqlCon))
            {
                //SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                adapter.Fill(dt);
            }
            return dt;
        }

        public void ExecuteNonQuery(string sql)
        {
            using (SqlDataAdapter adapter = new SqlDataAdapter())
            {
                adapter.InsertCommand = new SqlCommand(sql, sqlCon);
                adapter.InsertCommand.ExecuteNonQuery();
            }
        }
    }
}
