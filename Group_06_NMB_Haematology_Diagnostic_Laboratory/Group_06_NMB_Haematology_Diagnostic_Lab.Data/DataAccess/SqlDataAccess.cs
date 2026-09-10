using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess
{
    public class SqlDataAccess : ISqlDataAccess
    {
        private readonly IConfiguration _config;

        public SqlDataAccess(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IEnumerable<T>> GetData<T, P>(string spName, P parameters, string connectionId = "conn")
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            return await connection.QueryAsync<T>(spName, parameters, commandType: CommandType.StoredProcedure);
        }


        // ===============================================================================
        // GET DATA OVERLOAD METHODS

        // NEW - Two object mapping
        public async Task<IEnumerable<TReturn>> GetData<TFirst, TSecond, TReturn, P>(
        string spName,
        Func<TFirst, TSecond, TReturn> map,
        P parameters,
        string splitOn = "Id",
        string connectionId = "conn")
        {
            using IDbConnection connection =
                new SqlConnection(_config.GetConnectionString(connectionId));

            return await connection.QueryAsync(
                spName,
                map,
                param: parameters,
                splitOn: splitOn,
                commandType: CommandType.StoredProcedure);
        }


        // NEW - Three object mapping
        public async Task<IEnumerable<TReturn>> GetData<TFirst, TSecond, TThird, TReturn, P>(
        string spName,
        Func<TFirst, TSecond, TThird, TReturn> map,
        P parameters,
        string splitOn = "Id",
        string connectionId = "conn")
        {
            using IDbConnection connection =
                new SqlConnection(_config.GetConnectionString(connectionId));

            return await connection.QueryAsync(
                spName,
                map,
                param: parameters,
                splitOn: splitOn,
                commandType: CommandType.StoredProcedure);
        }

        // NEW - Four object mapping
        public async Task<IEnumerable<TReturn>> GetData<TFirst, TSecond, TThird, TFourth, TReturn, P>(
        string spName,
        Func<TFirst, TSecond, TThird, TFourth, TReturn> map,
        P parameters,
        string splitOn = "Id",
        string connectionId = "conn")
        {
            using IDbConnection connection =
                new SqlConnection(_config.GetConnectionString(connectionId));

            return await connection.QueryAsync(
                spName,
                map,
                param: parameters,
                splitOn: splitOn,
                commandType: CommandType.StoredProcedure);
        }

		// ==========================================================================================


		public async Task<T> SaveDataAndReturnWithTable<T>(
	string spName,
	DynamicParameters parameters,
	string connectionId = "conn")
		{
			using IDbConnection connection =
				new SqlConnection(_config.GetConnectionString(connectionId));

			return await connection.ExecuteScalarAsync<T>(
				spName,
				parameters,
				commandType: CommandType.StoredProcedure);
		}

		public async Task SaveData<T>(string spName, T parameters, string connectionId = "conn")
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            await connection.ExecuteAsync(spName, parameters, commandType: CommandType.StoredProcedure);
        }
        // ===============================================================================
        // SAVE DATA OVERLOAD METHODS

        public async Task<T> SaveDataAndReturn<T, P>(
        string spName,
        P parameters,
         string connectionId = "conn")
        {
            using IDbConnection connection =
                new SqlConnection(_config.GetConnectionString(connectionId));

            return await connection.ExecuteScalarAsync<T>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        // ======================== TRANSACTION METHOD ================================
        public async Task ExecuteInTransaction(
        Func<IDbConnection, IDbTransaction, Task> action)
        {
            using var connection =
                new SqlConnection(_config.GetConnectionString("conn"));

            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                await action(connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        //===========================================================================
    }

}
