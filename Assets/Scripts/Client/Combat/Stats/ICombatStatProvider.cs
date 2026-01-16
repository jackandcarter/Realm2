namespace Client.Combat
{
    public interface ICombatStatProvider
    {
        bool TryGetStat(string statId, out float value);
        float GetStatOrDefault(string statId, float fallback = 0f);
    }
}
