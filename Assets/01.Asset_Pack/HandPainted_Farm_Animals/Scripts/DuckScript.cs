using UnityEngine;
using System.Collections;

public class DuckScript : MonoBehaviour {

    Animator anim;

    enum Anim { Idle, Walk, Run, Sit, Eat, Swim };

    Anim currentAnim = Anim.Idle;

    // Use this for initialization
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Idle()
    {
        if (currentAnim != Anim.Idle)
        {
            anim.SetTrigger("Idle");

            currentAnim = Anim.Idle;
        }
    }

    public void Walk()
    {
        if (currentAnim == Anim.Idle || currentAnim == Anim.Run)
        {
            anim.SetTrigger("Walk");

            currentAnim = Anim.Walk;
        }
    }

    public void Run()
    {
        if (currentAnim == Anim.Idle || currentAnim == Anim.Walk)
        {
            anim.SetTrigger("Run");

            currentAnim = Anim.Run;
        }
    }

    public void Eat()
    {
        if (currentAnim == Anim.Idle)
        {
            anim.SetTrigger("Eat");

            currentAnim = Anim.Eat;
        }
    }

    public void Sit()
    {
        if (currentAnim == Anim.Idle)
        {
            anim.SetTrigger("Sit");

            currentAnim = Anim.Sit;
        }
    }

    public void Swim()
    {
        if (currentAnim == Anim.Idle)
        {
            anim.SetTrigger("Swim");

            currentAnim = Anim.Swim;
        }
    }

    public void Dive()
    {
        if (currentAnim == Anim.Swim)
        {
            anim.SetTrigger("Dive");

            //currentAnim = Anim.Dive;
        }
    }
}
