using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient.Model;

namespace LudiscanApiClient.Runtime.ApiClient
{
    public class PositionLogger
    {
        private int writeIndex;
        PositionEntry[] buffer;

        public PositionEntry[] Buffer => buffer;
        private int recordIntervalMilli;

        public bool IsLoggingStarted { get; private set; }

        public Func<List<PositionEntry>> OnLogPosition { get; set; }
        public PositionLogger(int _bufferSize)
        {
            buffer = new PositionEntry[_bufferSize];
            writeIndex = 0;
            IsLoggingStarted = false;
        }

        public void StartLogging(int _recordIntervalMilli)
        {
            if (IsLoggingStarted)
            {
                return;
            }
            IsLoggingStarted = true;
            recordIntervalMilli = _recordIntervalMilli;
            _ = Logging();
        }

        public void StopLogging()
        {
            if (!IsLoggingStarted)
            {
                return;
            }
            IsLoggingStarted = false;
        }

        private async Task Logging()
        {
            while (IsLoggingStarted)
            {
                // Simulate logging every 250ms
                await Task.Delay(recordIntervalMilli);
                // Log the position
                LogPosition();
            }
        }
        private void LogPosition()
        {
            if (OnLogPosition == null)
            {
                return;
            }
            var positionEntries = OnLogPosition.Invoke();
            if (positionEntries == null || positionEntries.Count == 0)
            {
                return;
            }
            foreach (PositionEntry entry in positionEntries)
            {
                buffer[writeIndex] = entry;
                writeIndex = (writeIndex + 1) % buffer.Length;
            }
        }
    }
}