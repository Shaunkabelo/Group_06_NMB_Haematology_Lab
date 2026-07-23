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

        //     ==============================================================================






        Task SaveData<T>(string spName, T parameters, string connectionId = "conn");
        // ===============================================================================
        //  SAVE DATA OVERLOAD METHODS
        Task<T> SaveDataAndReturn<T, P>(string spName, P parameters, string connectionId = "conn");

        // ===========================================================================================






        // ======================== TRANSACTION METHOD ================================
        Task ExecuteInTransaction(Func<IDbConnection, IDbTransaction, Task> action);
          //===========================================================================





    }


}
