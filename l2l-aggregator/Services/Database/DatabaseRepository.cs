using Dapper;
using FirebirdSql.Data.FirebirdClient;
using l2l_aggregator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database
{
    public class DatabaseRepository
    {
        private readonly DatabaseService _dbService;

        public DatabaseRepository(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public string GetConfigValue(string key)
        {
            using (FbConnection connection = _dbService.GetConnection())
            {
                return connection.Query<string>(
                "SELECT CONFIG_VALUE FROM CONFIG_INFO WHERE CONFIG_KEY = @key",
                    new { key }
                ).FirstOrDefault();
            }
        }

        public void SetConfigValue(string key, string value)
        {
            using (FbConnection connection = _dbService.GetConnection())
            {
                //connection.Execute("UPDATE OR INSERT INTO CONFIG_INFO (CONFIG_KEY, CONFIG_VALUE) VALUES (@key, @value)", new { key, value });
                connection.Execute(@"UPDATE OR INSERT INTO CONFIG_INFO (CONFIG_KEY, CONFIG_VALUE) VALUES (@key, @value) MATCHING (CONFIG_KEY);", new { key, value });
            }
        }

        public void SaveRegistration(ArmDeviceRegistrationResponse response)
        {
            using (FbConnection connection = _dbService.GetConnection())
            {
                connection.Execute(@"INSERT INTO REGISTRATION_INFO 
                (DEVICEID, DEVICE_NAME, LICENSE_DATA, SETTINGS_DATA)
                VALUES (@DEVICEID, @DEVICE_NAME, @LICENSE_DATA, @SETTINGS_DATA)",
                new
                {
                    DEVICEID = response.DEVICEID,
                    DEVICE_NAME = response.DEVICE_NAME,
                    LICENSE_DATA = response.LICENSE_DATA,
                    SETTINGS_DATA = response.SETTINGS_DATA
                });
            }
        }
        public void SaveUserAuth(UserAuthResponse response)
        {
            using (FbConnection connection = _dbService.GetConnection())
            {
                var existingRecord = connection.ExecuteScalar<int>(@"SELECT COUNT(*) FROM USER_AUTH_INFO WHERE USERID = @USERID",
                    new { USERID = response.USERID });
                if (existingRecord > 0)
                {
                    connection.Execute(@"UPDATE USER_AUTH_INFO SET 
                                    USER_NAME = @USER_NAME, 
                                    PERSONID = @PERSONID, 
                                    PERSON_NAME = @PERSON_NAME, 
                                    PERSON_DELETE_FLAG = @PERSON_DELETE_FLAG, 
                                    AUTH_OK = @AUTH_OK, 
                                    ERROR_TEXT = @ERROR_TEXT, 
                                    NEED_CHANGE_FLAG = @NEED_CHANGE_FLAG, 
                                    EXPIRATION_DATE = @EXPIRATION_DATE, 
                                    REC_TYPE = @REC_TYPE
                                    WHERE USERID = @USERID",
                    new
                    {
                        USERID = response.USERID,
                        USER_NAME = response.USER_NAME,
                        PERSONID = response.PERSONID,
                        PERSON_NAME = response.PERSON_NAME,
                        PERSON_DELETE_FLAG = response.PERSON_DELETE_FLAG,
                        AUTH_OK = response.AUTH_OK,
                        ERROR_TEXT = response.ERROR_TEXT,
                        NEED_CHANGE_FLAG = response.NEED_CHANGE_FLAG,
                        EXPIRATION_DATE = DateTime.ParseExact(response.EXPIRATION_DATE, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                        REC_TYPE = response.REC_TYPE
                    });
                }
                else
                {
                    connection.Execute(@"INSERT INTO USER_AUTH_INFO 
                                    (USERID, USER_NAME, PERSONID, PERSON_NAME, PERSON_DELETE_FLAG, 
                                    AUTH_OK, ERROR_TEXT, NEED_CHANGE_FLAG, EXPIRATION_DATE, REC_TYPE)
                                    VALUES (@USERID, @USER_NAME, @PERSONID, @PERSON_NAME, @PERSON_DELETE_FLAG, 
                                    @AUTH_OK, @ERROR_TEXT, @NEED_CHANGE_FLAG, @EXPIRATION_DATE, @REC_TYPE)",
                    new
                    {
                        USERID = response.USERID,
                        USER_NAME = response.USER_NAME,
                        PERSONID = response.PERSONID,
                        PERSON_NAME = response.PERSON_NAME,
                        PERSON_DELETE_FLAG = response.PERSON_DELETE_FLAG,
                        AUTH_OK = response.AUTH_OK,
                        ERROR_TEXT = response.ERROR_TEXT,
                        NEED_CHANGE_FLAG = response.NEED_CHANGE_FLAG,
                        EXPIRATION_DATE = DateTime.ParseExact(response.EXPIRATION_DATE, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                        REC_TYPE = response.REC_TYPE
                    });
                }
            }
        }
        public List<UserAuthResponse> GetUserAuth()
        {
            using (FbConnection connection = _dbService.GetConnection())
            {
                return connection.Query<UserAuthResponse>(@"SELECT 
                                    USERID, 
                                    USER_NAME, 
                                    PERSONID, 
                                    PERSON_NAME, 
                                    PERSON_DELETE_FLAG, 
                                    AUTH_OK, 
                                    ERROR_TEXT, 
                                    NEED_CHANGE_FLAG, 
                                    EXPIRATION_DATE, 
                                    REC_TYPE 
                                FROM USER_AUTH_INFO").ToList();
            }
        }
        public string GetUserId()
        {
            using (FbConnection connection = _dbService.GetConnection())
            {
                return connection.Query<string>(@"SELECT 
                                    USERID
                                FROM USER_AUTH_INFO").LastOrDefault();
            }
        }
        public bool ValidateAdminUser(string username, string password)
        {
            using (FbConnection connection = _dbService.GetConnection())
            {
                var storedPassword = connection.Query<string>(
                    "SELECT PASSWORD FROM ADMIN_USERS WHERE USERNAME = @username",
                    new { username }
                ).FirstOrDefault();

                return storedPassword == password; // Direct comparison (not hashed)
            }
        }
        public void SaveScannerDevice(ScannerDevice scanner)
        {
            SetConfigValue("ScannerId", scanner.Id);
            //SetConfigValue("ScannerType", scanner.Type);
            //SetConfigValue("ScannerName", scanner.DisplayName);
        }
        public ScannerDevice LoadScannerDevice()
        {
            var id = GetConfigValue("ScannerId");
            if (string.IsNullOrWhiteSpace(id)) return null;

            return new ScannerDevice
            {
                Id = id,
            };
        }
    }
}
