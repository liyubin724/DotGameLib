
namespace CSToLua
{
    interface ICSToLuaRegister
    {
        string RegisterActionToLua(int indent);
        string RegisterFunctionToLua(int indent);
    }
}
