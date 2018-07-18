
namespace Game.Core.Event
{
    public class GameEvent
    {
        private int eventID = -1;
        public int EventID
        {
            get { return eventID; }
        }
        private float eventDelayTime = 0.0f;

        public float EventDelayTime
        {
            get { return eventDelayTime; }
            set { eventDelayTime = value; }
        }

        private System.Object[] eventParams = null;
        public System.Object[] EventParams
        {
            get { return eventParams; }
        }

        public GameEvent() { }

        internal void SetEvent(int eID, float eDelayTime, params object[] objs)
        {
            eventID = eID;
            eventDelayTime = eDelayTime;
            eventParams = objs;
        }

        internal void OnReset()
        {
            eventID = -1;
            eventDelayTime = 0.0f;
            eventParams = null;
        }

        public T GetEventParam<T>(int index = 0)
        {
            object result = GetEventParam(index);
            if (result == null)
                return default(T);
            else
                return (T)result;
        }

        public object GetEventParam(int index = 0)
        {
            if (eventParams == null || eventParams.Length == 0)
            {
                return null;
            }
            if (index < 0 || index >= eventParams.Length)
            {
                return null;
            }

            return eventParams[index];
        }

    }
}

