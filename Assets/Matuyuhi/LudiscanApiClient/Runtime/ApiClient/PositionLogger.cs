using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient.Model;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// プレイヤーの位置情報を定期的に記録するロガークラス
    /// リングバッファを使用して位置情報を蓄積し、定期的にアップロードすることを想定しています
    /// </summary>
    public class PositionLogger
    {
        private int writeIndex;
        PositionEntry[] buffer;

        /// <summary>
        /// 位置情報のバッファを取得します
        /// </summary>
        public PositionEntry[] Buffer => buffer;
        private int recordIntervalMilli;

        /// <summary>
        /// ロギングが開始されているかどうかを取得します
        /// </summary>
        public bool IsLoggingStarted { get; private set; }

        /// <summary>
        /// 位置情報を取得するためのコールバック関数
        /// このコールバックは定期的に呼び出され、現在のプレイヤー位置を返す必要があります
        /// </summary>
        public Func<List<PositionEntry>> OnLogPosition { get; set; }

        /// <summary>
        /// PositionLoggerを初期化します
        /// </summary>
        /// <param name="_bufferSize">リングバッファのサイズ</param>
        public PositionLogger(int _bufferSize)
        {
            buffer = new PositionEntry[_bufferSize];
            writeIndex = 0;
            IsLoggingStarted = false;
        }

        /// <summary>
        /// 位置情報のロギングを開始します
        /// </summary>
        /// <param name="_recordIntervalMilli">記録間隔（ミリ秒）</param>
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

        /// <summary>
        /// 位置情報のロギングを停止します
        /// </summary>
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