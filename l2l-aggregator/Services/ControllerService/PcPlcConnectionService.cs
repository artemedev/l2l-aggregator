using Avalonia.Threading;
using NModbus;
using NModbus.Utility;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.ControllerService
{
    public class PcPlcConnectionService : IDisposable
    {
        private readonly SessionService _session;
        private readonly byte _slaveId = 1;
        private DispatcherTimer _timer;
        private bool _isActive;
        private bool _disposed;

        public bool IsConnected { get; private set; }
        //private async Task<IModbusMaster> ConnectAsync()
        //{
        //    var client = new TcpClient();
        //    await client.ConnectAsync(_session.ControllerIP, 502);
        //    var factory = new ModbusFactory();
        //    return factory.CreateMaster(client);
        //}
        //public async Task<ushort> ReadUInt16HoldingAsync(ushort register)
        //{
        //    using var master = await ConnectAsync();
        //    var data = await master.ReadHoldingRegistersAsync(1, register, 1);
        //    return data[0];
        //}


        //public async Task WriteUInt16HoldingAsync(ushort register, ushort value)
        //{
        //    using var master = await ConnectAsync();
        //    await master.WriteSingleRegisterAsync(1, register, value);
        //}
        //public async Task WriteBoolHoldingAsync(ushort register, int bitIndex, bool value)
        //{
        //    using var master = await ConnectAsync();
        //    var current = await master.ReadHoldingRegistersAsync(1, register, 1);
        //    ushort modified = ModbusUtility.SetBool(current[0], bitIndex, value);
        //    await master.WriteSingleRegisterAsync(1, register, modified);
        //}
        //public async Task<bool> ReadBoolInputAsync(ushort register, int bitIndex)
        //{
        //    using var master = await ConnectAsync();
        //    var data = await master.ReadInputRegistersAsync(1, register, 1);
        //    return ModbusUtility.GetBool(data[0], bitIndex);
        //}

        //public async Task<bool> WaitForBoolInputBitAsync(ushort register, int bitIndex, int timeoutMs = 5000)
        //{
        //    var start = DateTime.UtcNow;
        //    while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        //    {
        //        if (await ReadBoolInputAsync(register, bitIndex))
        //            return true;
        //        await Task.Delay(100);
        //    }
        //    return false;
        //}

        public PcPlcConnectionService(SessionService session)
        {
            _session = session;
        }

        /// <summary>
        /// Запускает пинг-понг и включает контроль активности.
        /// Используется в AggregationViewModel.
        /// </summary>
        public async Task StartAsync()
        {
            if (_isActive) return;
            _isActive = true;

            await EnablePingPongAsync();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _timer.Tick += async (_, __) => await PingAsync();
            _timer.Start();
        }

        /// <summary>
        /// Останавливает пинг-понг и сбрасывает активность ПЛК.
        /// </summary>
        public async Task StopAsync()
        {
            _isActive = false;
            _timer?.Stop();
            await DisablePingPongAsync();
        }

        /// <summary>
        /// Одноразовая проверка соединения, используется для валидации IP (в TaskDetailsViewModel, настройках и т.п.).
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var client = new TcpClient(_session.ControllerIP, 502);
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                await EnablePingPongAsync(master);
                await Task.Delay(1000);

                await PerformPingExchangeAsync(master);
                return IsConnected;
            }
            catch
            {
                return false;
            }
        }

        private async Task EnablePingPongAsync()
        {
            using var client = new TcpClient(_session.ControllerIP, 502);
            var factory = new ModbusFactory();
            var master = factory.CreateMaster(client);
            await EnablePingPongAsync(master);
        }

        private async Task EnablePingPongAsync(IModbusMaster master)
        {
            // Включить бит 4x0.12
            await master.WriteSingleCoilAsync(_slaveId, 12, true);

            // Установить бит 12 в 4x0 (контроль активности включен)
            var reg = await master.ReadHoldingRegistersAsync(_slaveId, 0, 1);
            ushort value = (ushort)(reg[0] | (1 << 12));
            await master.WriteSingleRegisterAsync(_slaveId, 0, value);
        }

        private async Task PingAsync()
        {
            try
            {
                using var client = new TcpClient(_session.ControllerIP, 502);
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                await PerformPingExchangeAsync(master);
            }
            catch
            {
                IsConnected = false;
            }
        }

        private async Task PerformPingExchangeAsync(IModbusMaster master)
        {
            // Получить уставку таймаута (4x17)
            var timeoutReg = await master.ReadHoldingRegistersAsync(_slaveId, 16, 1);
            ushort timeoutMs = timeoutReg[0];
            if (timeoutMs == 0 || timeoutMs > 10000)
                timeoutMs = 3000;

            await Task.Delay(Math.Min(timeoutMs / 3, 1000));

            // Сбросить бит активности ПК: бит 10 в 4x0
            var reg = await master.ReadHoldingRegistersAsync(_slaveId, 0, 1);
            ushort value = (ushort)(reg[0] & ~(1 << 10));
            await master.WriteSingleRegisterAsync(_slaveId, 0, value);

            // Проверить 3x0.11 (pong) и 3x0.13 (ПК активен)
            var inputs = await master.ReadInputsAsync(_slaveId, 0, 14);
            bool pong = inputs[11];
            bool pcActive = inputs[13];

            if (!pong || !pcActive)
            {
                IsConnected = false;
                return;
            }

            // Установить флаг активности ПЛК: бит 11 в 4x0
            reg = await master.ReadHoldingRegistersAsync(_slaveId, 0, 1);
            value = (ushort)(reg[0] | (1 << 11));
            await master.WriteSingleRegisterAsync(_slaveId, 0, value);

            IsConnected = true;
        }

        private async Task DisablePingPongAsync()
        {
            try
            {
                using var client = new TcpClient(_session.ControllerIP, 502);
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                // Отключить пинг-понг: сброс 4x0.12
                await master.WriteSingleCoilAsync(_slaveId, 12, false);

                // Сбросить флаг активности ПЛК: бит 11 в 4x0
                var reg = await master.ReadHoldingRegistersAsync(_slaveId, 0, 1);
                ushort value = (ushort)(reg[0] & ~(1 << 11));
                await master.WriteSingleRegisterAsync(_slaveId, 0, value);
            }
            catch
            {
                // fail silently
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Stop();
            _timer = null;
        }

        public class CameraPlcSettings
        {
            public int OffsetFromZeroPosition { get; set; }
            public int PositioningTimeout { get; set; }
            public int ZeroToHomeDistance { get; set; }
            public bool ArrowDirectionReversed { get; set; }
            public int MovementSpeed { get; set; }
            public int MinCameraToTableDistance { get; set; }

            public int LightIntensity { get; set; }
            public int LightDelay { get; set; }
            public int LightExposure { get; set; }
            public int TriggerDelay { get; set; }
            public int LightMode { get; set; }
            public int PcTimeout { get; set; }
        }

        public async Task<CameraPlcSettings?> ReadCameraSettingsAsync()
        {
            try
            {
                using var client = new TcpClient(_session.ControllerIP, 502);
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                // Пример: читаем с 4x7 (index = 6), 12 регистров
                var regs = await master.ReadHoldingRegistersAsync(_slaveId, 6, 12);

                return new CameraPlcSettings
                {
                    OffsetFromZeroPosition = regs[0],
                    PositioningTimeout = regs[1],
                    ZeroToHomeDistance = regs[2],
                    ArrowDirectionReversed = regs[3] != 0,
                    MovementSpeed = regs[4],
                    MinCameraToTableDistance = regs[5],
                    LightIntensity = regs[6],
                    LightDelay = regs[7],
                    LightExposure = regs[8],
                    TriggerDelay = regs[9],
                    LightMode = regs[10],
                    PcTimeout = regs[11]
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> WriteCameraSettingsAsync(CameraPlcSettings settings)
        {
            try
            {
                using var client = new TcpClient(_session.ControllerIP, 502);
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                // Подготовим значения для записи
                ushort[] values = new ushort[]
                {
                    (ushort)settings.OffsetFromZeroPosition,
                    (ushort)settings.PositioningTimeout,
                    (ushort)settings.ZeroToHomeDistance,
                    (ushort)(settings.ArrowDirectionReversed ? 1 : 0),
                    (ushort)settings.MovementSpeed,
                    (ushort)settings.MinCameraToTableDistance,
                    (ushort)settings.LightIntensity,
                    (ushort)settings.LightDelay,
                    (ushort)settings.LightExposure,
                    (ushort)settings.TriggerDelay,
                    (ushort)settings.LightMode,
                    (ushort)settings.PcTimeout
                };

                // Записываем начиная с 4x7 (адрес 6), 12 регистров
                await master.WriteMultipleRegistersAsync(_slaveId, 6, values);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<List<string>> ReadNonFatalErrorsAsync()
        {
            try
            {
                using var client = new TcpClient(_session.ControllerIP, 502);
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                // 3x4 — Input-регистр с некритическими ошибками (word)
                var regs = await master.ReadInputRegistersAsync(_slaveId, 4, 1);
                ushort word = regs[0];
                return PlcErrorDecoder.DecodeNonFatalErrors(word);
            }
            catch
            {
                return new List<string> { "Ошибка чтения не критических ошибок." };
            }
        }

        public async Task<List<string>> ReadFatalErrorsAsync()
        {
            try
            {
                using var client = new TcpClient(_session.ControllerIP, 502);
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                // 3x5 — Критические ошибки, 3x6 — Final (объединяем)
                var regs = await master.ReadInputRegistersAsync(_slaveId, 5, 2);
                ushort regular = regs[0];
                ushort final = regs[1];
                ushort combined = (ushort)(regular | final);
                return PlcErrorDecoder.DecodeFatalErrors(combined);
            }
            catch
            {
                return new List<string> { "Ошибка чтения критических ошибок." };
            }
        }
    }
}