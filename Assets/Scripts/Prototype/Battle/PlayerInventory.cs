using System.Collections.Generic;

namespace WeaponMazeAlchemy.Prototype
{
    public class PlayerInventory
    {
        private readonly List<WeaponInstance> weapons = new List<WeaponInstance>();
        private int equippedWeaponIndex = -1;

        public PlayerInventory(int maxWeaponCount)
        {
            MaxWeaponCount = maxWeaponCount < 0 ? 0 : maxWeaponCount;
        }

        public int MaxWeaponCount { get; }
        public int Count => weapons.Count;
        public bool IsFull => Count >= MaxWeaponCount;
        public IReadOnlyList<WeaponInstance> Weapons => weapons;
        public int EquippedWeaponIndex => IsValidIndex(equippedWeaponIndex) ? equippedWeaponIndex : -1;
        public WeaponInstance EquippedWeapon => IsValidIndex(equippedWeaponIndex) ? weapons[equippedWeaponIndex] : null;

        public bool TryAdd(WeaponInstance weapon)
        {
            if (weapon == null || IsFull)
            {
                return false;
            }

            weapons.Add(weapon);
            if (equippedWeaponIndex < 0)
            {
                equippedWeaponIndex = weapons.Count - 1;
            }

            return true;
        }

        public bool TryGetWeapon(int index, out WeaponInstance weapon)
        {
            if (index < 0 || index >= weapons.Count)
            {
                weapon = null;
                return false;
            }

            weapon = weapons[index];
            return true;
        }

        public bool TryReplaceWeapon(int index, WeaponInstance weapon, out WeaponInstance replacedWeapon)
        {
            if (weapon == null || index < 0 || index >= weapons.Count)
            {
                replacedWeapon = null;
                return false;
            }

            replacedWeapon = weapons[index];
            weapons[index] = weapon;
            return true;
        }

        public bool TryEquip(int index)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            equippedWeaponIndex = index;
            return true;
        }

        public bool IsEquipped(int index)
        {
            return EquippedWeaponIndex == index;
        }

        public void Clear()
        {
            weapons.Clear();
            equippedWeaponIndex = -1;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < weapons.Count;
        }
    }
}
