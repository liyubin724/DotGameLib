
namespace Game.Core.Util
{
    public abstract class Singleton <T> where T :Singleton<T>, new()
    {
        protected static T instance = null;
        public static T GetInstance()
        {
            if(instance == null)
            {
                instance = new T();
                instance.OnInit();
            }
            return instance;
        }

        protected virtual void OnInit()
        {
        }
    }
}

