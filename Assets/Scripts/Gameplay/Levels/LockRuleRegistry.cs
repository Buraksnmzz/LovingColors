using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameplay.Levels
{
    public static class LockRuleRegistry
    {
        private static readonly Dictionary<string, LockRule> RulesByName = typeof(LockRule)
            .Assembly
            .GetTypes()
            .Where(type => typeof(LockRule).IsAssignableFrom(type) && !type.IsAbstract)
            .ToDictionary(type => type.Name, type => (LockRule)Activator.CreateInstance(type));

        public static bool TryGet(string ruleName, out LockRule lockRule)
        {
            return RulesByName.TryGetValue(ruleName, out lockRule);
        }
    }
}