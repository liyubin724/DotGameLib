using Game.Core.Pool;
using Game.Core.Timer;
using Game.Core.Util;
using System.Collections.Generic;

namespace Game.Core.Event
{
    public delegate void GameEventHandler(GameEvent e);

    public sealed class GameEventManager : Singleton<GameEventManager>
    {
        private Dictionary<GameEvent, TimerTaskInfo> delayEventTaskInfo = null;
        private Dictionary<int, List<GameEventHandler>> eventHandlerDic = null;

        private SimpleObjectPool<GameEvent> eventPool = null;

        public GameEventManager()
        {
        }

        protected override void OnInit()
        {
            eventPool = new SimpleObjectPool<GameEvent>(5);

            delayEventTaskInfo = new Dictionary<GameEvent, TimerTaskInfo>();
            eventHandlerDic = new Dictionary<int, List<GameEventHandler>>();
        }

        public void Dispose()
        {
            if(eventPool!=null)
            {
                eventPool.Dispose();
            }
            eventPool = null;
            delayEventTaskInfo = null;
            eventHandlerDic = null;

            instance = null;
        }

        /// <summary>
        /// 注册侦听事件
        /// </summary>
        /// <param name="eventID"></param>
        /// <param name="handler"></param>
        public void RegisterEvent(int eventID, GameEventHandler handler)
        {
            List<GameEventHandler> handlerList = null;
            if (!eventHandlerDic.TryGetValue(eventID, out handlerList))
            {
                handlerList = new List<GameEventHandler>();
                eventHandlerDic.Add(eventID, handlerList);
            }

            handlerList.Add(handler);
        }

        /// <summary>
        /// 取消事件侦听
        /// </summary>
        /// <param name="eventID"></param>
        /// <param name="handler"></param>
        public void UnregisterEvent(int eventID, GameEventHandler handler)
        {
            List<GameEventHandler> handlerList = null;
            if (eventHandlerDic.TryGetValue(eventID, out handlerList))
            {
                if (handlerList != null)
                {
                    for (int i = handlerList.Count - 1; i >= 0; i--)
                    {
                        if (handlerList[i] == null || handlerList[i] == handler)
                        {
                            handlerList.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void TriggerEvent(int eventID)
        {
            TriggerEvent(eventID, 0.0f, null);
        }

        public void TriggerEvent(int eventID, float delayTime)
        {
            TriggerEvent(eventID, delayTime, null);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="eventID">事件ID号</param>
        /// <param name="delayTime">事件触发延迟时长，为0表示立即触发</param>
        /// <param name="datas">事件携带参数</param>
        public void TriggerEvent(int eventID, float delayTime, params object[] datas)
        {
            GameEvent e = eventPool.GetItem();
            e.SetEvent(eventID, delayTime, datas);

            if (e.EventDelayTime <= 0)
            {
                TriggerEvent(e);
            }
            else
            {
                //使用时间轮来管理事件触发 
                TimerTaskInfo taskInfo = TimerManager.GetInstance().AddTimerTask(delayTime, delayTime, null, null, OnDelayEventTrigger, e);
                delayEventTaskInfo.Add(e, taskInfo);
            }
        }

        private void OnDelayEventTrigger(object userdata)
        {
            if (userdata == null)
                return;
            GameEvent gEvent = userdata as GameEvent;
            if(gEvent !=null)
            {
                delayEventTaskInfo.Remove(gEvent);
                TriggerEvent(gEvent);
            }
        }

        private void TriggerEvent(GameEvent e)
        {
            List<GameEventHandler> handlerList = null;
            if (eventHandlerDic.TryGetValue(e.EventID, out handlerList))
            {
                if (handlerList != null && handlerList.Count > 0)
                {
                    for (int i = 0; i < handlerList.Count; i++)
                    {
                        if (handlerList[i] != null)
                        {
                            handlerList[i](e);

                            eventPool.PutItem(e);
                        }
                    }
                }
            }
        }
    }
}

