using Dapper;
using FirebirdSql.Data.FirebirdClient;
using l2l_aggregator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database
{
    public abstract class BaseRepository
    {
        protected readonly DatabaseInitializer _dbService;

        protected BaseRepository(DatabaseInitializer dbService)
        {
            _dbService = dbService;
        }
        // ========================== Общие методы ==========================
        protected async Task<T> WithConnectionAsync<T>(Func<FbConnection, Task<T>> action)
        {
            await using var connection = _dbService.GetConnection();
            return await action(connection);
        }

        protected async Task WithConnectionAsync(Func<FbConnection, Task> action)
        {
            await using var connection = _dbService.GetConnection();
            await action(connection);
        }
    }
}
