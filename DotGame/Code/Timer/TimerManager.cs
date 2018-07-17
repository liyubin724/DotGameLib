using Game.Core.Util;

namespace Game.Core.Timer
{
    public delegate void VoidDelegateWithObj(object obj);
   
    public class TimerManager : Singleton <TimerManager>
    {
        private HierarchicalTimerWheel hTimerWheel = null;
        private bool isPause = false;

        protected override void OnInit()
        {
            hTimerWheel = new HierarchicalTimerWheel();
        }

        public void Clear()
        {
            if (hTimerWheel != null)
            {
                hTimerWheel.Clear();
                hTimerWheel = null;
            }
        }

        public void PauseTimer()
        {
            isPause = true;
        }

        public void ResumeTimer()
        {
            isPause = false;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!isPause && hTimerWheel != null)
            {
                hTimerWheel.OnUpdate(deltaTime);
            }
        }

        public TimerTaskInfo AddTimerTask(float intervalInSec,
                                                float totalInSec,
                                                VoidDelegateWithObj startCallback,
                                                VoidDelegateWithObj intervalCallback,
                                                VoidDelegateWithObj endCallback,
                                                object callbackData)
        {
            TimerTask task = hTimerWheel.GetIdleTimerTask();
            task.OnReused(intervalInSec, totalInSec, startCallback, intervalCallback, endCallback, callbackData);
            return hTimerWheel.AddTimerTask(task);
        }

        public bool RemoveTimerTask(TimerTaskInfo taskInfo)
        {
            return hTimerWheel.RemoveTimerTask(taskInfo);
        }
    }
}
