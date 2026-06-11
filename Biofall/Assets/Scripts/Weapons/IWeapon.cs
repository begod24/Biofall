namespace Biofall.Weapons
{
    /// <summary>Common contract for everything that can be equipped and fired.</summary>
    public interface IWeapon
    {
        WeaponData Data { get; }
        int MagazineAmmo { get; }
        int ReserveAmmo { get; }
        bool IsReloading { get; }

        /// <summary>Attempt to fire. Returns true if a shot actually went out.</summary>
        bool TryFire();
        void Reload();
    }
}
