#nullable enable
using System;
using System.Reflection;

namespace VikingsAscensionAPI
{
    public static class AscensionAPI
    {
        private const string ImplAssemblyName = "VikingsAscension";
        private const string ImplTypeName = "VikingsAscension.AscensionAPI";

        private static bool _resolved;
        private static bool _resolveAttempted;
        private static Func<int, int>? _addExperience;
        private static Func<int, int>? _removeExperience;
        private static Func<int, int>? _addLevel;
        private static Func<int, int>? _removeLevel;
        private static Func<int>? _getLevel;
        private static Func<long>? _getExperience;
        private static Func<bool>? _guildsActive;
        private static Func<int>? _ownGuild;
        private static Func<string, string, int>? _playerGuild;
        private static Func<int, string, string, bool>? _isMember;
        private static Func<int, string>? _guildName;

        public static int AscensionAddExperience(int amount) => Resolve() ? Call(_addExperience, amount) : 0;

        public static int AscensionRemoveExperience(int amount) => Resolve() ? Call(_removeExperience, amount) : 0;

        public static int AscensionAddLevel(int levels) => Resolve() ? Call(_addLevel, levels) : 0;

        public static int AscensionRemoveLevel(int levels) => Resolve() ? Call(_removeLevel, levels) : 0;

        public static int AscensionGetLevel() => Resolve() ? Read(_getLevel) : 0;

        public static long AscensionGetExperience() => Resolve() ? Read(_getExperience) : 0;

        public static bool AscensionIsGuildsActive() => Resolve() && Read(_guildsActive);

        public static int AscensionGetOwnGuildId() => Resolve() ? Read(_ownGuild) : 0;

        public static int AscensionGetPlayerGuildId(string playerId, string playerName)
        {
            if (!Resolve() || _playerGuild == null) return 0;
            try { return _playerGuild(playerId, playerName); }
            catch { return 0; }
        }

        public static bool AscensionIsGuildMember(int guildId, string playerId, string playerName)
        {
            if (!Resolve() || _isMember == null) return false;
            try { return _isMember(guildId, playerId, playerName); }
            catch { return false; }
        }

        public static string AscensionGetGuildName(int guildId)
        {
            if (!Resolve() || _guildName == null) return string.Empty;
            try { return _guildName(guildId); }
            catch { return string.Empty; }
        }

        private static int Call(Func<int, int>? method, int value)
        {
            if (value < 1 || method == null) return 0;
            try { return method(value); }
            catch { return 0; }
        }

        private static T Read<T>(Func<T>? method) where T : struct
        {
            if (method == null) return default;
            try { return method(); }
            catch { return default; }
        }

        private static bool Resolve()
        {
            if (_resolveAttempted) return _resolved;
            _resolveAttempted = true;

            try
            {
                Type? impl = Type.GetType(ImplTypeName + ", " + ImplAssemblyName, false);
                if (impl == null) return false;

                _addExperience = Method(impl, nameof(AscensionAddExperience));
                _removeExperience = Method(impl, nameof(AscensionRemoveExperience));
                _addLevel = Method(impl, nameof(AscensionAddLevel));
                _removeLevel = Method(impl, nameof(AscensionRemoveLevel));
                _getLevel = Getter<int>(impl, nameof(AscensionGetLevel));
                _getExperience = Getter<long>(impl, nameof(AscensionGetExperience));
                _guildsActive = Bind<Func<bool>>(impl, nameof(AscensionIsGuildsActive));
                _ownGuild = Bind<Func<int>>(impl, nameof(AscensionGetOwnGuildId));
                _playerGuild = Bind<Func<string, string, int>>(impl, nameof(AscensionGetPlayerGuildId), typeof(string), typeof(string));
                _isMember = Bind<Func<int, string, string, bool>>(impl, nameof(AscensionIsGuildMember), typeof(int), typeof(string), typeof(string));
                _guildName = Bind<Func<int, string>>(impl, nameof(AscensionGetGuildName), typeof(int));
            }
            catch
            {
                return false;
            }

            _resolved = true;
            return true;
        }

        private static Func<int, int> Method(Type impl, string name)
        {
            MethodInfo method = impl.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null)
                ?? throw new MissingMethodException(ImplTypeName, name);
            return (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), method);
        }

        private static T? Bind<T>(Type impl, string name, params Type[] parameters) where T : Delegate
        {
            MethodInfo? method = impl.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
            return method == null ? null : (T?)Delegate.CreateDelegate(typeof(T), method, false);
        }

        private static Func<T>? Getter<T>(Type impl, string name)
        {
            MethodInfo? method = impl.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            return method != null && method.ReturnType == typeof(T) ? (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), method) : null;
        }
    }
}
