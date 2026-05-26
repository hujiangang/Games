using UnityEngine;

// Unit category (Ground or Air)
public enum UnitType
{
    Ground,
    Air
}

// Target types a unit can attack
public enum TargetType
{
    GroundOnly,
    AirOnly,
    GroundAndAir,
    BuildingsOnly
}

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Units/Unit Data")]
public class UnitData : ScriptableObject
{
    public string unitName;           // Unique identifier
    public GameObject prefab;         // Unit prefab
    public Sprite icon;               // UI icon

    [Header("General Stats")]
    public int manaCost;              // Mana required to spawn
    public float health;              // Max health
    public float moveSpeed;           // Movement speed
    public UnitType unitType;         // Unit type (ground/air)

    [Header("Attack Stats")]
    public TargetType targetType;     // What the unit can target
    public float damage;              // Damage per attack
    public float attackSpeed;         // Attacks per second
    public float attackRange;         // Attack range
    public GameObject projectilePrefab; // Projectile prefab (if any)
}
