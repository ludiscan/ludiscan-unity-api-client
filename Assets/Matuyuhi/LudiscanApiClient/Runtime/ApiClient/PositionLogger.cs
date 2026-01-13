using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LudiscanApiClient.Runtime.ApiClient.Model;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient
{
    /// <summary>
    /// プレイヤーの位置情報を定期的に記録するロガークラス
    /// シングルトンパターンで実装されており、Initialize()で初期化後、Instanceプロパティでアクセスします
    /// リングバッファを使用して位置情報を蓄積し、定期的にアップロードすることを想定しています
    /// </summary>
    public class PositionLogger
    {
        private static PositionLogger _instance;

        private int writeIndex;
        private PositionEntry[] buffer;
        private int recordIntervalMilli;

        /// <summary>
        /// PositionLoggerのシングルトンインスタンスを取得します
        /// Initialize()で初期化されていない場合は例外をスローします
        /// </summary>
        /// <exception cref="InvalidOperationException">ロガーが初期化されていない場合</exception>
        public static PositionLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException(
                        "PositionLogger is not initialized. Call Initialize(int bufferSize) first."
                    );
                }
                return _instance;
            }
        }

        /// <summary>
        /// PositionLoggerが初期化済みかどうかを確認します
        /// </summary>
        /// <returns>初期化済みの場合はtrue、未初期化の場合はfalse</returns>
        public static bool IsInitialized => _instance != null;

        /// <summary>
        /// PositionLoggerを初期化します
        /// 既に初期化されている場合は警告を出力して再初期化します
        /// </summary>
        /// <param name="bufferSize">リングバッファのサイズ</param>
        public static void Initialize(int bufferSize)
        {
            if (_instance != null)
            {
                UnityEngine.Debug.LogWarning("PositionLogger is already initialized. Reinitializing...");
            }
            _instance = new PositionLogger(bufferSize);
        }

        /// <summary>
        /// 位置情報のバッファを取得します
        /// </summary>
        public PositionEntry[] Buffer => buffer;

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
        /// プライベートコンストラクタ
        /// </summary>
        /// <param name="bufferSize">リングバッファのサイズ</param>
        private PositionLogger(int bufferSize)
        {
            buffer = new PositionEntry[bufferSize];
            writeIndex = 0;
            IsLoggingStarted = false;
        }

        /// <summary>
        /// 位置情報のロギングを開始します
        /// </summary>
        /// <param name="recordIntervalMilli">記録間隔（ミリ秒）</param>
        public void StartLogging(int recordIntervalMilli)
        {
            if (IsLoggingStarted)
            {
                return;
            }
            IsLoggingStarted = true;
            this.recordIntervalMilli = recordIntervalMilli;
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

        /// <summary>
        /// バッファをクリアして書き込み位置をリセットします
        /// アップロード後に呼び出すことで、次回のゲームで古いデータが混入することを防ぎます
        /// </summary>
        public void ClearBuffer()
        {
            Array.Clear(buffer, 0, buffer.Length);
            writeIndex = 0;
        }

        /// <summary>
        /// 現在の有効なバッファデータを取得し、バッファをクリアします
        /// FieldObjectLoggerやGeneralEventLoggerのGetLogsAndClear()と同じパターン
        /// </summary>
        /// <returns>有効な位置情報エントリの配列</returns>
        public PositionEntry[] GetBufferAndClear()
        {
            // 有効なエントリのみを抽出（nullでないもの）
            var validEntries = new List<PositionEntry>();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Position != Vector3.zero && buffer[i].PlayerId >= 0 && buffer[i].OffsetTimeStamp > 0)
                {
                    validEntries.Add(buffer[i]);
                }
            }
            var result = validEntries.ToArray();
            ClearBuffer();
            return result;
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