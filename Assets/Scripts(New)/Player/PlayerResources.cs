using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerResources : MonoBehaviour
{
    [SerializeField, Min(0)] private int health = 5;
    [SerializeField, Min(0)] private int mana = 5;

    public int Health => health;
    public int Mana => mana;

    public event Action Changed;

    public void SetHealth(int value)
    {
        value = Mathf.Max(0, value);
        if (health == value)
            return;

        health = value;

        if(health <= 0)
        {
            Go();
        }

        Changed?.Invoke();
    }

    public void SetMana(int value)
    {
        value = Mathf.Max(0, value);
        if (mana == value)
            return;

        mana = value;
        Changed?.Invoke();
    }

    private void Go()
    {
        SceneManager.LoadScene("deadScene");
    }

    private void OnValidate()
    {
        health = Mathf.Max(0, health);
        mana = Mathf.Max(0, mana);
    }
}
