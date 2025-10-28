using System;

namespace Software.Contraband.StateMachines
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class DefaultStateAttribute : Attribute { }
}