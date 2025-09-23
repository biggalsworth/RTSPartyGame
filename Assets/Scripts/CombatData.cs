using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;

[System.Serializable]
public struct UnitStats
{
    public int team;
    public int health;
    public int defenceRating;
    public int offenceRating;
    public int damage;
    public int attackRange;

}

[System.Serializable]
public class CombatData
{
    internal GameObject owner;
    internal UnitClass ownerUnit;
    internal UnitClass opponentUnit;

    public int team;

    internal int health = 0;

    public int defenceRating = 5;
    public int offenceRating = 2;

    public int damage = 3;
    public int attackRange = 3;

    public CombatData(int team, int health, int defenceRating, int offenceRating, int damage, int attackRange  )
    {
        this.team = team;
        this.health = health;
        this.defenceRating = defenceRating;
        this.offenceRating = offenceRating;
        this.damage = damage;
        this.attackRange = attackRange;
        this.owner = null;
    }

    internal IEnumerator BattleSimulation(CombatData opponent)
    {
        opponent.owner.GetComponent<UnitClass>().busy = true;
        owner.GetComponent<UnitClass>().busy = true;

        // Step 1: Calculate strength differential
        int strengthDifference = offenceRating - opponent.defenceRating;

        // Step 2: Use unit's own damage as base
        double damageMultiplier = Math.Pow(1.05, strengthDifference); // 5% per point difference

        float terrainBonus = CalculateTerrainBonus(ownerUnit, opponentUnit);

        int finalDamage = (int)(damage * damageMultiplier * terrainBonus);

        // Clamp damage to reasonable bounds
        finalDamage = Math.Clamp(finalDamage, 1, damage * 3); // Max 3x base damage

        owner.GetComponent<UnitClass>().attacking = true;
        opponent.owner.GetComponent<UnitClass>().defending = true;

        yield return new WaitForSeconds(1f);

        owner.GetComponent<UnitAudio>().PlayBattle();
        // Step 3: Apply damage to opponent
        opponent.health -= finalDamage;

        yield return new WaitForSeconds(1f);

        Debug.Log($"Team {team} attacks Team {opponent.team}!");
        Debug.Log($"Strength difference: {strengthDifference}");
        Debug.Log($"Damage dealt: {finalDamage}");
        Debug.Log($"Opponent's remaining health: {opponent.health}");


        owner.GetComponent<UnitClass>().attacking = false;
        opponent.owner.GetComponent<UnitClass>().defending = false;



        // Step 4: Optional counterattack if opponent survives
        if (opponent.health > 0 && HexManager.instance.HexDistance(owner.transform.position, opponent.owner.transform.position) <= attackRange)
        {
            int counterStrengthDiff = opponent.offenceRating - defenceRating;
            double counterMultiplier = Math.Pow(1.05, counterStrengthDiff);

            terrainBonus = opponent.CalculateTerrainBonus(opponentUnit, ownerUnit);

            int counterDamage = (int)(opponent.damage * counterMultiplier * terrainBonus);
            counterDamage = Math.Clamp(counterDamage, 1, opponent.damage * 3);

            owner.GetComponent<UnitClass>().defending = true;
            opponent.owner.GetComponent<UnitClass>().attacking = true;


            yield return new WaitForSeconds(1f);
            try
            {
                opponent.owner.GetComponent<UnitAudio>().PlayBattle();
            }
            catch{ }
            health -= counterDamage;
            yield return new WaitForSeconds(1f);


            Debug.Log($"Team {opponent.team} counterattacks!");
            Debug.Log($"Counter damage dealt: {counterDamage}");
            Debug.Log($"Your remaining health: {health}");
        }


        owner.GetComponent<UnitClass>().defending = false;
        opponent.owner.GetComponent<UnitClass>().attacking = false;

        opponent.owner.GetComponent<UnitClass>().busy = false;
        owner.GetComponent<UnitClass>().busy = false;
    }

    private float CalculateTerrainBonus(UnitClass UnitA, UnitClass UnitB)
    {
        float terrainModifier = 1f;

        //Check if we get a terrain bonus
        terrainModifier += HexManager.instance.FindHex(UnitA.HexPosition, 2.0f).TerrainBonuses();

        //check if oponnent gets terrain bonus
        terrainModifier -= HexManager.instance.FindHex(UnitB.HexPosition, 2.0f).TerrainBonuses();

        terrainModifier = Mathf.Clamp(terrainModifier, 0.5f, 2f); // Prevent extreme values

        return terrainModifier;
    }

