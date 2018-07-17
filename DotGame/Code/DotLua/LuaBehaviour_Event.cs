using UnityEngine;

namespace Game.Core.DotLua
{
    public partial class LuaBehaviour : MonoBehaviour
    {
        #region Event Trigger.
        public void OnClicked(GameObject go)
        {
            string funcName = go == this.gameObject ? "OnClick" : "OnClick" + go.name;
            if (IsOnClickPassGameObject)
                CallFunction(funcName, go);
            else
                CallFunction(funcName);
        }

        public void OnValueChanged(GameObject go)
        {
            string funcName = "OnValueChanged" + go.name;
            CallFunction(funcName);
        }

        public void OnFinished(GameObject go)
        {
            string funcName = "OnFinished" + go.name;
            CallFunction(funcName);
        }

        public void OnPressed(GameObject go)
        {
            string funcName = "OnPressed" + go.name;
            CallFunction(funcName);
        }

        public void OnReleased(GameObject go)
        {
            string funcName = "OnReleased" + go.name;
            CallFunction(funcName);
        }

        public void OnHovered(GameObject go)
        {
            string funcName = "OnHovered" + go.name;
            CallFunction(funcName);
        }

        public void OnHoverOuted(GameObject go)
        {
            string funcName = "OnHoverOuted" + go.name;
            CallFunction(funcName);
        }

        public void OnSelected(GameObject go)
        {
            string funcName = "OnSelected" + go.name;
            CallFunction(funcName);
        }

        public void OnDeselected(GameObject go)
        {
            string funcName = "OnDeselected" + go.name;
            CallFunction(funcName);
        }

        public void OnDoubleClicked(GameObject go)
        {
            string funcName = "OnDoubleClicked" + go.name;
            CallFunction(funcName);
        }

        public void OnDragStarted(GameObject go)
        {
            string funcName = "OnDragStarted" + go.name;
            CallFunction(funcName);
        }

        public void OnDragEnded(GameObject go)
        {
            string funcName = "OnDragEnded" + go.name;
            CallFunction(funcName);
        }

        public void OnDragOvered(GameObject go)
        {
            string funcName = "OnDragOvered" + go.name;
            CallFunction(funcName);
        }

        public void OnDragOuted(GameObject go)
        {
            string funcName = "OnDragOuted" + go.name;
            CallFunction(funcName);
        }

        public void OnDragged(GameObject go)
        {
            string funcName = "OnDragged" + go.name;
            CallFunction(funcName);
        }

        public void OnLongPressed(GameObject go)
        {
            string funcName = go == this.gameObject ? "OnLongPressed" : "OnLongPressed" + go.name;
            CallFunction(funcName);
        }
        #endregion 
    }
}
