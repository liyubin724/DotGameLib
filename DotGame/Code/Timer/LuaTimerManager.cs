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
        private LuaState lua;

        private int timerIndex = 0;
        private Dictionary<int, LuaTimerData> timerDatas = new Dictionary<int, LuaTimerData>();
        private Dictionary<int, TimerTaskInfo> timerTasks = new Dictionary<int, TimerTaskInfo>();
        protected override void OnInit()
        {
            lua = LuaInstance.Instance.Get();
            timerIndex = 0;
        }

        public int AddTimer(LuaTable timer,float interval,float total,
                                        LuaFunction startFun,LuaFunction intervalFun,
                                        LuaFunction endFun,object userData)
        {
            int infoIndex = timerIndex;

            LuaTimerData timerData = new LuaTimerData();
            timerData.luaTimer = timer;
            timerData.startFunc = startFun;
            timerData.intervalFunc = intervalFun;
            timerData.endFunc = endFun;
            timerData.userData = userData;

            timerDatas.Add(infoIndex, timerData);

            TimerTaskInfo taskInfo = TimerManager.GetInstance().AddTimerTask(interval, total, OnTimerStart, OnTimerInterval, OnTimerEnd, infoIndex);
            timerTasks.Add(infoIndex, taskInfo);
            timerIndex++;

            return infoIndex;
        }

        public void RemoveTimer(int index)
        {
            if(timerTasks.ContainsKey(index))
            {
                TimerTaskInfo taskInfo = timerTasks[index];
                TimerManager.GetInstance().RemoveTimerTask(taskInfo);
                ClearTimer(index);
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
                lua.GetField(-1, "OnTimerEnd");
                lua.PushValue(-2);
                lua.PushInteger(index);
                lua.PCall(2, 0, 0);
                lua.Pop(1);
            }
            ClearTimer(index);
        }

        public void ClearTimer()
        {
            foreach(KeyValuePair<int,TimerTaskInfo> kvp in timerTasks)
            {
                if(kvp.Value!=null)
                {
                    TimerManager.GetInstance().RemoveTimerTask(kvp.Value);
                }
                ClearTimer(kvp.Key);
            }
            timerDatas.Clear();
            timerTasks.Clear();
        }

        private void ClearTimer(int index)
        {
            LuaTimerData timerData = timerDatas[index];
            timerDatas.Remove(index);
            timerTasks.Remove(index);
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
