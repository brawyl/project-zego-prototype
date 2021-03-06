using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text charNameText;
    [SerializeField]
    private Slider hpSlider;

    private Vector3 startPosition;

    public string charName;
    public int charHPMax;
    public int charHPCurrent;
    public int charStrength;
    public int charDefense;
    public int charSpeed;
    public bool charAlive;
    public bool activeTurn;

    // Start is called before the first frame update
    void Start()
    {
        charAlive = true;
        charHPCurrent = charHPMax;
        hpSlider.value = 1.0f;
        charNameText.text = charName;

        //get starting position
        startPosition = gameObject.GetComponent<Transform>().position;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CheckTurn()
    {
        if (activeTurn)
        {
            gameObject.transform.position = Vector3.zero;
            if (gameObject.tag.Equals("Hero"))
            {
                GameManager.instance.GetComponent<UIManager>().ShowMenuMain();
            }
            else
            {
                //enemy targets random player with random action
                GameManager.instance.EnemyTurn();
            }
            GameManager.instance.GetComponent<UIManager>().UpdateTimerText(charSpeed.ToString());
        }
        else
        {
            gameObject.transform.position = startPosition;
            GameManager.instance.GetComponent<UIManager>().HideAllMenus();
        }
    }

    public void TakeDamage(int damage)
    {
        charHPCurrent -= damage;
        charHPCurrent = charHPCurrent < 0 ? 0 : charHPCurrent;
        hpSlider.value = (float)charHPCurrent / (float)charHPMax;

        if (charHPCurrent <= 0)
        {
            charAlive = false;
        }
    }

    void DamageTarget(int damage, GameObject target)
    {
        target.GetComponent<CharController>().TakeDamage(damage);
    }
}
