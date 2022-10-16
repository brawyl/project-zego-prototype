using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    private GameManager gameManager;

    public GameObject menuObject;
    public GameObject menuMain;
    public GameObject menuAttack;
    public GameObject menuSkill;
    public GameObject menuDefend;
    public GameObject menuTarget;
    public TMP_Text menuText;

    //menu description strings
    public string[] menuMainItems;
    public string[] menuAttackItems;
    public string[] menuSkillItems;
    public string[] menuDefendItems;
    public string[] menuTargetItems;

    public TMP_Text timerText;

    public GameObject gameOverObject;
    public GameObject gameOverScreen;
    public TMP_Text gameOverText;

    public GameObject mainFirstButton, attackFirstButton, skillFirstButton, defendFirstButton;
    public GameObject[] targetButtons;
    public GameObject restartButton;

    public List<GameObject> heroDamageText, enemyDamageText;

    public GameObject currentMenu;

    public TMP_Text nextTurnText;

    public string actionString = "";

    private void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        menuObject.SetActive(true);
        gameOverObject.SetActive(false);

        BuildMenuItems();

        gameManager.NextTurn();
    }

    private void Update()
    {
        //manu navigation with arrow keys
        if (currentMenu != null)
        {
            if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.Escape))
            {
                if (currentMenu == menuAttack)
                {
                    ShowMenuMain();
                }
                else if (currentMenu == menuTarget)
                {
                    ShowMenuAttack();
                }
            }
            else if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                //click the selected button right right arrow to go to next menu
                EventSystem.current.currentSelectedGameObject.GetComponent<Button>().onClick.Invoke();
            }
        }
    }

    public void HideAllMenus()
    {
        menuMain.gameObject.SetActive(false);
        menuAttack.gameObject.SetActive(false);
        menuSkill.gameObject.SetActive(false);
        menuDefend.gameObject.SetActive(false);
        menuTarget.gameObject.SetActive(false);
        currentMenu = null;
    }

    public void ShowMenuMain()
    {
        HideAllMenus();
        SetMenuDesc("ACTION");
        menuMain.gameObject.SetActive(true);

        //clear selected object
        EventSystem.current.SetSelectedGameObject(null);

        //set a new selected object
        EventSystem.current.SetSelectedGameObject(mainFirstButton);

        //change to atk pose
        GameObject activeChar = gameManager.activeChar;
        activeChar.GetComponentInChildren<SpriteRenderer>().sprite = activeChar.GetComponent<CharController>().idlePose;

        currentMenu = menuMain;
    }

    public void ShowMenuAttack()
    {
        HideAllMenus();
        SetMenuDesc("ATTACK");
        menuAttack.gameObject.SetActive(true);

        //clear selected object
        EventSystem.current.SetSelectedGameObject(null);

        //set a new selected object
        EventSystem.current.SetSelectedGameObject(attackFirstButton);

        //change to atk pose
        GameObject activeChar = gameManager.activeChar;
        activeChar.GetComponentInChildren<SpriteRenderer>().sprite = activeChar.GetComponent<CharController>().attackPose;

        currentMenu = menuAttack;

        actionString = "ATTACK";
    }

    public void ShowMenuSkill()
    {
        HideAllMenus();
        SetMenuDesc("SKILL");
        menuSkill.gameObject.SetActive(true);

        //clear selected object
        EventSystem.current.SetSelectedGameObject(null);

        //set a new selected object
        EventSystem.current.SetSelectedGameObject(skillFirstButton);

        //change to skill pose
        GameObject activeChar = gameManager.activeChar;
        activeChar.GetComponentInChildren<SpriteRenderer>().sprite = activeChar.GetComponent<CharController>().skillPose;

        currentMenu = menuSkill;

        actionString = "SKILL";
    }

    public void ShowMenuDefend()
    {
        HideAllMenus();
        SetMenuDesc("DEFEND");
        menuDefend.gameObject.SetActive(true);

        //clear selected object
        EventSystem.current.SetSelectedGameObject(null);

        //set a new selected object
        EventSystem.current.SetSelectedGameObject(defendFirstButton);

        //change to def pose
        GameObject activeChar = gameManager.activeChar;
        activeChar.GetComponentInChildren<SpriteRenderer>().sprite = activeChar.GetComponent<CharController>().defendPose;

        currentMenu = menuAttack;

        actionString = "DEFEND";
    }

    public void ShowMenuTarget(Button button)
    {
        gameManager.menuSelection = button.GetComponentInChildren<TMP_Text>().text;
        HideAllMenus();
        SetMenuDesc("TARGET");
        //add the action name from the button to the action string which is assumed to be at index 0
        actionString += " > " + button.GetComponentsInChildren<TMP_Text>()[0].text;
        menuTarget.gameObject.SetActive(true);

        //clear selected object
        EventSystem.current.SetSelectedGameObject(null);

        //loop thru targets and get first active button
        foreach(GameObject targetButton in targetButtons)
        {
            if (targetButton.activeSelf)
            {
                //set a new selected object
                EventSystem.current.SetSelectedGameObject(targetButton);
                break;
            }
        }

        currentMenu = menuTarget;
    }

    public void SetMenuDesc(string newDesc)
    {
        if (newDesc.Length > 0)
        {
            menuText.text = newDesc.ToUpper();
        }
        else
        {
            menuText.text = "ERROR";
        }
    }

    public void UpdateTimerText(string speed)
    {
        timerText.text = speed.ToString();
    }

    public void ShowGameOverScreen(bool gameOver)
    {
        gameOverObject.SetActive(gameOver);

        if (gameOver)
        {
            currentMenu = null;

            nextTurnText.text = "";

            //clear selected object
            EventSystem.current.SetSelectedGameObject(null);

            //set a new selected object
            EventSystem.current.SetSelectedGameObject(restartButton);
        }
    }

    private void BuildMenuItems()
    {
        menuMainItems = new string[] { "ATTACK", "DEFEND" };
        menuAttackItems = new string[] { "LIGHT", "HEAVY" };
        menuSkillItems = new string[] { "SINGLE", "MULTI" };
        menuDefendItems = new string[] { "BLOCK", "EVADE" };
        menuTargetItems = new string[] { "ENEMY 1", "ENEMY 2", "ENEMY 3" };

        //set attack costs
        TMP_Text[] attackItems = menuAttack.GetComponentsInChildren<TMP_Text>();
        foreach (TMP_Text attackItemText in attackItems)
        {
            if (attackItemText.gameObject.name.Contains("cost"))
            {
                //assume gameObject name is formatted like atk_cost_1 so index 2 of a split string will return the atk number
                string attackNumber = attackItemText.gameObject.name.Split("_")[2];
                //subtract 1 from attack number since the object names are numbered starting at 1
                int attackIndex = int.Parse(attackNumber) - 1;
                string attackName = menuAttackItems[attackIndex];
                int attackCost = gameManager.gameObject.GetComponent<Attack>().getAttackCost(attackName);
                attackItemText.text = attackCost.ToString();
            }
        }

        //set skill costs
        TMP_Text[] skillItems = menuSkill.GetComponentsInChildren<TMP_Text>();
        foreach (TMP_Text skillItemText in skillItems)
        {
            if (skillItemText.gameObject.name.Contains("cost"))
            {
                //assume gameObject name is formatted like skill_cost_1 so index 2 of a split string will return the atk number
                string skillNumber = skillItemText.gameObject.name.Split("_")[2];
                //subtract 1 from skill number since the object names are numbered starting at 1
                int skillIndex = int.Parse(skillNumber) - 1;
                string skillName = menuSkillItems[skillIndex];
                int skillCost = gameManager.gameObject.GetComponent<Skill>().getSkillCost(skillName);
                skillItemText.text = skillCost.ToString();
            }
        }
    }
}
