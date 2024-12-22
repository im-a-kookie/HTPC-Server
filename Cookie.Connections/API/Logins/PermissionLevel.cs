using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cookie.Connections.API.Logins
{
    public enum Level
    {
        LOW, MED, HIGH
    }


    public struct PermissionLevel
    {

        public Level ReadLevel;
        public Level WriteLevel;

        public PermissionLevel(int value)
        {
            ReadLevel = (Level)(value & 0xF);
            WriteLevel = (Level)(value >> 4 & 0xF);
        }

        public PermissionLevel() : this(Level.LOW, Level.LOW) { }
        public PermissionLevel(Level level) : this(level, level) { }
        public PermissionLevel(Level read, Level write)
        {
            ReadLevel = read;
            WriteLevel = write;
        }

        public (bool read, bool write) Validate(PermissionLevel level)
        {
            return (ValidateRead(level), ValidateWrite(level));
        }

        public bool ValidateRead(PermissionLevel input)
        {
            return input.ReadLevel <= ReadLevel;
        }

        public bool ValidateWrite(PermissionLevel input)
        {
            return input.WriteLevel <= WriteLevel;
        }

        public int ToInt()
        {
            return (int)ReadLevel | (int)WriteLevel << 4;
        }

    }


}
