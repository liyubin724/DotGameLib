using Game.Core.DotLua;
using Game.Core.Util;
using System.Collections.Generic;

namespace Game.Core.Timer
{
    internal class LuaTimerData
    {
        public LuaTable luaTimer;
        public LuaFunction startFunc;
        public LuaFunction intervalFunc;
        public LuaFunction endFunc;

        public object userData;
    }

    public class LuaTimerManager : Singleton<LuaTimerManager>
    {
        private List<LuaTimerData> timerDatas = new List<LuaTimerData>();
        private List<TimerTaskInfo> timerTasks = new List<TimerTaskInfo>();
        private LuaState lua;

        protected override void OnInit()
        {
            lua = LuaInstance.Instance.Get();
        }

        public TimerTaskInfo AddTimer(LuaTable timer,float interval,float total,
                                        LuaFunction startFun,LuaFunction intervalFun,
                                        LuaFunction endFun,object userData)
        {
            LuaTimerData timerData = new LuaTimerData();
            timerData.luaTimer = timer;
            timerData.startFunc = startFun;
            timerData.intervalFunc = intervalFun;
            timerData.endFunc = endFun;
            timerData.userData = userData;

            timerDatas.Add(timerData);

            TimerTaskInfo taskInfo = TimerManager.GetInstance().AddTimerTask(interval, total, OnTimerStart, OnTimerInterval, OnTimerEnd, timerDatas.Count - 1);
            timerTasks.Add(taskInfo);
            return taskInfo;
        }

        public void RemoveTimer(TimerTaskInfo taskInfo)
        {
            if (taskInfo != null)
            {
                for (int i = 0; i < timerTasks.Count; i++)
                {
                    if (taskInfo == timerTasks[i])
                    {
                        TimerManager.GetInstance().RemoveTimerTask(taskInfo);
                        ClearTimer(i);
                        return;
                    }
                }
            }
        }

        private void OnTimerStart(object obj)
        {
            int index = (int)obj;
            LuaTimerData timerData = timerDatas[index];
            if (timerData.startFunc != null && timerData.startFunc.IsValid())
            {
                timerData.startFunc.Invoke(timerData.userData);
            }
        }

        private void OnTimerInterval(object obj)
        {
            int index = (int)obj;
            LuaTimerData timerData = timerDatas[index];
            if (timerData.intervalFunc != null && timerData.intervalFunc.IsValid())
            {
                timerData.intervalFunc.Invoke(timerData.userData);
            }
        }

        private void OnTimerEnd(object obj)
        {
            int index = (int)obj;
            LuaTimerData timerData = timerDatas[index];
            if (timerData.endFunc != null && timerData.endFunc.IsValid())
            {
                timerData.endFunc.Invoke(timerData.userData);
            }
            if(timerData.luaTimer!=null && timerData.luaTimer.IsValid())
            {
                lua.RawGetI(LuaAPI.LUA_REGISTRYINDEX, timerData.luaTimer.TableRef());
                lua.GetField(-1, "RemoveTimer");
                lua.PushValue(-2);
                lua.PushSystemObject(timerTasks[index],typeof(TimerTaskInfo));
                lua.PCall(2, 0, 0);
                lua.Pop(1);
            }
        }

        public void ClearTimer()
        {
            for (int i = 0; i < timerDatas.Count; i++)
            {
                TimerTaskInfo taskInfo = timerTasks[i];
                if (taskInfo != null)
                {
                    TimerManager.GetInstance().RemoveTimerTask(taskInfo);
                }
                ClearTimer(i);
            }
            timerDatas.Clear();
            timerTasks.Clear();
        }

        private void ClearTimer(int index)
        {
            LuaTimerData timerData = timerDatas[index];
            timerDatas.RemoveAt(index);
            timerTasks.RemoveAt(index);
            if (timerData != null)
            {
                if(timerData.luaTimer!=null)
                {
                    timerData.luaTimer.Dispose();
                }
                if (timerData.startFunc != null)
                {
                    timerData.startFunc.Dispose();
                }
                if (timerData.intervalFunc != null)
                {
                    timerData.intervalFunc.Dispose();
                }
                if (timerData.endFunc != null)
                {
                    timerData.endFunc.Dispose();
                }
                timerData.luaTimer = null;
                timerData.startFunc = null;
                timerData.intervalFunc = null;
                timerData.endFunc = null;
                timerData.userData = null;
            }
        }
    }
}
