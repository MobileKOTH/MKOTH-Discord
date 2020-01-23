using System;

namespace MKOTHDiscordBot.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class SingletonServiceAttribute : Attribute
    {
        public string Description { get; private set; }
        public bool ForceInstantiate { get; set; } = false;

        public SingletonServiceAttribute(string description)
        {
            Description = description;
        }
    }
}