    public void SimulateBattle(CombatData opponent)
    {
        //run the coroutine throught he unitclass monobehaviour - allows animation timing
        ownerUnit = owner.GetComponent<UnitClass>();
        opponentUnit = opponent.owner.GetComponent<UnitClass>();

        ownerUnit.StartCoroutine(BattleSimulation(opponent));
    }

}



[System.Serializable]
public class CombatLogic
{
    private UnitClass unit;

    public CombatLogic(UnitClass unit)
    {
        this.unit = unit;
    }

    internal IEnumerator BattleSimulation(UnitClass opponentUnit)
    {
        UnitStats owner = unit.data;
        UnitStats opponent = opponentUnit.data;

        // Step 1: Calculate strength differential
        int strengthDifference = owner.offenceRating - opponent.defenceRating;

        // Step 2: Use unit's own damage as base
        double damageMultiplier = Math.Pow(1.05, strengthDifference); // 5% per point difference

        float terrainBonus = CalculateTerrainBonus(unit, opponentUnit);

        int finalDamage = (int)(owner.damage * damageMultiplier * terrainBonus);

        // Clamp damage to reasonable bounds
        finalDamage = Math.Clamp(finalDamage, 1, owner.damage * 3); // Max 3x base damage

        unit.attacking = true;
        opponentUnit.defending = true;

        yield return new WaitForSeconds(1f);

        unit.GetComponent<UnitAudio>().PlayBattle();

        // Step 3: Apply damage to opponent
        opponent.health -= finalDamage;
        //opponentUnit.data.health -= finalDamage;
        opponentUnit.data = opponent;


        yield return new WaitForSeconds(1f);

        Debug.Log($"Team {owner.team} attacks Team {opponent.team}!");
        Debug.Log($"Strength difference: {strengthDifference}");
        Debug.Log($"Damage dealt: {finalDamage}");
        Debug.Log($"Opponent's remaining health: {opponent.health}");


        unit.attacking = false;
        opponentUnit.defending = false;


        // Step 4: Optional counterattack if opponent survives
        if (opponent.health > 0 && HexManager.instance.HexDistance(unit.transform.position, opponentUnit.transform.position) <= opponent.attackRange)
        {
            int counterStrengthDiff = opponent.offenceRating - owner.defenceRating;
            double counterMultiplier = Math.Pow(1.05, counterStrengthDiff);

            terrainBonus = CalculateTerrainBonus(opponentUnit, unit);

            int counterDamage = (int)(opponent.damage * counterMultiplier * terrainBonus);
            counterDamage = Math.Clamp(counterDamage, 1, opponent.damage * 3);

            unit.defending = true;
            opponentUnit.attacking = true;


            yield return new WaitForSeconds(1f);
            try
            {
                opponentUnit.GetComponent<UnitAudio>().PlayBattle();
            }
            catch { }

            //update health
            owner.health -= counterDamage;
            unit.data = owner;

            yield return new WaitForSeconds(1f);


            Debug.Log($"Team {opponent.team} counterattacks!");
            Debug.Log($"Counter damage dealt: {counterDamage}");
            Debug.Log($"Your remaining health: {owner.health}");
        }


        unit.defending = false;
        opponentUnit.attacking = false;

        opponentUnit.busy = false;
        unit.busy = false;
    }

    private float CalculateTerrainBonus(UnitClass UnitA, UnitClass UnitB)
    {
        float terrainModifier = 1f;

        //Check if we get a terrain bonus
        terrainModifier += HexManager.instance.FindHex(UnitA.HexPosition, 2.0f).TerrainBonuses();

        //check if oponnent gets terrain bonus
        terrainModifier -= HexManager.instance.FindHex(UnitB.HexPosition, 2.0f).TerrainBonuses();

        terrainModifier = Mathf.Clamp(terrainModifier, 0.5f, 2f); // Prevent extreme values

        return terrainModifier;
    }

    public void SimulateBattle(UnitClass opponentUnit)
    {
        //run the coroutine throught he unitclass monobehaviour - allows animation timing
        //ownerUnit = owner.GetComponent<UnitClass>();
        //opponentUnit = opponent.owner.GetComponent<UnitClass>();

        unit.StartCoroutine(BattleSimulation(opponentUnit));
    }

}
