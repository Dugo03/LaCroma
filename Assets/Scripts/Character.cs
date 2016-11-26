using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class Character : MonoBehaviour {

    public delegate void OnHpChangeHandler(object sender, HpChangeArgs args);
    public event OnHpChangeHandler OnHpChange;

    private List<SpriteRenderer> IrisSpriteRenderers;

    public int currentHP;
    public int maxHP;
    public int damageThisTurn;

    void Start()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<OnHurtOverworld>().OnIsHurt += takeDamage;
        currentHP = maxHP;
        IrisSpriteRenderers = getAllRenderers(gameObject.transform);
    }

    private void takeDamage(object sender, DamageArgs args)
    {
        currentHP -= args.Damage;
        damageThisTurn += args.Damage;
        float percentage = (float)currentHP / maxHP;

        setSaturation(percentage);
        if (OnHpChange != null)
            OnHpChange(this, new HpChangeArgs(currentHP));

        AnimateArgs animateArgs = new AnimateArgs("CurrentLife");
        animateArgs.F = percentage;

        BroadcastMessage("animateFloat", animateArgs);

        if (currentHP <= 0)
        {
            Destroy(gameObject.GetComponent<OnHurtOverworld>());
            Destroy(gameObject.GetComponent<Jump>());
            Destroy(gameObject.GetComponent<Move>());
            BroadcastMessage("die");
        }

    }

    public void setSaturation(float percentage)
    {
        foreach (SpriteRenderer renderer in IrisSpriteRenderers)
        {
            Material mat = renderer.material;
            mat.SetFloat("_Sat", percentage);
        }
    }

    private List<SpriteRenderer> getAllRenderers(Transform obj)
    {
        List<SpriteRenderer> result = new List<SpriteRenderer>();
        if (obj.GetComponent<SpriteRenderer>())
            result.Add(obj.GetComponent<SpriteRenderer>());

        foreach (Transform child in obj.transform)
            result.AddRange(getAllRenderers(child));

        return result;
    }

    public void nextTurn()
    {
        damageThisTurn = 0;
    }
}

public class HpChangeArgs : EventArgs
{
    int hp;

    public int Hp
    {
        get { return hp; }
    }

    public HpChangeArgs(int newHp)
    {
        hp = newHp;
    }
}