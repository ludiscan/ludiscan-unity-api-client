using Matuyuhi.LudiscanApi.Client.Model;
using UnityEngine;

namespace LudiscanApiClient.Runtime.ApiClient.Model
{
    public struct FieldObjectEntity
    {
        public string ObjectId;

        public string ObjectType;

        private Vector3 position;

        public float X
        {
            get => position.x;
            set => position.x = value;
        }
        public float Y
        {
            get => position.y;
            set => position.y = value;
        }
        public float Z
        {
            get => position.z;
            set => position.z = value;
        }
        public Vector3 Position
        {
            get => position;
            set => position = value;
        }

        public ulong OffsetTimeStamp;

        public object Status;

        public FieldObjectLogDto.EventTypeEnum EventType;
    }
}