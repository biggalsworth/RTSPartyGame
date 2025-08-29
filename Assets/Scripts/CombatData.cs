using System;
using System.Collections;
using UnityEngine;

public class CombatData
{
    internal GameObject owner;

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
        int finalDamage = (int)(damage * damageMultiplier);

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
        if (opponent.health > 0)
        {
            int counterStrengthDiff = opponent.offenceRating - defenceRating;
            double counterMultiplier = Math.Pow(1.05, counterStrengthDiff);
            int counterDamage = (int)(opponent.damage * counterMultiplier);
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


    public void SimulateBattle(CombatData opponent)
    {
        //run the coroutine throught he unitclass monobehaviour
        owner.GetComponent<UnitClass>().StartCoroutine(BattleSimulation(opponent));

        //// Step 1: Calculate strength differential
        //int strengthDifference = offenceRating - opponent.defenceRating;
        //
        //// Step 2: Use unit's own damage as base
        //double damageMultiplier = Math.Pow(1.05, strengthDifference); // 5% per point difference
        //int finalDamage = (int)(damage * damageMultiplier);
        //
        //// Clamp damage to reasonable bounds
        //finalDamage = Math.Clamp(finalDamage, 1, damage * 3); // Max 3x base damage
        //
        //// Step 3: Apply damage to opponent
        //opponent.health -= finalDamage;
        //opponent.owner.GetComponent<UnitClass>().defending = true;
        //
        //Debug.Log($"Team {team} attacks Team {opponent.team}!");
        //Debug.Log($"Strength difference: {strengthDifference}");
        //Debug.Log($"Damage dealt: {finalDamage}");
        //Debug.Log($"Opponent's remaining health: {opponent.health}");
        //
        //// Step 4: Optional counterattack if opponent survives
        //if (opponent.health > 0)
        //{
        //    int counterStrengthDiff = opponent.offenceRating - defenceRating;
        //    double counterMultiplier = Math.Pow(1.05, counterStrengthDiff);
        //    int counterDamage = (int)(opponent.damage * counterMultiplier);
        //    counterDamage = Math.Clamp(counterDamage, 1, opponent.damage * 3);
        //
        //    health -= counterDamage;
        //
        //    Console.WriteLine($"Team {opponent.team} counterattacks!");
        //    Console.WriteLine($"Counter damage dealt: {counterDamage}");
        //    Console.WriteLine($"Your remaining health: {health}");
        //}

    }

}
