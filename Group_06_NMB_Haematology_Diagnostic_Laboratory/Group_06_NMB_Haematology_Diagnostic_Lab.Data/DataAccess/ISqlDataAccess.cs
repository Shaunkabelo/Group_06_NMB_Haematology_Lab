using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess
{


    public interface ISqlDataAccess
    {
        Task<IEnumerable<T>> GetData<T, P>(string spName, P parameters, string connectionId = "conn");



        // ===============================================================================
                              // GET DATA OVERLOAD METHODS

        // NEW - Two object mapping
        Task<IEnumerable<TReturn>> GetData<TFirst, TSecond, TReturn, P>(
            string spName,
            Func<TFirst, TSecond, TReturn> map,
            P parameters,
            string splitOn = "Id",
            string connectionId = "conn");

        // NEW - Three object mapping
        Task<IEnumerable<TReturn>> GetData<TFirst, TSecond, TThird, TReturn, P>(
            string spName,
            Func<TFirst, TSecond, TThird, TReturn> map,
            P parameters,
            string splitOn = "Id",
            string connectionId = "conn");

        // NEW - Four object mapping
        Task<IEnumerable<TReturn>> GetData<TFirst, TSecond, TThird, TFourth, TReturn, P>(
        string spName,
        Func<TFirst, TSecond, TThird, TFourth, TReturn> map,
        P parameters,
        string splitOn = "Id",
        string connectionId = "conn");

        //     ==============================================================================






        Task SaveData<T>(string spName, T parameters, string connectionId = "conn");
        // ===============================================================================
        //  SAVE DATA OVERLOAD METHODS
        Task<T> SaveDataAndReturn<T, P>(string spName, P parameters, string connectionId = "conn");

        // ===========================================================================================






        // ======================== TRANSACTION METHOD ================================
        Task ExecuteInTransaction(Func<IDbConnection, IDbTransaction, Task> action);
        //===========================================================================




        // Patient subsystem - multiple result sets
        Task<(
            IEnumerable<TFirst> First,
            IEnumerable<TSecond> Second,
            IEnumerable<TThird> Third
        )> GetMultipleData<TFirst, TSecond, TThird, P>(
            string spName,
            P parameters,
            string connectionId = "conn");

        // Patient subsystem - parameterized SQL queries
        Task<IEnumerable<T>> LoadData<T, U>(
            string sql,
            U parameters,
            string connectionId = "conn");

        Task<int> Save<T>(
            string sql,
            T parameters,
            string connectionId = "conn");

        Task<T?> QueryFirstOrDefaultAsync<T>(
            string query,
            object? parameters = null,
            string connectionId = "conn");

        // Patient subsystem - stored procedure returning an ID
        Task<T> SaveDataReturnId<T, U>(
            string spName,
            U parameters,
            string connectionId = "conn");

        Task<int> ExecuteScalarAsync<T>(
            string sql,
            T parameters,
            string connectionId = "conn");


    }


}
