namespace Game.Core.Timer
{
    internal class TimerTask
    {
        internal int index = -1;
        internal int intervalInMS = 0;
        private int totalInMS = 0;
        private VoidDelegateWithObj onStartEvent = null;
        private VoidDelegateWithObj onIntervalEvent = null;
        private VoidDelegateWithObj onEndEvent = null;
        private object userData = null;

        internal int remainingWheelInMS = 0;
        private int leftInMS = 0;

        internal TimerTask()
        {

        }

        internal void OnReused(float intervalInSec,
                                                float totalInSec,
                                                VoidDelegateWithObj startCallback,
                                                VoidDelegateWithObj intervalCallback,
                                                VoidDelegateWithObj endCallback,
                                                object callbackData)
        {
            this.intervalInMS = UnityEngine.Mathf.CeilToInt(intervalInSec * 1000);
            if (totalInSec <= 0)
            {
                totalInMS = 0;
            }
            else
            {
                this.totalInMS = UnityEngine.Mathf.CeilToInt(totalInSec * 1000);
            }
            onStartEvent = startCallback;
            onIntervalEvent = intervalCallback;
            onEndEvent = endCallback;
            userData = callbackData;

            remainingWheelInMS = intervalInMS;
            leftInMS = totalInMS;
        }

        internal bool IsValidTask()
        {
            if (intervalInMS <= 0)
            {
                return false;
            }
            if (totalInMS == 0)
            {
                return true;
            }
            else if (totalInMS > 0)
            {
                return leftInMS > 0;
            }
            return false;
        }

        internal void OnTaskStart()
        {
            if (onStartEvent != null)
            {
                onStartEvent(userData);
            }
        }

        internal void OnTrigger()
        {
            if (totalInMS > 0)
            {
                leftInMS -= intervalInMS;
            }

            if (onIntervalEvent != null)
            {
                onIntervalEvent(userData);
            }

            if (totalInMS == 0 || leftInMS > 0)
            {
                if (totalInMS == 0 || leftInMS >= intervalInMS)
                {
                    remainingWheelInMS = intervalInMS;
                }
                else
                {
                    remainingWheelInMS = leftInMS;
                }
            }
            else
            {
                if (onEndEvent != null)
                {
                    onEndEvent(userData);
                }
            }
        }

        internal void OnClear()
        {
            intervalInMS = 0; ;
            totalInMS = 0;
            remainingWheelInMS = 0;
            leftInMS = 0;
            onStartEvent = null;
            onIntervalEvent = null;
            onEndEvent = null;
            userData = null;
        }
    }
}
