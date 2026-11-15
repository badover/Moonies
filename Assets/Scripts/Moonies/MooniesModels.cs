using System;
using UnityEngine;

namespace Moonies
{
    [Serializable]
    public class MooniesDatabase
    {
        public MooniesMission[] missions;
    }

    [Serializable]
    public class MooniesMission
    {
        public int phase;
        public string phaseName;
        public string title;
        public string tag;
        public string scenario;
        public string question;
        public string hint;
        public string learn;
        public string fact;
        public MooniesChoice[] choices;
    }

    [Serializable]
    public class MooniesChoice
    {
        public string text;
        public bool isCorrect;
        public string why;
        public ResourceDelta gain;
        public ResourceDelta damage;
    }

    [Serializable]
    public class ResourceDelta
    {
        public int oxygen;
        public int fuel;
        public int power;
        public int hull;
        public int team;
        public int science;

        public int Get(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Oxygen: return oxygen;
                case ResourceType.Fuel: return fuel;
                case ResourceType.Power: return power;
                case ResourceType.Hull: return hull;
                case ResourceType.Team: return team;
                case ResourceType.Science: return science;
                default: return 0;
            }
        }
    }

    public enum ResourceType
    {
        Oxygen,
        Fuel,
        Power,
        Hull,
        Team,
        Science
    }

    [Serializable]
    public class MissionResources
    {
        [Range(0, 100)] public int oxygen = 100;
        [Range(0, 100)] public int fuel = 100;
        [Range(0, 100)] public int power = 100;
        [Range(0, 100)] public int hull = 100;
        [Range(0, 100)] public int team = 100;
        [Range(0, 100)] public int science = 100;

        public int Get(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Oxygen: return oxygen;
                case ResourceType.Fuel: return fuel;
                case ResourceType.Power: return power;
                case ResourceType.Hull: return hull;
                case ResourceType.Team: return team;
                case ResourceType.Science: return science;
                default: return 0;
            }
        }

        public void Add(ResourceDelta delta, int sign)
        {
            if (delta == null) return;
            oxygen = Clamp(oxygen + delta.oxygen * sign);
            fuel = Clamp(fuel + delta.fuel * sign);
            power = Clamp(power + delta.power * sign);
            hull = Clamp(hull + delta.hull * sign);
            team = Clamp(team + delta.team * sign);
            science = Clamp(science + delta.science * sign);
        }

        public int Average()
        {
            return (oxygen + fuel + power + hull + team + science) / 6;
        }

        public ResourceType Lowest()
        {
            ResourceType lowest = ResourceType.Oxygen;
            int min = oxygen;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int value = Get(type);
                if (value < min)
                {
                    min = value;
                    lowest = type;
                }
            }
            return lowest;
        }

        public void Recover(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Oxygen: oxygen = Clamp(oxygen + amount); break;
                case ResourceType.Fuel: fuel = Clamp(fuel + amount); break;
                case ResourceType.Power: power = Clamp(power + amount); break;
                case ResourceType.Hull: hull = Clamp(hull + amount); break;
                case ResourceType.Team: team = Clamp(team + amount); break;
                case ResourceType.Science: science = Clamp(science + amount); break;
            }
        }

        private static int Clamp(int value) => Mathf.Clamp(value, 0, 100);
    }

    [Serializable]
    public class MissionAttempt
    {
        public int actNumber;
        public bool correct;
        public int selectedOption;
    }

    public static class MooniesText
    {
        public static string ResourceName(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Oxygen: return "Oxygen";
                case ResourceType.Fuel: return "Fuel";
                case ResourceType.Power: return "Power";
                case ResourceType.Hull: return "Hull";
                case ResourceType.Team: return "Crew";
                case ResourceType.Science: return "Science";
                default: return type.ToString();
            }
        }

        public static string PhaseEmoji(int phase)
        {
            switch (phase)
            {
                case 0: return "🛰";
                case 1: return "🚀";
                case 2: return "🌌";
                case 3: return "🌕";
                case 4: return "🛬";
                case 5: return "🔬";
                case 6: return "🌘";
                case 7: return "🌍";
                default: return "🌙";
            }
        }
    }
}
