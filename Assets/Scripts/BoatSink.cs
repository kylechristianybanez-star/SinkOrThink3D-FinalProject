
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoatSink : MonoBehaviour
{
    public float sinkSpeed = 0.5f;
    public bool isSinking = false;

    public int maxHealth = 100;
    public int currentHealth = 100;
    public int damage = 20;

    public Slider healthBar;
    public TMP_Text healthText;

    void Start()
    {
        currentHealth = maxHealth;
        isSinking = false;

        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;

        healthText.text = "Boat Health: " + currentHealth + "/" + maxHealth;
    }

    void Update()
    {
        if (isSinking == true)
        {
            transform.position += Vector3.down * sinkSpeed * Time.deltaTime;
        }
    }

    public void SinkBoat()
    {
        if (currentHealth <= 0)
        {
            return;
        }

        currentHealth = currentHealth - damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isSinking = true;
        }

        healthBar.value = currentHealth;

        healthText.text = "Boat Health: " + currentHealth + "/" + maxHealth;

        Debug.Log("Boat Health: " + currentHealth);
    }
}
