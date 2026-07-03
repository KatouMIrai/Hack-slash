namespace WeaponMazeAlchemy.Prototype
{
    public readonly struct DamageResult
    {
        public DamageResult(int amount, bool isCritical, bool isEvaded)
        {
            Amount = amount;
            IsCritical = isCritical;
            IsEvaded = isEvaded;
        }

        public int Amount { get; }
        public bool IsCritical { get; }
        public bool IsEvaded { get; }
    }
}
