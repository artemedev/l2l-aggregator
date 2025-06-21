// RemoteDatabaseService.cs - обновленный для работы с хранимыми процедурами
using Dapper;
using FirebirdSql.Data.FirebirdClient;
using l2l_aggregator.Models;
using l2l_aggregator.Services.Database.Repositories.Interfaces;
using l2l_aggregator.Services.Notification.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database
{
    public class RemoteDatabaseService
    {
        private readonly IConfigRepository _configRepository;
        private readonly INotificationService _notificationService;
        //private string? _connectionString;
        private long? _currentSessionId;
        private long? _currentDeviceId;
        private readonly string _connectionString = "Server=172.16.3.237;Port=3050;Database=/DATA_SSD/fb/arm_mtd_test.fdb;User=AEEGOROV;Password=AEE123456;Charset=UTF8;";
        //private readonly string _connectionString = "Database=172.16.3.237:arm_mtd_test; User=AEEGOROV; Password=AEE123456; Role=RDB$ADMIN";
        public RemoteDatabaseService(IConfigRepository configRepository, INotificationService notificationService)
        {
            _configRepository = configRepository;
            _notificationService = notificationService;
        }

        public async Task<bool> InitializeConnectionAsync()
        {
            try
            {
                //var databaseUri = await _configRepository.GetConfigValueAsync("DatabaseUri");
                //if (string.IsNullOrWhiteSpace(databaseUri))
                //{
                //    _notificationService.ShowMessage("Адрес базы данных не настроен!", NotificationType.Error);
                //    return false;
                //}

                //_connectionString = databaseUri;
                _notificationService.ShowMessage($"Подключение к БД", NotificationType.Info);
                return await TestConnectionAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка инициализации подключения к БД: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                return false;

            try
            {
                //using var connection = new FbConnection(_connectionString)
                //{
                //    connection
                //}
                //connection.Open();
                using (FbConnection connection = new FbConnection(_connectionString))
                {
                   await connection.OpenAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка подключения к удаленной БД: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        public async Task SetConnectionStringAsync(string connectionString)
        {
           // _connectionString = connectionString;
            await _configRepository.SetConfigValueAsync("DatabaseUri", connectionString);
        }

        private async Task<T> WithConnectionAsync<T>(Func<FbConnection, Task<T>> action)
        {
            //if (string.IsNullOrWhiteSpace(_connectionString))
            //    throw new InvalidOperationException("Строка подключения не установлена");

            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            return await action(connection);
        }

        private async Task WithConnectionAsync(Func<FbConnection, Task> action)
        {
            //if (string.IsNullOrWhiteSpace(_connectionString))
            //    throw new InvalidOperationException("Строка подключения не установлена");

            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            await action(connection);
        }

        // ---------------- AUTH ----------------
        public async Task<UserAuthResponse?> LoginAsync(string login, string password)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    var sql = "SELECT * FROM MARK_ARM_USER_AUTH(@USER_IDENT, @USER_PASSWD)";

                    var result = await conn.QueryFirstOrDefaultAsync(sql, new
                    {
                        USER_IDENT = login,
                        USER_PASSWD = password
                    });

                    if (result != null)
                    {
                        return new UserAuthResponse
                        {
                            USERID = result.USERID?.ToString(),
                            USER_NAME = result.USER_NAME,
                            PERSONID = result.PERSONID?.ToString(),
                            PERSON_NAME = result.PERSON_NAME,
                            PERSON_DELETE_FLAG = result.PERSON_DELETE_FLAG?.ToString(),
                            AUTH_OK = result.AUTH_OK?.ToString(),
                            ERROR_TEXT = result.ERROR_TEXT,
                            NEED_CHANGE_FLAG = result.NEED_CHANGE_FLAG?.ToString(),
                            EXPIRATION_DATE = result.EXPIRATION_DATE?.ToString("dd.MM.yyyy HH:mm:ss"),
                            REC_TYPE = result.REC_TYPE?.ToString()
                        };
                    }

                    return null;
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка авторизации: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        // Проверка прав администратора
        public async Task<bool> CheckAdminRoleAsync(long userId)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    var sql = "SELECT * FROM ACL_CHECK_ADMIN_ROLE(@USERID)";

                    var result = await conn.QueryFirstOrDefaultAsync(sql, new { USERID = userId });

                    return result?.RES == 1;
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка проверки прав администратора: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        // Регистрация устройства
        public async Task<ArmDeviceRegistrationResponse?> RegisterDeviceAsync(ArmDeviceRegistrationRequest data)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    var sql = @"SELECT * FROM MARK_ARM_DEVICE_REGISTER(
                        @NAME, @MAC_ADDRESS, @SERIAL_NUMBER, @NET_ADDRESS, 
                        @KERNEL_VERSION, @HARDWARE_VERSION, @SOFTWARE_VERSION, 
                        @FIRMWARE_VERSION, @DEVICE_TYPE)";

                    var result = await conn.QueryFirstOrDefaultAsync(sql, new
                    {
                        NAME = data.NAME,
                        MAC_ADDRESS = data.MAC_ADDRESS,
                        SERIAL_NUMBER = data.SERIAL_NUMBER,
                        NET_ADDRESS = data.NET_ADDRESS,
                        KERNEL_VERSION = data.KERNEL_VERSION,
                        HARDWARE_VERSION = data.HADWARE_VERSION, // Исправляем опечатку
                        SOFTWARE_VERSION = data.SOFTWARE_VERSION,
                        FIRMWARE_VERSION = data.FIRMWARE_VERSION,
                        DEVICE_TYPE = data.DEVICE_TYPE
                    });

                    if (result != null)
                    {
                        _currentDeviceId = result.DEVICEID;

                        return new ArmDeviceRegistrationResponse
                        {
                            DEVICEID = result.DEVICEID?.ToString(),
                            DEVICE_NAME = result.DEVICE_NAME,
                            LICENSE_DATA = result.LICENSE_DATA?.ToString(),
                            SETTINGS_DATA = result.SETTINGS_DATA?.ToString()
                        };
                    }

                    return null;
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка регистрации устройства: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        // ---------------- JOB LIST ----------------
        public async Task<ArmJobResponse?> GetJobsAsync(string userId)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    var sql = "SELECT * FROM MARK_ARM_JOB_GET(@IN_USERID)";

                    var records = await conn.QueryAsync(sql, new { IN_USERID = long.Parse(userId) });

                    var armJobRecords = records.Select(r => new ArmJobRecord
                    {
                        DOCID = r.DOCID,
                        RESOURCEID = r.RESOURCEID,
                        SERIESID = r.SERIESID,
                        RES_BOXID = r.RES_BOXID,
                        DOC_ORDER = r.DOC_ORDER,
                        DOCDATE = r.DOCDATE?.ToString("dd.MM.yyyy"),
                        MOVEDATE = r.MOVEDATE?.ToString("dd.MM.yyyy"),
                        BUHDATE = r.BUHDATE?.ToString("dd.MM.yyyy"),
                        FIRMID = r.FIRMID,
                        DOC_NUM = r.DOC_NUM,
                        DEPART_NAME = r.DEPART_NAME,
                        RESOURCE_NAME = r.RESOURCE_NAME,
                        RESOURCE_ARTICLE = r.RESOURCE_ARTICLE,
                        SERIES_NAME = r.SERIES_NAME,
                        RES_BOX_NAME = r.RES_BOX_NAME,
                        GTIN = r.GTIN,
                        EXPIRE_DATE_VAL = r.EXPIRE_DATE_VAL?.ToString("dd.MM.yyyy"),
                        MNF_DATE_VAL = r.MNF_DATE_VAL?.ToString("dd.MM.yyyy"),
                        DOC_TYPE = r.DOC_TYPE,
                        AGREGATION_CODE = r.AGREGATION_CODE,
                        AGREGATION_TYPE = r.AGREGATION_TYPE,
                        CRYPTO_CODE_FLAG = r.CRYPTO_CODE_FLAG,
                        ERROR_FLAG = r.ERROR_FLAG,
                        FIRM_NAME = r.FIRM_NAME,
                        QTY = r.QTY,
                        AGGR_FLAG = r.AGGR_FLAG,
                        UN_TEMPLATEID = r.UN_TEMPLATEID,
                        UN_RESERVE_DOCID = r.UN_RESERVE_DOCID
                    }).ToList();

                    return new ArmJobResponse { RECORDSET = armJobRecords };
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения списка заданий: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        // Загрузка задания
        public async Task<bool> LoadJobAsync(long jobId)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    var sql = "SELECT COUNT(*) FROM ARM_JOB_LOAD(@JOBID)";

                    var result = await conn.QueryFirstOrDefaultAsync<int>(sql, new { JOBID = jobId });

                    return result == 1;
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка загрузки задания: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        // ---------------- JOB DETAILS ----------------
        public async Task<ArmJobInfoRecord?> GetJobDetailsAsync(long docId)
        {
            try
            {
                // Сначала загружаем задание
                var loadResult = await LoadJobAsync(docId);
                if (!loadResult)
                {
                    _notificationService.ShowMessage("Не удалось загрузить задание", NotificationType.Error);
                    return null;
                }

                return await WithConnectionAsync(async conn =>
                {
                    // Теперь получаем данные из таблицы ARM_TASK, куда они загружены после ARM_JOB_LOAD
                    var sql = @"SELECT 
                DOCID, RESOURCEID, SERIESID, RES_BOXID, DOC_ORDER, DOCDATE, MOVEDATE, BUHDATE,
                FIRMID, DOC_NUM, DEPART_NAME, RESOURCE_NAME, RESOURCE_ARTICLE, SERIES_NAME,
                RES_BOX_NAME, GTIN, EXPIRE_DATE_VAL, MNF_DATE_VAL, DOC_TYPE, AGREGATION_CODE,
                AGREGATION_TYPE, CRYPTO_CODE_FLAG, FIRM_NAME, QTY, AGGR_FLAG, UN_TEMPLATE,
                UN_TEMPLATEID, UN_RESERVE_DOCID, IN_BOX_QTY, IN_INNER_BOX_QTY, INNER_BOX_FLAG,
                INNER_BOX_AGGR_FLAG, INNER_BOX_QTY, IN_PALLET_BOX_QTY, LAST_PACKAGE_LOCATION_INFO,
                PALLET_NOT_USE_FLAG, PALLET_AGGR_FLAG, AGREGATION_TYPEID, SERIES_SYS_NUM,
                LAYERS_QTY, LAYER_ROW_QTY, LAYER_ROWS_QTY, PACK_HEIGHT, PACK_WIDTH, PACK_LENGTH,
                PACK_WEIGHT, PACK_CODE_POSITION, BOX_TEMPLATEID, BOX_RESERVE_DOCID, BOX_TEMPLATE,
                PALLETE_TEMPLATEID, PALLETE_RESERVE_DOCID, PALLETE_TEMPLATE, INT_BOX_TEMPLATEID,
                INT_BOX_RESERVE_DOCID, INT_BOX_TEMPLATE, UN_TEMPLATE_FR, LOAD_START_TIME,
                LOAD_FINISH_TIME, EXPIRE_DATE, MNF_DATE
                FROM ARM_TASK 
                WHERE DOCID = @DOCID";

                    var record = await conn.QueryFirstOrDefaultAsync(sql, new { DOCID = docId });

                    if (record != null)
                    {
                        return new ArmJobInfoRecord
                        {
                            DOCID = record.DOCID,
                            RESOURCEID = record.RESOURCEID,
                            SERIESID = record.SERIESID,
                            RES_BOXID = record.RES_BOXID,
                            DOC_ORDER = record.DOC_ORDER,
                            DOCDATE = record.DOCDATE?.ToString("dd.MM.yyyy"),
                            MOVEDATE = record.MOVEDATE?.ToString("dd.MM.yyyy"),
                            BUHDATE = record.BUHDATE?.ToString("dd.MM.yyyy"),
                            FIRMID = record.FIRMID,
                            DOC_NUM = record.DOC_NUM,
                            DEPART_NAME = record.DEPART_NAME,
                            RESOURCE_NAME = record.RESOURCE_NAME,
                            RESOURCE_ARTICLE = record.RESOURCE_ARTICLE,
                            SERIES_NAME = record.SERIES_NAME,
                            RES_BOX_NAME = record.RES_BOX_NAME,
                            GTIN = record.GTIN,
                            EXPIRE_DATE_VAL = record.EXPIRE_DATE_VAL?.ToString("dd.MM.yyyy"),
                            MNF_DATE_VAL = record.MNF_DATE_VAL?.ToString("dd.MM.yyyy"),
                            DOC_TYPE = record.DOC_TYPE,
                            AGREGATION_CODE = record.AGREGATION_CODE,
                            AGREGATION_TYPE = record.AGREGATION_TYPE,
                            CRYPTO_CODE_FLAG = record.CRYPTO_CODE_FLAG,
                            FIRM_NAME = record.FIRM_NAME,
                            QTY = record.QTY,
                            AGGR_FLAG = record.AGGR_FLAG,
                            UN_TEMPLATE = record.UN_TEMPLATE,
                            UN_TEMPLATEID = record.UN_TEMPLATEID,
                            UN_RESERVE_DOCID = record.UN_RESERVE_DOCID,
                            UN_TEMPLATE_FR = record.UN_TEMPLATE_FR,

                            // Дополнительные поля из ARM_TASK
                            IN_BOX_QTY = record.IN_BOX_QTY,
                            IN_INNER_BOX_QTY = record.IN_INNER_BOX_QTY,
                            INNER_BOX_FLAG = record.INNER_BOX_FLAG,
                            INNER_BOX_AGGR_FLAG = record.INNER_BOX_AGGR_FLAG,
                            INNER_BOX_QTY = record.INNER_BOX_QTY,
                            IN_PALLET_BOX_QTY = record.IN_PALLET_BOX_QTY,
                            LAST_PACKAGE_LOCATION_INFO = record.LAST_PACKAGE_LOCATION_INFO,
                            PALLET_NOT_USE_FLAG = record.PALLET_NOT_USE_FLAG,
                            PALLET_AGGR_FLAG = record.PALLET_AGGR_FLAG,
                            AGREGATION_TYPEID = record.AGREGATION_TYPEID,
                            SERIES_SYS_NUM = record.SERIES_SYS_NUM,
                            LAYERS_QTY = record.LAYERS_QTY,
                            LAYER_ROW_QTY = record.LAYER_ROW_QTY,
                            LAYER_ROWS_QTY = record.LAYER_ROWS_QTY,
                            PACK_HEIGHT = record.PACK_HEIGHT,
                            PACK_WIDTH = record.PACK_WIDTH,
                            PACK_LENGTH = record.PACK_LENGTH,
                            PACK_WEIGHT = record.PACK_WEIGHT,
                            PACK_CODE_POSITION = record.PACK_CODE_POSITION,
                            BOX_TEMPLATEID = record.BOX_TEMPLATEID,
                            BOX_RESERVE_DOCID = record.BOX_RESERVE_DOCID,
                            BOX_TEMPLATE = record.BOX_TEMPLATE,
                            PALLETE_TEMPLATEID = record.PALLETE_TEMPLATEID,
                            PALLETE_RESERVE_DOCID = record.PALLETE_RESERVE_DOCID,
                            PALLETE_TEMPLATE = record.PALLETE_TEMPLATE,
                            INT_BOX_TEMPLATEID = record.INT_BOX_TEMPLATEID,
                            INT_BOX_RESERVE_DOCID = record.INT_BOX_RESERVE_DOCID,
                            INT_BOX_TEMPLATE = record.INT_BOX_TEMPLATE,
                            LOAD_START_TIME = record.LOAD_START_TIME?.ToString("dd.MM.yyyy HH:mm:ss"),
                            LOAD_FINISH_TIME = record.LOAD_FINISH_TIME?.ToString("dd.MM.yyyy HH:mm:ss"),
                            EXPIRE_DATE = record.EXPIRE_DATE,
                            MNF_DATE = record.MNF_DATE
                        };
                    }

                    return null;
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения деталей задания: {ex.Message}", NotificationType.Error);
                return null;
            }
        }
        public async Task<ArmJobSgtinResponse?> GetSgtinAsync(long docId)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    // Предполагаем, что есть отдельная процедура для получения SGTIN
                    // Если нет, нужно будет создать или использовать прямой запрос к таблице
                    var sql = @"SELECT 
                        UNID, BARCODE, UN_CODE, GS1FIELD91, GS1FIELD92, GS1FIELD93,
                        UN_CODE_STATEID, UN_CODE_STATE
                        FROM ARM_JOB_SGTIN WHERE DOCID = @docId";

                    var records = await conn.QueryAsync(sql, new { docId });

                    var sgtinRecords = records.Select(r => new ArmJobSgtinRecord
                    {
                        UNID = r.UNID,
                        BARCODE = r.BARCODE,
                        UN_CODE = r.UN_CODE,
                        GS1FIELD91 = r.GS1FIELD91,
                        GS1FIELD92 = r.GS1FIELD92,
                        GS1FIELD93 = r.GS1FIELD93,
                        UN_CODE_STATEID = r.UN_CODE_STATEID,
                        UN_CODE_STATE = r.UN_CODE_STATE
                    }).ToList();

                    return new ArmJobSgtinResponse { RECORDSET = sgtinRecords };
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения SGTIN: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        public async Task<ArmJobSsccResponse?> GetSsccAsync(long docId)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    // Предполагаем, что есть отдельная процедура для получения SSCC
                    var sql = @"SELECT 
                        SSCCID, SERIESID, ORDER_NUM, TYPEID, CHECK_BAR_CODE,
                        DISPLAY_BAR_CODE, STATEID, CODE_STATE
                        FROM ARM_JOB_SSCC WHERE DOCID = @docId";

                    var records = await conn.QueryAsync(sql, new { docId });

                    var ssccRecords = records.Select(r => new ArmJobSsccRecord
                    {
                        SSCCID = r.SSCCID,
                        SERIESID = r.SERIESID,
                        ORDER_NUM = r.ORDER_NUM,
                        TYPEID = r.TYPEID,
                        CHECK_BAR_CODE = r.CHECK_BAR_CODE,
                        DISPLAY_BAR_CODE = r.DISPLAY_BAR_CODE,
                        STATEID = r.STATEID,
                        CODE_STATE = r.CODE_STATE
                    }).ToList();

                    return new ArmJobSsccResponse { RECORDSET = ssccRecords };
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения SSCC: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        // ---------------- SESSION MANAGEMENT ----------------
        public async Task<long?> StartSessionAsync(long docId, long userId)
        {
            try
            {
                if (!_currentDeviceId.HasValue)
                {
                    _notificationService.ShowMessage("Устройство не зарегистрировано", NotificationType.Error);
                    return null;
                }

                return await WithConnectionAsync(async conn =>
                {
                    var sql = @"SELECT * FROM MARK_ARM_SESSION_START(@DOCID, @DEVICEID, @USERID, @START_TIME)";

                    var result = await conn.QueryFirstOrDefaultAsync(sql, new
                    {
                        DOCID = docId,
                        DEVICEID = _currentDeviceId.Value,
                        USERID = userId,
                        START_TIME = DateTime.Now
                    });

                    if (result?.SESSIONID != null)
                    {
                        _currentSessionId = result.SESSIONID;
                        return _currentSessionId;
                    }

                    return null;
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка начала сессии: {ex.Message}", NotificationType.Error);
                return null;
            }
        }

        public async Task<bool> CloseSessionAsync(long userId)
        {
            try
            {
                if (!_currentSessionId.HasValue)
                    return true; // Сессия уже закрыта

                return await WithConnectionAsync(async conn =>
                {
                    var sql = @"EXECUTE PROCEDURE MARK_ARM_SESSION_CLOSE(@SESSIONID, @USERID, @FINISH_TIME)";

                    await conn.ExecuteAsync(sql, new
                    {
                        SESSIONID = _currentSessionId.Value,
                        USERID = userId,
                        FINISH_TIME = DateTime.Now
                    });

                    _currentSessionId = null;
                    return true;
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка закрытия сессии: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        // Логирование агрегации
        public async Task<bool> LogAggregationAsync(long docId)
        {
            try
            {
                if (!_currentSessionId.HasValue || !_currentDeviceId.HasValue)
                {
                    _notificationService.ShowMessage("Сессия или устройство не инициализированы", NotificationType.Error);
                    return false;
                }

                return await WithConnectionAsync(async conn =>
                {
                    var sql = @"EXECUTE PROCEDURE JOB_LOG_ADD(
                        @DOCID, @EVENT_TYPE, @EVENT_INFO, @SESSIONID, @DEVICEID,
                        @UNID, @STATEID, @PARENT_CODEID, null, null)";

                    await conn.ExecuteAsync(sql, new
                    {
                        DOCID = docId,
                        EVENT_TYPE = 200, // Код события "Агрегация"
                        EVENT_INFO = "Агрегация завершена успешно",
                        SESSIONID = _currentSessionId.Value,
                        DEVICEID = _currentDeviceId.Value,
                        UNID = (long?)null,
                        STATEID = (long?)null,
                        PARENT_CODEID = (long?)null
                    });

                    return true;
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка логирования агрегации: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        // Свойства для отслеживания состояния
        public long? CurrentSessionId => _currentSessionId;
        public long? CurrentDeviceId => _currentDeviceId;
        public string ConnectionString => _connectionString;
    }
}