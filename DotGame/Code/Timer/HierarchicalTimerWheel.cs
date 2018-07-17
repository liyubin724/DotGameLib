using System.Collections.Generic;


namespace Game.Core.Timer
{
    public class TimerTaskInfo
    {
        internal int index = -1;
        internal int wheelIndex = -1;
        internal int wheelSlotIndex = -1;
        internal int taskListIndex = -1;

        internal TimerTaskInfo()
        {
        }

        internal void OnClear()
        {
            index = -1;
            wheelIndex = -1;
            wheelSlotIndex = -1;
            taskListIndex = -1;
        }

        internal bool IsClear()
        {
            return index == -1;
        }

        public override string ToString()
        {
            return string.Format("Client.Common.Timer::TimerTaskInfo->index = {0},wheelIndex = {1},wheelSlotIndex = {2},taskListIndex = {3}", index, wheelIndex, wheelSlotIndex, taskListIndex);
        }
    }

    internal sealed class HierarchicalTimerWheel
    {
        private TimerWheel[] wheelArr = new TimerWheel[4];
        private int taskIndex = 0;
        private Dictionary<int, TimerTaskInfo> taskInfoDic = new Dictionary<int, TimerTaskInfo>();
        private List<TimerTask> idleTimerTaskList = new List<TimerTask>();

        private float lapseTime = 0; //seconds
        internal HierarchicalTimerWheel()
        {
            wheelArr[0] = new TimerWheel(0, 50, 20);
            wheelArr[1] = new TimerWheel(1, 1000, 60);
            wheelArr[2] = new TimerWheel(2, 60000, 60);
            wheelArr[3] = new TimerWheel(3, 3600000, 24);
            for (int i = 0; i < wheelArr.Length; i++)
            {
                wheelArr[i].wheelTriggerEvent = OnTimerWheelTrigger;
                wheelArr[i].wheelOutEvent = OnTimerWheelOut;
            }
        }

        internal void OnUpdate(float deltaTime)
        {
            lapseTime += deltaTime;
            int lTime = (int)(lapseTime * 1000);
            int turnNum = lTime / wheelArr[0].TickInMS;
            if (wheelArr[0] != null && taskInfoDic.Count > 0)
            {
                if (turnNum > 0)
                {
                    wheelArr[0].DoTimerTurn(turnNum);
                }
            }
            lapseTime -= turnNum * wheelArr[0].TickInMS * 0.001f;
        }

        internal TimerTaskInfo AddTimerTask(TimerTask task)
        {
            TimerTaskInfo taskInfo = new TimerTaskInfo();
            if (AddTimerTask(task, taskInfo))
            {
                task.OnTaskStart();
                return taskInfo;
            }
            else
            {
                return null;
            }
        }

        private bool AddTimerTask(TimerTask task, TimerTaskInfo taskInfo)
        {
            if (!task.IsValidTask())
            {
                return false;
            }

            for (int i = 0; i < wheelArr.Length; i++)
            {
                if (wheelArr[i].AddTimerTask(task, ref taskInfo.wheelSlotIndex, ref taskInfo.taskListIndex))
                {
                    taskInfo.wheelIndex = i;
                    break;
                }
            }
            if (taskInfo.wheelIndex < 0 || taskInfo.wheelSlotIndex < 0 || taskInfo.taskListIndex < 0)
            {
                return false;
            }
            taskIndex++;
            task.index = taskIndex;
            taskInfo.index = taskIndex;
            taskInfoDic.Add(taskInfo.index, taskInfo);
            return true;
        }

        internal bool RemoveTimerTask(TimerTaskInfo taskInfo)
        {
            if (taskInfo == null || taskInfo.wheelIndex < 0 || taskInfo.wheelSlotIndex < 0 || taskInfo.taskListIndex < 0)
            {
                return false;
            }
            if (!taskInfoDic.ContainsKey(taskInfo.index))
            {
                return false;
            }

            if (taskInfo.wheelIndex < 0 || taskInfo.wheelIndex >= wheelArr.Length || wheelArr[taskInfo.wheelIndex] == null)
            {
                return false;
            }

            taskInfoDic.Remove(taskInfo.index);

            int wheelIndex = taskInfo.wheelIndex;
            int wheelSlotIndex = taskInfo.wheelSlotIndex;
            int taskListIndex = taskInfo.taskListIndex;
            taskInfo.OnClear();

            return wheelArr[wheelIndex].RemoveTimerTask(wheelSlotIndex, taskListIndex);
        }

        private void OnTimerWheelOut(int index)
        {
            if (index >= 0 && index < wheelArr.Length - 1)
            {
                wheelArr[index + 1].DoTimerTurn(1);
            }
        }

        private void OnTimerWheelTrigger(int index, List<TimerTask> taskList)
        {
            for (int i = 0; i < taskList.Count; i++)
            {
                TimerTask task = taskList[i];
                if (task == null)
                {
                    continue;
                }
                TimerTaskInfo taskInfo = null;
                if (!taskInfoDic.TryGetValue(task.index, out taskInfo))
                {
                    continue;
                }
                if (task.remainingWheelInMS == 0)
                {
                    task.OnTrigger();
                }

                if (taskInfo.IsClear())
                {
                    RecycleTimerTask(task);
                }
                else
                {
                    taskInfoDic.Remove(task.index);
                    taskInfo.OnClear();
                    if (task.IsValidTask())
                    {
                        AddTimerTask(task, taskInfo);
                    }
                    else
                    {
                        RecycleTimerTask(task);
                    }
                }
            }
        }

        internal TimerTask GetIdleTimerTask()
        {
            TimerTask task = null;
            if (idleTimerTaskList.Count == 0)
            {
                task = new TimerTask();
            }
            else
            {
                task = idleTimerTaskList[0];
                idleTimerTaskList.RemoveAt(0);
            }
            return task;
        }

        private void RecycleTimerTask(TimerTask task)
        {
            if (task != null)
            {
                task.OnClear();
                idleTimerTaskList.Add(task);
            }
        }

        internal void Clear()
        {
            if (taskInfoDic.Count > 0)
            {
                List<int> keys = new List<int>(taskInfoDic.Keys);
                for (int i = 0, imax = keys.Count; i < imax; ++i)
                {
                    RemoveTimerTask(taskInfoDic[keys[i]]);
                }
            }    
        }
    }
}


