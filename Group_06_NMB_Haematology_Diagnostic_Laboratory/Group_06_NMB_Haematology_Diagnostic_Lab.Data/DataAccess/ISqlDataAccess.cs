using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess
{


    public interface ISqlDataAccess
    {
        Task<IEnumerable<T>> GetData<T, P>(string spName, P parameters, string connectionId = "conn");
        Task SaveData<T>(string spName, T parameters, string connectionId = "conn");


        //LOGIN QUERY
        Task<T?> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null, string connectionId = "conn");


        //NEW METHODS FOR LOADING SQL QUERY DIRECTLY INTO VS
        Task<IEnumerable<T>> LoadData<T, U>(string sql, U parameters, string connectionId = "conn");
        Task<int> Save<T>(string sql, T parameters, string connectionId = "conn");
        Task<int> ExecuteScalarAsync<T>(string sql, T parameters, string connectionId = "conn");


      

        Task<T> SaveDataReturnId<T, U>(
            string storedProcedure,
            U parameters,
            string connectionId = "conn");
    }


}
