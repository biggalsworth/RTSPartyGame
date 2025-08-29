using UnityEngine;
using UnityEngine.AI;

public class UnitAnimation : MonoBehaviour
{
    public Animator anim;

    internal UnitClass unitInfo;

    public ParticleSystem hitEffect;
    int currHealth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unitInfo = GetComponent<UnitClass>();
        currHealth = unitInfo.data.health;
    }

    // Update is called once per frame
    void Update()
    {
        if (anim)
        {

            //if(GetComponent<NavMeshAgent>().velocity.magnitude > 0)
            //    Debug.Log("Current speed " + GetComponent<NavMeshAgent>().velocity.magnitude);
            anim.SetFloat("Speed", GetComponent<NavMeshAgent>().velocity.magnitude);


            anim.SetBool("Attacking", unitInfo.attacking);
            anim.SetBool("Defending", unitInfo.defending);
        }

        if(unitInfo.defending)
        {
            if (currHealth > GetComponent<UnitClass>().data.health)
            {
                hitEffect.Play();
                currHealth = unitInfo.data.health;
            }
        }
        else
        {
            currHealth = unitInfo.data.health;
        }
    }
}
