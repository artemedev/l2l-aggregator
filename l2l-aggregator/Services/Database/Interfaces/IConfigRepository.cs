using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database.Interfaces
{
    public interface IConfigRepository
    {
        Task<string> GetConfigValueAsync(string key);
        Task SetConfigValueAsync(string key, string value);
    }
}
