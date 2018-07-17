using System.Collections.Generic;

namespace Game.Core.Timer
{
    internal delegate void OnTimerWheelTrigger(int index, List<TimerTask> taskList);
    internal delegate void OnTimerWheelOut(int index);

    internal sealed class TimerWheel
    {
        private int index = 0;
        private int tickInMS = 0;
        private int slotSize = 0;

        private int currentSlotIndex = 0;
        private List<TimerTask>[] slotArr = null;
        private List<TimerTask> willTriggerTaskList = new List<TimerTask>();
        internal OnTimerWheelTrigger wheelTriggerEvent = null;
        internal OnTimerWheelOut wheelOutEvent = null;

        internal int TickInMS
        {
            get { return tickInMS; }
        }

        internal TimerWheel(int index, int tickInMS, int slotSize)
        {
            this.index = index;
            this.tickInMS = tickInMS;
            this.slotSize = slotSize;

            slotArr = new List<TimerTask>[slotSize];
        }

        internal bool AddTimerTask(TimerTask task, ref int slotIndex, ref int taskListIndex)
        {
            if (task.remainingWheelInMS >= slotSize * tickInMS)
            {
                slotIndex = -1;
                taskListIndex = -1;
                return false;
            }

            int targetSlot = task.remainingWheelInMS / tickInMS;
            if (targetSlot == 0)
            {
                targetSlot = 1;
                task.remainingWheelInMS = 0;
            }
            else
            {
                task.remainingWheelInMS = task.remainingWheelInMS % tickInMS;
            }

            slotIndex = currentSlotIndex + targetSlot;
            slotIndex = slotIndex % slotSize;
            if (slotArr[slotIndex] == null)
            {
                slotArr[slotIndex] = new List<TimerTask>();
            }
            taskListIndex = slotArr[slotIndex].Count;
            slotArr[slotIndex].Add(task);

            return true;
        }

        internal bool RemoveTimerTask(int slotIndex, int taskListIndex)
        {
            List<TimerTask> taskList = slotArr[slotIndex];
            if (taskList != null && taskListIndex >= 0 && taskListIndex < taskList.Count)
            {
                taskList.RemoveAt(taskListIndex);
                return true;
            }
            return false;
        }

        internal void DoTimerTurn(int turnNum)
        {
            for (int i = 0; i < turnNum; i++)
            {
                currentSlotIndex++;
                if (currentSlotIndex == slotSize)
                {
                    currentSlotIndex = 0;
                    if (wheelOutEvent != null)
                    {
                        wheelOutEvent(index);
                    }
                }
                if (slotArr[currentSlotIndex] != null)
                {
                    willTriggerTaskList.AddRange(slotArr[currentSlotIndex]);
                    slotArr[currentSlotIndex].Clear();
                }
            }

            if (willTriggerTaskList.Count > 0)
            {
                if (wheelTriggerEvent != null)
                {
                    wheelTriggerEvent(index, willTriggerTaskList);
                }
                willTriggerTaskList.Clear();
            }
        }
    }
}