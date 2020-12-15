using UnityEngine;
using System.Collections;

public class RabbitScript : MonoBehaviour {

    Animator anim;

    enum Anim { Idle, Walk, Run, Sit, Eat };

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

    public void Run()
    {
        if (currentAnim == Anim.Idle)
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
}
